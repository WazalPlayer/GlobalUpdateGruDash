using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class EventsSystemManager : MonoBehaviour
{
    public static EventsSystemManager Instance { get; private set; }

    [Header("Supabase Settings")]
    public string supabaseUrl = "https://fiwfzjcceqiaisesmxas.supabase.co";
    public string supabaseKey = ""; // вставьте свой anon ключ
    public string eventsTable = "events";
    public string depositsTable = "event_deposits";

    [Header("UI Prefabs")]
    public GameObject eventItemPrefab;
    public Transform eventsContainer;
    public GameObject depositPanel;
    public TMP_InputField depositInputField;
    public Button confirmDepositButton;
    public Button cancelDepositButton;

    [Header("Event Images")]
    public Sprite[] eventImages;

    [Header("Debug")]
    public bool debugMode = true;

    private List<EventItemUI> activeEventItems = new List<EventItemUI>();
    private EventItemUI currentEventForDeposit;

    [Serializable] private class EventList { public List<EventData> events; }
    [Serializable] private class DepositList { public List<DepositRecord> deposits; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (depositPanel != null) depositPanel.SetActive(false);
        if (confirmDepositButton != null) confirmDepositButton.onClick.AddListener(OnConfirmDeposit);
        if (cancelDepositButton != null) cancelDepositButton.onClick.AddListener(OnCancelDeposit);
    }

    void Start()
    {
        if (string.IsNullOrEmpty(supabaseKey))
        {
            Debug.LogError("EventsSystemManager: supabaseKey не задан!");
            return;
        }
        LoadActiveEvents();
    }

    public void LoadActiveEvents()
    {
        StartCoroutine(LoadEventsCoroutine());
    }

    public void RefreshEventProgress(int eventId)
    {
        StartCoroutine(RefreshEventCoroutine(eventId));
    }

    public void ShowDepositPanel(EventItemUI eventItem)
    {
        if (depositPanel == null) return;
        currentEventForDeposit = eventItem;
        if (depositInputField != null) depositInputField.text = "50";
        depositPanel.SetActive(true);
    }

    public void ClaimReward(int eventId, int medalId, EventItemUI uiItem)
    {
        StartCoroutine(ClaimRewardCoroutine(eventId, medalId, uiItem));
    }

    private IEnumerator LoadEventsCoroutine()
    {
        string url = $"{supabaseUrl}/rest/v1/{eventsTable}?is_active=eq.true&select=*";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Ошибка загрузки событий: {request.responseCode} - {request.error}");
                yield break;
            }

            List<EventData> events = ParseEventsFromJSON(request.downloadHandler.text);
            if (events == null) events = new List<EventData>();

            string playerId = SkinManager.Instance.GetPlayerID();
            string depositsUrl = $"{supabaseUrl}/rest/v1/{depositsTable}?player_id=eq.{UnityWebRequest.EscapeURL(playerId)}&select=*";
            List<DepositRecord> playerDeposits = new List<DepositRecord>();
            using (UnityWebRequest depositReq = UnityWebRequest.Get(depositsUrl))
            {
                depositReq.SetRequestHeader("apikey", supabaseKey);
                depositReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
                yield return depositReq.SendWebRequest();
                if (depositReq.result == UnityWebRequest.Result.Success)
                {
                    playerDeposits = ParseDepositsFromJSON(depositReq.downloadHandler.text);
                }
            }

            UpdateEventsUI(events, playerDeposits);
        }
    }

    private IEnumerator RefreshEventCoroutine(int eventId)
    {
        string url = $"{supabaseUrl}/rest/v1/{eventsTable}?id=eq.{eventId}&select=*";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                List<EventData> events = ParseEventsFromJSON(request.downloadHandler.text);
                if (events != null && events.Count > 0)
                {
                    EventData updatedEvent = events[0];
                    EventItemUI uiItem = activeEventItems.Find(item => item.eventId == eventId);
                    if (uiItem != null)
                    {
                        uiItem.UpdateProgress(updatedEvent.current_deposit, updatedEvent.goal);
                        StartCoroutine(CheckAndUpdateClaimButtonCoroutine(uiItem, updatedEvent, SkinManager.Instance.GetPlayerID()));
                    }
                }
            }
        }
    }

private IEnumerator SubmitDepositCoroutine(int eventId, int amount)
{
    string playerId = SkinManager.Instance.GetPlayerID();
    int currentStars = AccountsManager.Instance.GetPlayerStars();

    if (amount <= 0)
    {
        ShowMessage("Сумма должна быть больше 0");
        yield break;
    }
    if (currentStars < amount)
    {
        ShowMessage("Недостаточно звёзд!");
        yield break;
    }

    // 1. Получить актуальное состояние события
    EventData currentEvent = null;
    string eventUrl = $"{supabaseUrl}/rest/v1/{eventsTable}?id=eq.{eventId}&select=*";
    using (UnityWebRequest eventReq = UnityWebRequest.Get(eventUrl))
    {
        eventReq.SetRequestHeader("apikey", supabaseKey);
        eventReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        yield return eventReq.SendWebRequest();

        if (eventReq.result == UnityWebRequest.Result.Success)
        {
            var events = ParseEventsFromJSON(eventReq.downloadHandler.text);
            if (events != null && events.Count > 0) currentEvent = events[0];
        }
    }

    if (currentEvent == null || !currentEvent.is_active)
    {
        ShowMessage("Событие не активно");
        yield break;
    }
    if (currentEvent.current_deposit >= currentEvent.goal)
    {
        ShowMessage("Событие уже завершено");
        yield break;
    }

    // 2. Ограничение: нельзя внести больше, чем осталось до цели
    int remaining = currentEvent.goal - currentEvent.current_deposit;
    if (amount > remaining)
    {
        ShowMessage($"Максимальная сумма для депозита: {remaining} звёзд");
        yield break;
    }

    // 3. Обновить прогресс события
    int newDeposit = currentEvent.current_deposit + amount;
    string patchUrl = $"{supabaseUrl}/rest/v1/{eventsTable}?id=eq.{eventId}";
    string patchJson = $"{{\"current_deposit\":{newDeposit}}}";
    using (UnityWebRequest patchReq = new UnityWebRequest(patchUrl, "PATCH"))
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(patchJson);
        patchReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
        patchReq.downloadHandler = new DownloadHandlerBuffer();
        patchReq.SetRequestHeader("Content-Type", "application/json");
        patchReq.SetRequestHeader("apikey", supabaseKey);
        patchReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        patchReq.SetRequestHeader("Prefer", "return=minimal");
        yield return patchReq.SendWebRequest();

        if (patchReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Ошибка обновления прогресса: {patchReq.error}");
            ShowMessage("Ошибка при внесении вклада");
            yield break;
        }
    }

    // 4. Записать депозит игрока (можно много записей, никакой уникальности)
    string insertJson = $"{{\"event_id\":{eventId},\"player_id\":\"{playerId}\",\"deposit_amount\":{amount},\"claimed\":false}}";
    if (debugMode) Debug.Log($"[Events] Депозит: {insertJson}");

    string insertUrl = $"{supabaseUrl}/rest/v1/{depositsTable}";
    using (UnityWebRequest insertReq = new UnityWebRequest(insertUrl, "POST"))
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(insertJson);
        insertReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
        insertReq.downloadHandler = new DownloadHandlerBuffer();
        insertReq.SetRequestHeader("Content-Type", "application/json");
        insertReq.SetRequestHeader("apikey", supabaseKey);
        insertReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        insertReq.SetRequestHeader("Prefer", "return=minimal");
        yield return insertReq.SendWebRequest();

        if (insertReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Ошибка записи депозита: {insertReq.responseCode} - {insertReq.error}");
            ShowMessage("Вклад не зафиксирован, звёзды не списаны");
            yield break;
        }
    }

    // 5. Списать звёзды
    AccountsManager.Instance.AddPlayerStars(-amount);
    ShowMessage($"Вы внесли {amount} звёзд! Всего собрано: {newDeposit}/{currentEvent.goal}");

    // 6. Обновить UI (прогресс, но кнопка Deposit остаётся, если цель не достигнута)
    RefreshEventProgress(eventId);

    // 7. Если событие завершилось, скрыть Deposit и показать Claim
    if (newDeposit >= currentEvent.goal)
    {
        ShowMessage($"Событие \"{currentEvent.event_name}\" завершено! Заберите награду.");
        RefreshEventProgress(eventId); // обновит кнопки
    }
}

    private IEnumerator ClaimRewardCoroutine(int eventId, int medalId, EventItemUI uiItem)
    {
        string playerId = SkinManager.Instance.GetPlayerID();

        string checkUrl = $"{supabaseUrl}/rest/v1/{depositsTable}?event_id=eq.{eventId}&player_id=eq.{UnityWebRequest.EscapeURL(playerId)}&select=id,claimed";
        using (UnityWebRequest checkReq = UnityWebRequest.Get(checkUrl))
        {
            checkReq.SetRequestHeader("apikey", supabaseKey);
            checkReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return checkReq.SendWebRequest();

            if (checkReq.result != UnityWebRequest.Result.Success || checkReq.downloadHandler.text == "[]")
            {
                ShowMessage("Вы не участвовали в этом событии");
                yield break;
            }

            var deposits = ParseDepositsFromJSON(checkReq.downloadHandler.text);
            if (deposits == null || deposits.Count == 0 || deposits[0].claimed)
            {
                ShowMessage("Награда уже получена");
                yield break;
            }
        }

        string eventUrl = $"{supabaseUrl}/rest/v1/{eventsTable}?id=eq.{eventId}&select=current_deposit,goal,is_active";
        using (UnityWebRequest eventReq = UnityWebRequest.Get(eventUrl))
        {
            eventReq.SetRequestHeader("apikey", supabaseKey);
            eventReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return eventReq.SendWebRequest();

            if (eventReq.result == UnityWebRequest.Result.Success)
            {
                var events = ParseEventsFromJSON(eventReq.downloadHandler.text);
                if (events == null || events.Count == 0)
                {
                    ShowMessage("Событие не найдено");
                    yield break;
                }
                var ev = events[0];
                if (!ev.is_active || ev.current_deposit < ev.goal)
                {
                    ShowMessage("Событие ещё не завершено");
                    yield break;
                }
            }
            else
            {
                ShowMessage("Ошибка проверки события");
                yield break;
            }
        }

        AccountsManager.Instance.AddPlayerMedal(medalId);

        string updateUrl = $"{supabaseUrl}/rest/v1/{depositsTable}?event_id=eq.{eventId}&player_id=eq.{UnityWebRequest.EscapeURL(playerId)}";
        var updateData = new { claimed = true, claimed_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") };
        string updateJson = JsonUtility.ToJson(updateData);
        using (UnityWebRequest updateReq = new UnityWebRequest(updateUrl, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(updateJson);
            updateReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
            updateReq.downloadHandler = new DownloadHandlerBuffer();
            updateReq.SetRequestHeader("Content-Type", "application/json");
            updateReq.SetRequestHeader("apikey", supabaseKey);
            updateReq.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            updateReq.SetRequestHeader("Prefer", "return=minimal");
            yield return updateReq.SendWebRequest();

            if (updateReq.result != UnityWebRequest.Result.Success)
                Debug.LogError($"Не удалось отметить получение: {updateReq.error}");
        }

        ShowMessage("Вы получили медаль!");
        uiItem.DisableClaimButton();
    }

    private void UpdateEventsUI(List<EventData> events, List<DepositRecord> playerDeposits)
{
    foreach (var item in activeEventItems)
        if (item != null) Destroy(item.gameObject);
    activeEventItems.Clear();

    if (events == null || events.Count == 0) return;

    foreach (var ev in events)
    {
        GameObject itemGO = Instantiate(eventItemPrefab, eventsContainer);
        EventItemUI uiItem = itemGO.GetComponent<EventItemUI>();
        if (uiItem == null) continue;

        Sprite secondaryImg = (ev.event_image_index >= 0 && ev.event_image_index < eventImages.Length) ? eventImages[ev.event_image_index] : null;
        uiItem.Initialize(ev, secondaryImg);

        bool isCompleted = ev.current_deposit >= ev.goal;

        // Deposit видна всегда, пока событие активно и не завершено
        uiItem.SetDepositButtonActive(!isCompleted);
        
        // Claim видна, если событие завершено И игрок внёс хотя бы один депозит
        bool hasDeposit = playerDeposits != null && playerDeposits.Exists(d => d.event_id == ev.id);
        bool hasClaimed = hasDeposit && playerDeposits.Find(d => d.event_id == ev.id).claimed;
        
        uiItem.SetClaimButtonActive(isCompleted && hasDeposit && !hasClaimed);
        uiItem.SetClaimedLabel(isCompleted && hasDeposit && hasClaimed);

        activeEventItems.Add(uiItem);
    }
}

 private IEnumerator CheckAndUpdateClaimButtonCoroutine(EventItemUI uiItem, EventData eventData, string playerId)
{
    string url = $"{supabaseUrl}/rest/v1/{depositsTable}?event_id=eq.{eventData.id}&player_id=eq.{UnityWebRequest.EscapeURL(playerId)}&select=claimed";
    using (UnityWebRequest req = UnityWebRequest.Get(url))
    {
        req.SetRequestHeader("apikey", supabaseKey);
        req.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        yield return req.SendWebRequest();

        bool hasClaimed = false;
        bool hasDeposit = false;
        if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.text != "[]")
        {
            var deposits = ParseDepositsFromJSON(req.downloadHandler.text);
            if (deposits != null && deposits.Count > 0)
            {
                hasDeposit = true;
                hasClaimed = deposits[0].claimed;
            }
        }

        bool isCompleted = eventData.current_deposit >= eventData.goal;
        
        // Deposit всегда активна, если событие не завершено
        uiItem.SetDepositButtonActive(!isCompleted);
        uiItem.SetClaimButtonActive(isCompleted && hasDeposit && !hasClaimed);
        uiItem.SetClaimedLabel(isCompleted && hasDeposit && hasClaimed);
    }
}

    private void OnConfirmDeposit()
    {
        if (currentEventForDeposit == null) return;
        int amount = 50;
        if (depositInputField != null && int.TryParse(depositInputField.text, out int customAmount) && customAmount > 0)
            amount = customAmount;

        depositPanel.SetActive(false);
        StartCoroutine(SubmitDepositCoroutine(currentEventForDeposit.eventId, amount));
        currentEventForDeposit = null;
    }

    private void OnCancelDeposit()
    {
        depositPanel.SetActive(false);
        currentEventForDeposit = null;
    }

    private void ShowMessage(string msg)
    {
        Debug.Log($"[EventsSystem] {msg}");
    }

    private List<EventData> ParseEventsFromJSON(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText) || jsonText == "[]") return new List<EventData>();
        try
        {
            string wrapped = "{\"events\":" + jsonText + "}";
            EventList wrapper = JsonUtility.FromJson<EventList>(wrapped);
            return wrapper?.events ?? new List<EventData>();
        }
        catch (Exception e) { Debug.LogError($"JSON Events error: {e.Message}"); return new List<EventData>(); }
    }

    private List<DepositRecord> ParseDepositsFromJSON(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText) || jsonText == "[]") return new List<DepositRecord>();
        try
        {
            string wrapped = "{\"deposits\":" + jsonText + "}";
            DepositList wrapper = JsonUtility.FromJson<DepositList>(wrapped);
            return wrapper?.deposits ?? new List<DepositRecord>();
        }
        catch (Exception e) { Debug.LogError($"JSON Deposits error: {e.Message}"); return new List<DepositRecord>(); }
    }
}