using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Глобальный чат на Firebase Realtime Database через REST (без SDK).
// Путь: {DatabaseUrl}/chat/global. Нужны правила с индексом по полю "time".
public class FirebaseChatService : MonoBehaviour
{
    public static FirebaseChatService Instance { get; private set; }

    public event Action<List<ChatMessage>> OnMessagesUpdated;
    public event Action<string> OnError;

    public bool IsPolling { get; private set; }
    public List<ChatMessage> Messages { get; } = new List<ChatMessage>();

    [Serializable]
    public class ChatMessage
    {
        public string name;
        public string text;
        public string uid;
        public double time;
    }

    private Coroutine pollRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Вызывать, когда открыта вкладка чата
    public void StartPolling()
    {
        if (IsPolling) return;
        if (!FirebaseSettings.IsConfigured) { OnError?.Invoke("Firebase не настроен"); return; }
        IsPolling = true;
        pollRoutine = StartCoroutine(PollLoop());
    }

    // Вызывать, когда вкладка чата закрыта
    public void StopPolling()
    {
        IsPolling = false;
        if (pollRoutine != null) StopCoroutine(pollRoutine);
        pollRoutine = null;
    }

    private IEnumerator PollLoop()
    {
        while (IsPolling)
        {
            yield return FetchCoroutine();
            yield return new WaitForSeconds(FirebaseSettings.ChatPollSeconds);
        }
    }

    public void SendMessage(string rawText)
    {
        string text = (rawText ?? "").Trim();
        if (text.Length == 0) return;
        if (text.Length > FirebaseSettings.ChatMaxLength) text = text.Substring(0, FirebaseSettings.ChatMaxLength);

        if (!GuestAccountService.Instance || !GuestAccountService.Instance.IsSignedIn)
        {
            OnError?.Invoke("Нет входа в аккаунт, попробуйте позже");
            return;
        }
        StartCoroutine(SendCoroutine(text));
    }

    private IEnumerator SendCoroutine(string text)
    {
        var message = new ChatMessage
        {
            name = PlayerPrefs.GetString("PlayerName", "Игрок"),
            text = text,
            uid = GuestAccountService.Instance.Uid,
            time = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
        };
        string url = $"{FirebaseSettings.DatabaseUrl}/chat/global.json?auth={GuestAccountService.Instance.IdToken}";
        string body = JsonUtility.ToJson(message);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                OnError?.Invoke("Не удалось отправить: " + request.error);
                yield break;
            }
        }

        yield return FetchCoroutine();
    }

    private IEnumerator FetchCoroutine()
    {
        if (!GuestAccountService.Instance || !GuestAccountService.Instance.IsSignedIn) yield break;

        string url = $"{FirebaseSettings.DatabaseUrl}/chat/global.json" +
                     $"?orderBy=%22time%22&limitToLast={FirebaseSettings.ChatHistoryCount}" +
                     $"&auth={GuestAccountService.Instance.IdToken}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                OnError?.Invoke("Чат недоступен: " + request.error);
                yield break;
            }

            List<ChatMessage> parsed = ParseMessages(request.downloadHandler.text);
            parsed.Sort((a, b) => a.time.CompareTo(b.time));

            Messages.Clear();
            Messages.AddRange(parsed);
            OnMessagesUpdated?.Invoke(Messages);
        }
    }

    // Ответ Firebase: {"key1":{...},"key2":{...}} (или "null", если пусто)
    public static List<ChatMessage> ParseMessages(string json)
    {
        var result = new List<ChatMessage>();
        if (string.IsNullOrEmpty(json) || json == "null") return result;

        foreach (string obj in TopLevelObjects(json))
        {
            ChatMessage m = null;
            try { m = JsonUtility.FromJson<ChatMessage>(obj); }
            catch (Exception) { }
            if (m != null && !string.IsNullOrEmpty(m.text)) result.Add(m);
        }
        return result;
    }

    // Достаёт значения-объекты верхнего уровня, учитывая строки и экранирование
    private static IEnumerable<string> TopLevelObjects(string json)
    {
        int depth = 0;
        bool inString = false;
        int start = -1;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (inString)
            {
                if (c == '\\') { i++; continue; }
                if (c == '"') inString = false;
                continue;
            }

            if (c == '"') { inString = true; continue; }

            if (c == '{')
            {
                if (depth == 1) start = i;   // объект-значение внутри корневого словаря
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 1 && start >= 0)
                {
                    yield return json.Substring(start, i - start + 1);
                    start = -1;
                }
            }
        }
    }
}
