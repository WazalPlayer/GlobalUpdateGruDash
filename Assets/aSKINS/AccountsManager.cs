using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Text;
using System.Collections;
using Random = UnityEngine.Random;

public class AccountsManager : MonoBehaviour
{
    public static AccountsManager Instance { get; private set; }

    [Header("Supabase Settings")]
    public string supabaseUrl = "https://fiwfzjcceqiaisesmxas.supabase.co";
    public string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZpd2Z6amNjZXFpYWlzZXNteGFzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzE3NjMwNzEsImV4cCI6MjA4NzMzOTA3MX0.v7Qi-gjrTsqXlAD_Zmq_vkfVfQCKpuNgPCyW9aj1pGw";
    public string accountsTable = "accounts";
    
    [Header("Debug")]
    public bool debugMode = true;
    
    private bool isInitialized = false;
    private AccountData currentAccountData;

    [Header("Promo Codes Settings")]
    public string promoTable = "promo_codes";
    public string redemptionsTable = "promo_code_redemptions";

    public void RedeemCode(string code, Action<bool, string, int> callback)
    {
        if (!isInitialized)
        {
            callback?.Invoke(false, "AccountsManager не инициализирован", 0);
            return;
        }
        StartCoroutine(RedeemCodeCoroutine(code, callback));
    }

    private IEnumerator RedeemCodeCoroutine(string code, Action<bool, string, int> callback)
    {
        string playerID = SkinManager.Instance.GetPlayerID();
        
        string getPromoUrl = $"{supabaseUrl}/rest/v1/{promoTable}?code=eq.{UnityWebRequest.EscapeURL(code)}&select=*";
        
        using (UnityWebRequest request = UnityWebRequest.Get(getPromoUrl))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                callback?.Invoke(false, $"Ошибка проверки промокода: {request.error}", 0);
                yield break;
            }
            
            string json = request.downloadHandler.text;
            if (json == "[]" || string.IsNullOrEmpty(json))
            {
                callback?.Invoke(false, "Промокод не найден", 0);
                yield break;
            }
            
            var promoCodes = ParsePromoCodesFromJSON(json);
            if (promoCodes == null || promoCodes.Count == 0)
            {
                callback?.Invoke(false, "Ошибка формата данных промокода", 0);
                yield break;
            }
            
            PromoCodeData promo = promoCodes[0];
            
            if (!string.IsNullOrEmpty(promo.expires_at))
            {
                DateTime expires;
                if (DateTime.TryParse(promo.expires_at, out expires) && expires < DateTime.UtcNow)
                {
                    callback?.Invoke(false, "Срок действия промокода истёк", 0);
                    yield break;
                }
            }
            
            if (promo.max_uses > 0 && promo.used_count >= promo.max_uses)
            {
                callback?.Invoke(false, "Промокод больше недействителен", 0);
                yield break;
            }
            
            string checkRedeemUrl = $"{supabaseUrl}/rest/v1/{redemptionsTable}?promo_code_id=eq.{promo.id}&player_id=eq.{UnityWebRequest.EscapeURL(playerID)}&select=id";
            
            using (UnityWebRequest redeemCheckRequest = UnityWebRequest.Get(checkRedeemUrl))
            {
                redeemCheckRequest.SetRequestHeader("apikey", supabaseKey);
                redeemCheckRequest.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
                yield return redeemCheckRequest.SendWebRequest();
                
                if (redeemCheckRequest.result == UnityWebRequest.Result.Success)
                {
                    string redeemJson = redeemCheckRequest.downloadHandler.text;
                    if (redeemJson != "[]" && !string.IsNullOrEmpty(redeemJson))
                    {
                        callback?.Invoke(false, "Вы уже активировали этот промокод", 0);
                        yield break;
                    }
                }
                else
                {
                    Debug.LogWarning($"Не удалось проверить использование промокода: {redeemCheckRequest.error}");
                    callback?.Invoke(false, "Ошибка проверки использования", 0);
                    yield break;
                }
            }
            
            string updatePromoUrl = $"{supabaseUrl}/rest/v1/{promoTable}?id=eq.{promo.id}";
            var updateData = new { used_count = promo.used_count + 1 };
            string updateJson = JsonUtility.ToJson(updateData);
            
            using (UnityWebRequest updateRequest = new UnityWebRequest(updatePromoUrl, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(updateJson);
                updateRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                updateRequest.downloadHandler = new DownloadHandlerBuffer();
                updateRequest.SetRequestHeader("Content-Type", "application/json");
                updateRequest.SetRequestHeader("apikey", supabaseKey);
                updateRequest.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
                updateRequest.SetRequestHeader("Prefer", "return=minimal");
                yield return updateRequest.SendWebRequest();
                if (updateRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Не удалось обновить счётчик промокода: {updateRequest.error}");
                    callback?.Invoke(false, "Ошибка активации", 0);
                    yield break;
                }
            }
            
            string insertRedeemUrl = $"{supabaseUrl}/rest/v1/{redemptionsTable}";
            var redemptionData = new RedemptionInsertData
            {
                promo_code_id = promo.id,
                player_id = playerID,
                redeemed_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
            string insertRedeemJson = JsonUtility.ToJson(redemptionData);
            
            using (UnityWebRequest insertRequest = new UnityWebRequest(insertRedeemUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(insertRedeemJson);
                insertRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                insertRequest.downloadHandler = new DownloadHandlerBuffer();
                insertRequest.SetRequestHeader("Content-Type", "application/json");
                insertRequest.SetRequestHeader("apikey", supabaseKey);
                insertRequest.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
                insertRequest.SetRequestHeader("Prefer", "return=minimal");
                yield return insertRequest.SendWebRequest();
                if (insertRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Не удалось записать использование: {insertRequest.error}\nОтвет: {insertRequest.downloadHandler?.text}");
                    callback?.Invoke(false, "Ошибка записи активации", 0);
                    yield break;
                }
            }
            
            AddPlayerStars(promo.stars);
            if (promo.medal_id == -2) // специальное значение "все медали"
{
    // Добавляем все медали, которые есть в MedalsManager
    int totalMedals = MedalsManager.Instance.medalSprites.Length;
    for (int i = 0; i < totalMedals; i++)
        AddPlayerMedal(i);
}
else if (promo.medal_id >= 0)
{
    AddPlayerMedal(promo.medal_id);
}
            SaveAccountData();
            callback?.Invoke(true, "Промокод успешно активирован", promo.stars);
        }
    }

    [System.Serializable]
    private class PromoCodeList { public List<PromoCodeData> items; }

    private List<PromoCodeData> ParsePromoCodesFromJSON(string jsonText)
    {
        List<PromoCodeData> list = new List<PromoCodeData>();
        try
        {
            string wrapped = "{\"items\":" + jsonText + "}";
            PromoCodeList wrapper = JsonUtility.FromJson<PromoCodeList>(wrapped);
            if (wrapper != null && wrapper.items != null) list = wrapper.items;
        }
        catch (Exception e) { Debug.LogError($"Ошибка парсинга промокодов: {e.Message}"); }
        return list;
    }

    public event Action<bool, string> OnAccountOperationComplete;
    public event Action<List<AccountData>> OnAccountsLoaded;
    public event Action<List<AccountData>> OnTopPlayersLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            isInitialized = true;
            StartCoroutine(LoadAccountWhenReady());
            Debug.Log("✅ AccountsManager инициализирован");
        }
        else Destroy(gameObject);
    }

    private IEnumerator LoadAccountWhenReady()
    {
        while (SkinManager.Instance == null) yield return null;
        yield return null;
        LoadAccountData();
    }

    public void LoadTopsPlayer(int limit = 1000, bool descending = true)
    {
        if (!isInitialized)
        {
            OnTopPlayersLoaded?.Invoke(new List<AccountData>());
            return;
        }
        StartCoroutine(LoadTopsPlayerCoroutine(limit, descending));
    }

    private IEnumerator LoadTopsPlayerCoroutine(int limit, bool descending)
    {
        string orderDirection = descending ? "desc" : "asc";
        string url = $"{supabaseUrl}/rest/v1/{accountsTable}?select=*&order=stars.{orderDirection},player_name.asc&limit={limit}";
        if (debugMode) Debug.Log($"🏆 Загружаем топ игроков: {url}");
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonText = request.downloadHandler.text;
                var accounts = ParseAccountsFromJSON(jsonText);
                OnTopPlayersLoaded?.Invoke(accounts ?? new List<AccountData>());
            }
            else
            {
                Debug.LogError($"❌ Ошибка загрузки топа: {request.error}");
                OnTopPlayersLoaded?.Invoke(new List<AccountData>());
            }
        }
    }

    public int GetPlayerStars() => PlayerPrefs.GetInt("PlayerStars", 0);
    public int GetPlayerDemons() => PlayerPrefs.GetInt("PlayerDemons", 0);
    public int GetPlayerBackground() => PlayerPrefs.GetInt("PlayerBackground", 0);
    public void SetPlayerBackground(int backgroundIndex)
    {
        PlayerPrefs.SetInt("PlayerBackground", backgroundIndex);
        PlayerPrefs.Save();
        if (currentAccountData != null) currentAccountData.background = backgroundIndex;
    }
    public void SetPlayerStars(int stars)
    {
        PlayerPrefs.SetInt("PlayerStars", stars);
        PlayerPrefs.Save();
        if (currentAccountData != null) currentAccountData.stars = stars;
    }
    public void SetPlayerDemons(int demons)
    {
        PlayerPrefs.SetInt("PlayerDemons", demons);
        PlayerPrefs.Save();
        if (currentAccountData != null) currentAccountData.demons = demons;
    }
    public void AddPlayerStars(int amount)
    {
        int currentStars = GetPlayerStars();
        SetPlayerStars(currentStars + amount);
        if (debugMode) Debug.Log($"⭐ Добавлено {amount} звёзд. Всего: {currentStars + amount}");
    }
    public void AddPlayerDemon(string demonName)
    {
        string demonKey = $"Demon_{demonName}";
        if (PlayerPrefs.HasKey(demonKey)) return;
        PlayerPrefs.SetInt(demonKey, 1);
        int currentDemons = GetPlayerDemons();
        SetPlayerDemons(currentDemons + 1);
        if (currentAccountData != null) SaveAccountData();
        Debug.Log($"👹 Добавлен демон: {demonName}. Всего демонов: {currentDemons + 1}");
    }
    
    // НОВЫЕ МЕТОДЫ ДЛЯ МЕДАЛЕЙ
    public int GetPlayerMedals() => PlayerPrefs.GetInt("PlayerMedals", 0);
    public void SetPlayerMedals(int medals)
    {
        PlayerPrefs.SetInt("PlayerMedals", medals);
        PlayerPrefs.Save();
        if (currentAccountData != null) currentAccountData.medals = medals;
    }
    public void AddPlayerMedal(int medalId)
    {
        if (medalId < 0) return;
        int current = GetPlayerMedals();
        int newMask = current | (1 << medalId);
        if (newMask != current)
        {
            SetPlayerMedals(newMask);
            Debug.Log($"🏅 Добавлена медаль ID {medalId}. Новая маска: {newMask}");
        }
    }

    public AccountData GetCurrentAccount() => currentAccountData;

    public void SaveAccountData()
    {
        if (!isInitialized || SkinManager.Instance == null)
        {
            OnAccountOperationComplete?.Invoke(false, "AccountsManager не готов");
            return;
        }
        StartCoroutine(SaveAccountDataCoroutine());
    }

    private IEnumerator SaveAccountDataCoroutine()
    {
        var skins = SkinManager.Instance.GetAllSelectedSkins();
        var accountData = new AccountData
        {
            player_id = SkinManager.Instance.GetPlayerID(),
            player_name = SkinManager.Instance.GetPlayerName(),
            cube_skin = skins.ContainsKey("Cube") ? skins["Cube"] : 0,
            ship_skin = skins.ContainsKey("Ship") ? skins["Ship"] : 0,
            wave_skin = skins.ContainsKey("Wave") ? skins["Wave"] : 0,
            ufo_skin = skins.ContainsKey("UFO") ? skins["UFO"] : 0,
            spider_skin = skins.ContainsKey("Spider") ? skins["Spider"] : 0,
            ball_skin = skins.ContainsKey("Ball") ? skins["Ball"] : 0,
            stars = GetPlayerStars(),
            demons = GetPlayerDemons(),
            is_admin = false,
            total_levels_created = 0,
            total_likes_received = 0,
            player_level = 1,
            experience = 0,
            background = GetPlayerBackground(),
            medals = GetPlayerMedals()
        };

        if (currentAccountData != null && currentAccountData.id > 0)
        {
            accountData.id = currentAccountData.id;
            accountData.is_admin = currentAccountData.is_admin;
            accountData.created_at = currentAccountData.created_at;
            accountData.total_levels_created = currentAccountData.total_levels_created;
            accountData.total_likes_received = currentAccountData.total_likes_received;
            accountData.experience = currentAccountData.experience;
            accountData.player_level = currentAccountData.player_level;
            accountData.updated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            yield return UpdateAccountByIdCoroutine(accountData);
        }
        else
        {
            accountData.created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            accountData.updated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            yield return CreateAccountCoroutine(accountData);
        }
    }

    private IEnumerator CreateAccountCoroutine(AccountData accountData)
    {
        var createData = new AccountDataCreate
        {
            player_id = accountData.player_id,
            player_name = accountData.player_name,
            cube_skin = accountData.cube_skin,
            ship_skin = accountData.ship_skin,
            wave_skin = accountData.wave_skin,
            ufo_skin = accountData.ufo_skin,
            ball_skin = accountData.ball_skin,
            spider_skin = accountData.spider_skin,
            stars = accountData.stars,
            demons = accountData.demons,
            is_admin = accountData.is_admin,
            created_at = accountData.created_at,
            updated_at = accountData.updated_at,
            total_levels_created = accountData.total_levels_created,
            total_likes_received = accountData.total_likes_received,
            player_level = accountData.player_level,
            experience = accountData.experience,
            background = accountData.background,
            medals = accountData.medals
        };
        string jsonData = JsonUtility.ToJson(createData);
        string url = $"{supabaseUrl}/rest/v1/{accountsTable}";
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Prefer", "return=representation");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = ParseAccountsFromJSON(request.downloadHandler.text);
                if (response != null && response.Count > 0)
                {
                    currentAccountData = response[0];
                    OnAccountOperationComplete?.Invoke(true, "Аккаунт успешно создан!");
                }
            }
            else
            {
                if (request.responseCode == 409)
                {
                    Debug.LogWarning("Аккаунт уже существует, загружаем...");
                    yield return LoadAccountDataCoroutine(accountData.player_id);
                    if (currentAccountData != null) yield return SaveAccountDataCoroutine();
                    else OnAccountOperationComplete?.Invoke(false, "Ошибка восстановления аккаунта");
                }
                else
                {
                    Debug.LogError($"Ошибка создания аккаунта: {request.error}");
                    OnAccountOperationComplete?.Invoke(false, $"Ошибка создания: {request.error}");
                }
            }
        }
    }

    private IEnumerator UpdateAccountByIdCoroutine(AccountData accountData)
    {
        var updateData = new AccountDataUpdate
        {
            player_name = accountData.player_name,
            cube_skin = accountData.cube_skin,
            ship_skin = accountData.ship_skin,
            wave_skin = accountData.wave_skin,
            ufo_skin = accountData.ufo_skin,
            ball_skin = accountData.ball_skin,
            spider_skin = accountData.spider_skin,
            stars = accountData.stars,
            demons = accountData.demons,
            updated_at = accountData.updated_at,
            total_levels_created = accountData.total_levels_created,
            total_likes_received = accountData.total_likes_received,
            player_level = accountData.player_level,
            experience = accountData.experience,
            background = accountData.background,
            medals = accountData.medals
        };
        string jsonData = JsonUtility.ToJson(updateData);
        string url = $"{supabaseUrl}/rest/v1/{accountsTable}?id=eq.{accountData.id}";
        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Prefer", "return=minimal");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                currentAccountData.player_name = accountData.player_name;
                currentAccountData.cube_skin = accountData.cube_skin;
                currentAccountData.ship_skin = accountData.ship_skin;
                currentAccountData.wave_skin = accountData.wave_skin;
                currentAccountData.ufo_skin = accountData.ufo_skin;
                currentAccountData.spider_skin = accountData.spider_skin;
                currentAccountData.ball_skin = accountData.ball_skin;
                currentAccountData.stars = accountData.stars;
                currentAccountData.demons = accountData.demons;
                currentAccountData.updated_at = accountData.updated_at;
                currentAccountData.total_levels_created = accountData.total_levels_created;
                currentAccountData.total_likes_received = accountData.total_likes_received;
                currentAccountData.player_level = accountData.player_level;
                currentAccountData.experience = accountData.experience;
                currentAccountData.background = accountData.background;
                currentAccountData.medals = accountData.medals;
                OnAccountOperationComplete?.Invoke(true, "Аккаунт успешно обновлен!");
            }
            else
            {
                Debug.LogError($"Ошибка обновления: {request.error}");
                OnAccountOperationComplete?.Invoke(false, $"Ошибка обновления: {request.error}");
            }
        }
    }

    public void LoadAccountData()
    {
        if (!isInitialized || SkinManager.Instance == null) return;
        string playerID = SkinManager.Instance.GetPlayerID();
        if (string.IsNullOrEmpty(playerID)) return;
        StartCoroutine(LoadAccountDataCoroutine(playerID));
    }

    private IEnumerator LoadAccountDataCoroutine(string playerID)
    {
        string escapedPlayerID = UnityWebRequest.EscapeURL(playerID);
        string url = $"{supabaseUrl}/rest/v1/{accountsTable}?player_id=eq.{escapedPlayerID}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonText = request.downloadHandler.text;
                var accounts = ParseAccountsFromJSON(jsonText);
                if (accounts != null && accounts.Count > 0)
                {
                    currentAccountData = accounts[0];
                    SetPlayerStars(currentAccountData.stars);
                    SetPlayerDemons(currentAccountData.demons);
                    SetPlayerBackground(currentAccountData.background);
                    SetPlayerMedals(currentAccountData.medals);
                    SkinManager.Instance?.LoadSkinsFromAccount(currentAccountData);
                }
                else currentAccountData = null;
            }
        }
    }

    public void SearchAccountsByName(string searchQuery)
    {
        if (!isInitialized || string.IsNullOrEmpty(searchQuery))
        {
            OnAccountsLoaded?.Invoke(new List<AccountData>());
            return;
        }
        StartCoroutine(SearchAccountsCoroutine(searchQuery));
    }

    private IEnumerator SearchAccountsCoroutine(string searchQuery)
    {
        string likePattern = $"%{searchQuery}%";
        string encodedPattern = UnityWebRequest.EscapeURL(likePattern);
        string url = $"{supabaseUrl}/rest/v1/{accountsTable}?select=*&player_name=ilike.{encodedPattern}&order=player_name&limit=20";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var accounts = ParseAccountsFromJSON(request.downloadHandler.text);
                OnAccountsLoaded?.Invoke(accounts ?? new List<AccountData>());
            }
            else OnAccountsLoaded?.Invoke(new List<AccountData>());
        }
    }

    public void GetTopPlayers(int limit = 100) => LoadTopsPlayer(limit, true);

    public void GetRandomAccount(Action<AccountData> callback)
    {
        if (!isInitialized) { callback?.Invoke(null); return; }
        StartCoroutine(GetRandomAccountCoroutine(callback));
    }

 private IEnumerator GetRandomAccountCoroutine(Action<AccountData> callback)
{
    string countUrl = $"{supabaseUrl}/rest/v1/{accountsTable}?select=count";
    using (UnityWebRequest countRequest = UnityWebRequest.Get(countUrl))
    {
        countRequest.SetRequestHeader("apikey", supabaseKey);
        countRequest.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        yield return countRequest.SendWebRequest();

        if (countRequest.result != UnityWebRequest.Result.Success)
        {
            callback?.Invoke(null);
            yield break;
        }

        string countJson = countRequest.downloadHandler.text;
        int totalCount = 0;
        try
        {
            var countData = JsonUtility.FromJson<CountResponse>("{\"array\":" + countJson + "}");
            if (countData?.array?.Length > 0)
                totalCount = countData.array[0].count;
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка парсинга количества: {e.Message}");
            callback?.Invoke(null);
            yield break;
        }

        if (totalCount <= 0)
        {
            callback?.Invoke(null);
            yield break;
        }

        int randomOffset = Random.Range(0, Mathf.Min(totalCount, 100));
        string url = $"{supabaseUrl}/rest/v1/{accountsTable}?select=*&offset={randomOffset}&limit=1";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var accounts = ParseAccountsFromJSON(request.downloadHandler.text);
                callback?.Invoke(accounts != null && accounts.Count > 0 ? accounts[0] : null);
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }
}

    private List<AccountData> ParseAccountsFromJSON(string jsonText)
    {
        List<AccountData> accounts = new List<AccountData>();
        try
        {
            if (string.IsNullOrEmpty(jsonText) || jsonText == "[]") return accounts;
            jsonText = jsonText.Trim();
            if (!jsonText.StartsWith("[")) jsonText = "[" + jsonText + "]";
            string wrapped = "{\"accounts\":" + jsonText + "}";
            AccountList wrapper = JsonUtility.FromJson<AccountList>(wrapped);
            if (wrapper?.accounts != null) accounts = wrapper.accounts;
            else accounts = ManualParseAccounts(jsonText);
        }
        catch (Exception e) { Debug.LogError($"Ошибка парсинга JSON: {e.Message}"); accounts = ManualParseAccounts(jsonText); }
        return accounts;
    }

    private List<AccountData> ManualParseAccounts(string jsonText)
    {
        List<AccountData> accounts = new List<AccountData>();
        try
        {
            if (string.IsNullOrEmpty(jsonText) || jsonText == "[]") return accounts;
            if (jsonText.StartsWith("[") && jsonText.EndsWith("]"))
                jsonText = jsonText.Substring(1, jsonText.Length - 2);
            int depth = 0;
            StringBuilder obj = new StringBuilder();
            foreach (char c in jsonText)
            {
                if (c == '{') { depth++; obj.Append(c); }
                else if (c == '}') { depth--; obj.Append(c); if (depth == 0) { accounts.Add(ParseSingleAccount(obj.ToString())); obj.Clear(); } }
                else if (depth > 0) obj.Append(c);
            }
        }
        catch (Exception e) { Debug.LogError($"Ручной парсинг ошибка: {e.Message}"); }
        return accounts;
    }

    private AccountData ParseSingleAccount(string jsonObject)
    {
        AccountData acc = new AccountData();
        try
        {
            string wrapped = "{\"account\":" + jsonObject + "}";
            SingleAccountWrapper wrapper = JsonUtility.FromJson<SingleAccountWrapper>(wrapped);
            if (wrapper?.account != null) return wrapper.account;
            string[] pairs = jsonObject.Replace("{", "").Replace("}", "").Split(',');
            foreach (string pair in pairs)
            {
                string[] kv = pair.Split(new[] { ':' }, 2);
                if (kv.Length != 2) continue;
                string key = kv[0].Trim().Trim('"');
                string val = kv[1].Trim().Trim('"');
                switch (key)
                {
                    case "id": int.TryParse(val, out acc.id); break;
                    case "player_id": acc.player_id = val; break;
                    case "player_name": acc.player_name = val; break;
                    case "cube_skin": int.TryParse(val, out acc.cube_skin); break;
                    case "ship_skin": int.TryParse(val, out acc.ship_skin); break;
                    case "wave_skin": int.TryParse(val, out acc.wave_skin); break;
                    case "ball_skin": int.TryParse(val, out acc.ball_skin); break;
                    case "ufo_skin": int.TryParse(val, out acc.ufo_skin); break;
                    case "spider_skin": int.TryParse(val, out acc.spider_skin); break;
                    case "stars": int.TryParse(val, out acc.stars); break;
                    case "demons": int.TryParse(val, out acc.demons); break;
                    case "creators": int.TryParse(val, out acc.creators); break;
                    case "is_admin": bool.TryParse(val, out acc.is_admin); break;
                    case "total_levels_created": int.TryParse(val, out acc.total_levels_created); break;
                    case "total_likes_received": int.TryParse(val, out acc.total_likes_received); break;
                    case "player_level": int.TryParse(val, out acc.player_level); break;
                    case "experience": int.TryParse(val, out acc.experience); break;
                    case "created_at": acc.created_at = val; break;
                    case "updated_at": acc.updated_at = val; break;
                    case "background": int.TryParse(val, out acc.background); break;
                    case "medals": int.TryParse(val, out acc.medals); break;
                }
            }
        }
        catch (Exception e) { Debug.LogError($"Ошибка парсинга одиночного аккаунта: {e.Message}"); }
        return acc;
    }

    [System.Serializable] private class RedemptionInsertData { public long promo_code_id; public string player_id; public string redeemed_at; }
    [System.Serializable] private class PromoCodeData { public long id; public string code; public int stars; public int max_uses; public int used_count; public string expires_at; public int medal_id; }
    [System.Serializable] private class AccountList { public List<AccountData> accounts; }
    [System.Serializable] private class SingleAccountWrapper { public AccountData account; }
    [System.Serializable] private class CountResponse { public CountItem[] array; }
    [System.Serializable] private class CountItem { public int count; }
}

[System.Serializable]
public class AccountData
{
    public int id;
    public string player_id;
    public string player_name;
    public int cube_skin;
    public int ship_skin;
    public int wave_skin;
    public int ufo_skin;
    public int spider_skin;
    public int ball_skin;
    public int stars;
    public bool is_admin;
    public string created_at;
    public string updated_at;
    public int total_levels_created;
    public int total_likes_received;
    public int player_level;
    public int experience;
    public int demons;
    public int creators;
    public int background;
    public int medals;  // НОВОЕ
    public AccountData()
    {
        cube_skin = 0; ship_skin = 0; wave_skin = 0; ufo_skin = 0; spider_skin = 0; ball_skin = 0;
        stars = 0; demons = 0; is_admin = false; total_levels_created = 0; total_likes_received = 0;
        player_level = 1; experience = 0; background = 0; medals = 0;
    }
}

[System.Serializable]
public class AccountDataCreate
{
    public string player_id; public string player_name; public int cube_skin; public int ship_skin; public int wave_skin;
    public int ufo_skin; public int spider_skin; public int ball_skin; public int stars; public int demons;
    public bool is_admin; public string created_at; public string updated_at; public int total_levels_created;
    public int total_likes_received; public int player_level; public int experience; public int background; public int medals;
}

[System.Serializable]
public class AccountDataUpdate
{
    public string player_name; public int cube_skin; public int ship_skin; public int wave_skin; public int ufo_skin;
    public int spider_skin; public int ball_skin; public int stars; public int demons; public string updated_at;
    public int total_levels_created; public int total_likes_received; public int player_level; public int experience;
    public int background; public int medals;
}