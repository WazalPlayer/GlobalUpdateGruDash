using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance;

    [System.Serializable]
    public class UnlockableObject
    {
        public string objectTag;
        [Tooltip("Отображать в списке редактора")] 
        public bool showInEditor = true;
    }

    [Header("Управление разблокировкой")]
    public List<UnlockableObject> unlockableObjects = new List<UnlockableObject>();

    private const string SaveKeyPrefix = "UNLOCKED_";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        RefreshAllObjects();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAllObjects();
    }

    public void UnlockObject(string objectTag)
    {
        if (!IsUnlocked(objectTag))
        {
            PlayerPrefs.SetInt(SaveKeyPrefix + objectTag, 1);
            PlayerPrefs.Save();
            SetObjectsActive(objectTag, true);
        }
    }

    private void RefreshAllObjects()
    {
        foreach (var item in unlockableObjects)
        {
            SetObjectsActive(item.objectTag, IsUnlocked(item.objectTag));
        }
    }

    private void SetObjectsActive(string tag, bool active)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            obj.SetActive(active);
        }
    }

    public bool IsUnlocked(string objectTag)
    {
        return PlayerPrefs.GetInt(SaveKeyPrefix + objectTag, 0) == 1;
    }

    [ContextMenu("Сбросить прогресс")]
    public void ResetProgress()
    {
        foreach (var item in unlockableObjects)
        {
            PlayerPrefs.DeleteKey(SaveKeyPrefix + item.objectTag);
        }
        RefreshAllObjects();
    }
}