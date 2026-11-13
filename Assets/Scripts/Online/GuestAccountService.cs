using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Гостевой аккаунт: Firebase Auth (e-mail + случайный пароль) + строка в Supabase через SupabaseManager.
// Данные сохраняются локально, чтобы потом можно было привязать нормальный аккаунт.
public class GuestAccountService : MonoBehaviour
{
    public static GuestAccountService Instance { get; private set; }

    private const string KeyEmail = "Guest_Email";
    private const string KeyPassword = "Guest_Password";
    private const string KeyUid = "Guest_Uid";
    private const string KeySupabaseDone = "Guest_SupabaseDone";

    public string IdToken { get; private set; }
    public string Uid { get; private set; }
    public bool IsSignedIn => !string.IsNullOrEmpty(IdToken);

    [Serializable] private class AuthResponse { public string idToken; public string localId; public string refreshToken; }
    [Serializable] private class AuthErrorBody { public string message; }
    [Serializable] private class AuthError { public AuthErrorBody error; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!FirebaseSettings.IsConfigured)
        {
            Debug.LogWarning("[Guest] Firebase не настроен (FirebaseSettings), гостевой вход пропущен.");
            return;
        }
        EnsureGuest((ok, msg) => Debug.Log("[Guest] " + (ok ? "готово" : "ошибка: " + msg)));
    }

    // Вход существующим гостем или регистрация нового
    public void EnsureGuest(Action<bool, string> done)
    {
        if (IsSignedIn) { done?.Invoke(true, null); return; }

        if (PlayerPrefs.HasKey(KeyPassword))
            StartCoroutine(AuthCoroutine("accounts:signInWithPassword", PlayerPrefs.GetString(KeyEmail), PlayerPrefs.GetString(KeyPassword), done, false));
        else
            StartCoroutine(SignUpGuest(done));
    }

    private IEnumerator SignUpGuest(Action<bool, string> done)
    {
        string email = "guest_" + Guid.NewGuid().ToString("N").Substring(0, 16) + "@" + FirebaseSettings.GuestEmailDomain;
        string password = RandomPassword(24);

        yield return AuthCoroutine("accounts:signUp", email, password, (ok, msg) =>
        {
            if (!ok) { done?.Invoke(false, msg); return; }

            // Сохраняем логин/пароль гостя локально (для будущей привязки аккаунта)
            PlayerPrefs.SetString(KeyEmail, email);
            PlayerPrefs.SetString(KeyPassword, password);
            PlayerPrefs.Save();

            RegisterInSupabase(done);
        }, true);
    }

    // Токен Firebase живёт около часа: при 401 берём новый по сохранённым e-mail и паролю
    public void RefreshSession(Action<bool, string> done)
    {
        if (!PlayerPrefs.HasKey(KeyPassword))
        {
            done?.Invoke(false, "Гостевой аккаунт ещё не создан");
            return;
        }
        IdToken = null;
        StartCoroutine(AuthCoroutine("accounts:signInWithPassword", PlayerPrefs.GetString(KeyEmail), PlayerPrefs.GetString(KeyPassword), done, false));
    }

    private void RegisterInSupabase(Action<bool, string> done)
    {
        if (PlayerPrefs.GetInt(KeySupabaseDone, 0) == 1)
        {
            done?.Invoke(true, null);
            return;
        }

        if (SupabaseManager.Instance == null)
        {
            done?.Invoke(false, "SupabaseManager не найден в сцене");
            return;
        }

        string gameName = PlayerPrefs.GetString("PlayerName", "Гость " + Uid?.Substring(0, Mathf.Min(4, Uid.Length)));
        SupabaseManager.Instance.CreateGuestAccount(gameName, (ok, msg) =>
        {
            if (ok)
            {
                PlayerPrefs.SetInt(KeySupabaseDone, 1);
                PlayerPrefs.Save();
            }
            done?.Invoke(ok, msg);
        });
    }

    private IEnumerator AuthCoroutine(string endpoint, string email, string password, Action<bool, string> done, bool isSignUp)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/{endpoint}?key={FirebaseSettings.ApiKey}";
        string body = JsonUtility.ToJson(new AuthRequest { email = email, password = password, returnSecureToken = true });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                done?.Invoke(false, ReadAuthError(request.downloadHandler.text, request.error));
                yield break;
            }

            AuthResponse res = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
            if (res == null || string.IsNullOrEmpty(res.idToken))
            {
                done?.Invoke(false, "Пустой ответ Firebase");
                yield break;
            }

            IdToken = res.idToken;
            Uid = res.localId;
            PlayerPrefs.SetString(KeyUid, Uid);
            PlayerPrefs.Save();

            done?.Invoke(true, null);
        }
    }

    [Serializable]
    private class AuthRequest
    {
        public string email;
        public string password;
        public bool returnSecureToken;
    }

    private static string ReadAuthError(string text, string fallback)
    {
        try
        {
            AuthError err = JsonUtility.FromJson<AuthError>(text);
            if (err != null && err.error != null && !string.IsNullOrEmpty(err.error.message))
                return err.error.message;
        }
        catch (Exception) { }
        return fallback;
    }

    private static string RandomPassword(int length)
    {
        const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        byte[] buffer = new byte[length];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        StringBuilder sb = new StringBuilder(length);
        foreach (byte b in buffer)
        {
            sb.Append(chars[b % chars.Length]);
        }
        return sb.ToString();
    }
}
