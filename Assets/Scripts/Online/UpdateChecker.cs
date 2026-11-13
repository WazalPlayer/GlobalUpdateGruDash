using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Проверка обновлений по манифесту на GitHub. Можно отключить в настройках.
public class UpdateChecker : MonoBehaviour
{
    public static UpdateChecker Instance { get; private set; }

    private const string KeyEnabled = "Updates_Enabled";

    public event Action<UpdateManifest> OnUpdateAvailable;
    public event Action<string> OnError;

    public UpdateManifest Latest { get; private set; }
    public bool IsChecking { get; private set; }

    [Serializable]
    public class UpdateManifest
    {
        public string version;
        public string url;
        public string notes;
    }

    public static string CurrentVersion => Application.version;

    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(KeyEnabled, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (IsEnabled) Check();
    }

    public void Check()
    {
        if (!FirebaseSettings.IsUpdateConfigured)
        {
            OnError?.Invoke("Адрес манифеста обновлений не настроен");
            return;
        }
        if (IsChecking) return;
        StartCoroutine(CheckCoroutine());
    }

    private IEnumerator CheckCoroutine()
    {
        IsChecking = true;
        using (UnityWebRequest request = UnityWebRequest.Get(FirebaseSettings.UpdateManifestUrl))
        {
            yield return request.SendWebRequest();

            IsChecking = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                OnError?.Invoke("Не удалось проверить обновления: " + request.error);
                yield break;
            }

            UpdateManifest manifest = null;
            try { manifest = JsonUtility.FromJson<UpdateManifest>(request.downloadHandler.text); }
            catch (Exception) { }

            if (manifest == null || string.IsNullOrEmpty(manifest.version))
            {
                OnError?.Invoke("Некорректный манифест обновлений");
                yield break;
            }

            Latest = manifest;
            if (IsNewer(manifest.version, CurrentVersion))
                OnUpdateAvailable?.Invoke(manifest);
        }
    }

    public static bool IsNewer(string remote, string local)
    {
        if (Version.TryParse(remote, out Version r) && Version.TryParse(local, out Version l))
            return r > l;
        return remote != local;
    }
}
