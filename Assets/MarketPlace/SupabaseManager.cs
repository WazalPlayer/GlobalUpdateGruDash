using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseManager : MonoBehaviour
{
    public static SupabaseManager Instance;
    
    [Header("Supabase Settings")]
    // ИСПРАВЬТЕ URL НА ТОТ, КОТОРЫЙ РАБОТАЕТ (nrkwqjuavktjmfalvigc)
    public string supabaseUrl = "https://nrkwqjuavktjmfalvigc.supabase.co";
    public string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5ya3dxanVhdmt0am1mYWx2aWdjIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzQ2MjQ2MzgsImV4cCI6MjA1MDIwMDYzOH0.6aK_U2x_8Pe4W1pPwymhKJXp2xE6g1QdFcB5kqJWR8E";
    public string levelsTable = "levels";
    
    [Header("Debug")]
    public bool debugMode = true;
    
    private string playerId;
    
    public string PlayerId => playerId;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Генерируем ID игрока
            playerId = PlayerPrefs.GetString("PlayerID", "");
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("PlayerID", playerId);
                PlayerPrefs.Save();
            }
            
            if (debugMode) Debug.Log($"🎮 Player ID: {playerId}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 👤 СОЗДАТЬ ГОСТЕВОЙ АККАУНТ В ТАБЛИЦЕ accounts (с данными из игры)
    public void CreateGuestAccount(string playerName, Action<bool, string> callback)
    {
        StartCoroutine(CreateGuestAccountCoroutine(playerName, callback));
    }

    [Serializable]
    private class GuestAccountRow
    {
        public string player_id;
        public string player_name;
        public int stars;
        public int demons;
        public bool is_admin;
        public string created_at;
        public string updated_at;
    }

    private IEnumerator CreateGuestAccountCoroutine(string playerName, Action<bool, string> callback)
    {
        string now = DateTime.UtcNow.ToString("o");
        var row = new GuestAccountRow
        {
            player_id = playerId,
            player_name = string.IsNullOrEmpty(playerName) ? "Гость" : playerName,
            stars = PlayerPrefs.GetInt("PlayerStars", 0),
            demons = PlayerPrefs.GetInt("PlayerDemons", 0),
            is_admin = false,
            created_at = now,
            updated_at = now
        };
        string jsonData = JsonUtility.ToJson(row);

        // on_conflict: если аккаунт с таким player_id уже есть, не создаём дубль
        string url = $"{supabaseUrl}/rest/v1/accounts?on_conflict=player_id";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Prefer", "resolution=ignore-duplicates,return=minimal");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (debugMode) Debug.Log("✅ Гостевой аккаунт создан в Supabase");
                callback?.Invoke(true, null);
            }
            else
            {
                Debug.LogError($"❌ Ошибка создания гостевого аккаунта: {request.error} {request.downloadHandler.text}");
                callback?.Invoke(false, request.error);
            }
        }
    }

    // 📤 ЗАГРУЗИТЬ УРОВЕНЬ (БЕЗ УКАЗАНИЯ ID)
    public void UploadLevel(SupabaseLevelData levelData, System.Action<bool, string> callback = null)
    {
        StartCoroutine(UploadLevelCoroutine(levelData, callback));
    }
    
    [System.Serializable]
    private class UploadLevelRequest
    {
        public string level_id;           // ОСТАВЛЯЕМ - это GUID
        public string level_name;
        public string author;
        public string author_id;
        public string description;
        public string level_data;
        public float difficulty;
        public int downloads;
        public int likes;
        public int version;
        // УБРАЛИ ПОЛЕ 'id' - оно будет генерироваться автоматически в базе данных
    }
    
    private IEnumerator UploadLevelCoroutine(SupabaseLevelData levelData, System.Action<bool, string> callback)
    {
        // Генерируем новый GUID для level_id если его нет
        if (string.IsNullOrEmpty(levelData.level_id))
        {
            levelData.level_id = System.Guid.NewGuid().ToString();
        }
        
        // Создаем объект для сериализации БЕЗ поля 'id'
        var uploadData = new UploadLevelRequest
        {
            level_id = levelData.level_id,
            level_name = levelData.level_name,
            author = levelData.author,
            author_id = levelData.author_id,
            description = levelData.description,
            level_data = levelData.level_data,
            difficulty = levelData.difficulty,
            downloads = levelData.downloads,
            likes = levelData.likes,
            version = levelData.version
        };
        
        string jsonData = JsonUtility.ToJson(uploadData);
        
        // Проверяем что JSON не пустой
        if (string.IsNullOrEmpty(jsonData) || jsonData == "{}")
        {
            Debug.LogError("❌ Пустой JSON данные!");
            callback?.Invoke(false, "Ошибка: пустые данные уровня");
            yield break;
        }
        
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}";
        
        if (debugMode) Debug.Log($"📤 Отправляем уровень (без ID): {jsonData}");
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Prefer", "return=minimal");
            // Добавляем User-Agent для Supabase
            request.SetRequestHeader("User-Agent", "UnityPlayer/2021.3.32f1 (UnityWebRequest/1.0, libcurl/7.80.0-DEV)");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (debugMode) Debug.Log("✅ Уровень загружен в Supabase!");
                callback?.Invoke(true, "Уровень опубликован!");
            }
            else
            {
                Debug.LogError($"❌ Ошибка загрузки: {request.error}");
                Debug.LogError($"Статус код: {request.responseCode}");
                Debug.LogError($"Ответ: {request.downloadHandler.text}");
                callback?.Invoke(false, $"Ошибка: {request.error}");
            }
        }
    }
    
    // 📥 ПОЛУЧИТЬ ВСЕ УРОВНИ
    public void GetLevels(System.Action<List<SupabaseLevelData>> callback)
    {
        StartCoroutine(GetLevelsCoroutine(callback));
    }

    // 📄 СТРАНИЦА УРОВНЕЙ (offset/limit), новые сверху
    public void GetLevelsPage(int offset, int limit, System.Action<List<SupabaseLevelData>> callback)
    {
        StartCoroutine(GetLevelsPageCoroutine(offset, limit, callback));
    }

    private IEnumerator GetLevelsPageCoroutine(int offset, int limit, System.Action<List<SupabaseLevelData>> callback)
    {
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}?select=*&order=created_at.desc&offset={offset}&limit={limit}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(ParseLevelsFromJSON(request.downloadHandler.text));
            }
            else
            {
                Debug.LogError($"❌ Ошибка загрузки страницы уровней: {request.error}");
                callback?.Invoke(new List<SupabaseLevelData>());
            }
        }
    }
    
    private IEnumerator GetLevelsCoroutine(System.Action<List<SupabaseLevelData>> callback)
    {
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}?select=*&order=created_at.desc";
        
        if (debugMode) Debug.Log($"📥 Загружаем уровни: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (debugMode) Debug.Log($"✅ Уровни загружены: {request.downloadHandler.text}");
                List<SupabaseLevelData> levels = ParseLevelsFromJSON(request.downloadHandler.text);
                callback?.Invoke(levels);
            }
            else
            {
                Debug.LogError($"❌ Ошибка загрузки уровней: {request.error}");
                Debug.LogError($"Ответ: {request.downloadHandler.text}");
                callback?.Invoke(new List<SupabaseLevelData>());
            }
        }
    }
    
// 🔍 ПОИСК УРОВНЕЙ
public void SearchLevels(string searchTerm, System.Action<List<SupabaseLevelData>> callback)
{
    if (string.IsNullOrEmpty(searchTerm))
    {
        GetLevels(callback);
        return;
    }
    StartCoroutine(SearchLevelsCoroutine(searchTerm, callback));
}

private bool IsGuid(string input)
{
    if (string.IsNullOrWhiteSpace(input)) return false;
    input = input.Trim();
    // GUID может быть в формате с дефисами или без, но Supabase хранит с дефисами
    return Guid.TryParse(input, out _);
}

private IEnumerator SearchLevelsCoroutine(string searchTerm, System.Action<List<SupabaseLevelData>> callback)
{
    // Очищаем поисковый запрос
    string cleanTerm = searchTerm?.Trim() ?? "";
    
    string url;
    
    if (IsGuid(cleanTerm))
    {
        // Точный поиск по level_id (поле типа uuid)
        string escapedGuid = UnityWebRequest.EscapeURL(cleanTerm);
        url = $"{supabaseUrl}/rest/v1/{levelsTable}?select=*&level_id=eq.{escapedGuid}";
        if (debugMode) Debug.Log($"🔍 Поиск по level_id GUID: {url}");
    }
    else if (!string.IsNullOrEmpty(cleanTerm))
    {
        // Обычный поиск по имени и автору
        string likePattern = $"%{cleanTerm}%";
        string encodedPattern = UnityWebRequest.EscapeURL(likePattern);
        url = $"{supabaseUrl}/rest/v1/{levelsTable}?select=*" +
              $"&or=(level_name.ilike.\"{encodedPattern}\",author.ilike.\"{encodedPattern}\")" +
              "&order=created_at.desc";
        if (debugMode) Debug.Log($"🔍 Поиск по тексту: {url}");
    }
else
{
    // Пустой поиск - загружаем все уровни
    yield return StartCoroutine(GetLevelsCoroutine(callback));
    yield break;
}
    
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        request.SetRequestHeader("apikey", supabaseKey);
        request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            if (debugMode) Debug.Log($"✅ Результаты поиска: {request.downloadHandler.text}");
            List<SupabaseLevelData> levels = ParseLevelsFromJSON(request.downloadHandler.text);
            callback?.Invoke(levels);
        }
        else
        {
            Debug.LogError($"❌ Ошибка поиска: {request.error}");
            Debug.LogError($"URL запроса: {url}");
            Debug.LogError($"Ответ сервера: {request.downloadHandler.text}");
            callback?.Invoke(new List<SupabaseLevelData>());
        }
    }
}    
    // ⭐ ДОБАВИТЬ ЛАЙК
    public void LikeLevel(string levelId, System.Action<bool> callback = null)
    {
        StartCoroutine(LikeLevelCoroutine(levelId, callback));
    }
    
    private IEnumerator LikeLevelCoroutine(string levelId, System.Action<bool> callback)
    {
        // Сначала получаем текущее количество лайков
        yield return StartCoroutine(GetLevelCoroutine(levelId, (level) => {
            if (level != null)
            {
                StartCoroutine(UpdateLevelLikesCoroutine(levelId, level.likes + 1, callback));
            }
            else
            {
                callback?.Invoke(false);
            }
        }));
    }
    
    // 📊 ОБНОВИТЬ СКАЧИВАНИЯ
    public void IncrementDownloads(string levelId)
    {
        StartCoroutine(IncrementDownloadsCoroutine(levelId));
    }
    
    private IEnumerator IncrementDownloadsCoroutine(string levelId)
    {
        yield return StartCoroutine(GetLevelCoroutine(levelId, (level) => {
            if (level != null)
            {
                StartCoroutine(UpdateLevelDownloadsCoroutine(levelId, level.downloads + 1));
            }
        }));
    }
    
    // 🗑️ УДАЛИТЬ УРОВЕНЬ
    public void DeleteLevel(string levelId, System.Action<bool> callback = null)
    {
        StartCoroutine(DeleteLevelCoroutine(levelId, callback));
    }
    
    private IEnumerator DeleteLevelCoroutine(string levelId, System.Action<bool> callback)
    {
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}?level_id=eq.{levelId}";
        
        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (debugMode) Debug.Log("✅ Уровень удален!");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError($"❌ Ошибка удаления: {request.error}");
                callback?.Invoke(false);
            }
        }
    }
    
    // 🎯 ПОЛУЧИТЬ КОНКРЕТНЫЙ УРОВЕНЬ
    private IEnumerator GetLevelCoroutine(string levelId, System.Action<SupabaseLevelData> callback)
    {
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}?level_id=eq.{levelId}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var levels = ParseLevelsFromJSON(request.downloadHandler.text);
                callback?.Invoke(levels.Count > 0 ? levels[0] : null);
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }
    
    // 🔄 ОБНОВИТЬ ЛАЙКИ
    private IEnumerator UpdateLevelLikesCoroutine(string levelId, int newLikes, System.Action<bool> callback)
    {
        string jsonData = $@"{{""likes"": {newLikes}}}";
        
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}?level_id=eq.{levelId}";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Prefer", "return=minimal");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true);
            }
            else
            {
                callback?.Invoke(false);
            }
        }
    }
    
    // 🔄 ОБНОВИТЬ СКАЧИВАНИЯ
    private IEnumerator UpdateLevelDownloadsCoroutine(string levelId, int newDownloads)
    {
        string jsonData = $@"{{""downloads"": {newDownloads}}}";
        
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}?level_id=eq.{levelId}";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Prefer", "return=minimal");
            
            yield return request.SendWebRequest();
        }
    }
    
    // 📝 ПАРСИНГ JSON ОТВЕТА
    private List<SupabaseLevelData> ParseLevelsFromJSON(string jsonText)
    {
        List<SupabaseLevelData> levels = new List<SupabaseLevelData>();
        
        try
        {
            if (string.IsNullOrEmpty(jsonText) || jsonText == "[]") return levels;
            
            // Используем JsonUtility для правильного парсинга
            string wrappedJson = "{\"levels\":" + jsonText + "}";
            SupabaseLevelList levelList = JsonUtility.FromJson<SupabaseLevelList>(wrappedJson);
            
            if (levelList != null && levelList.levels != null)
            {
                levels = levelList.levels;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Ошибка парсинга JSON: {e.Message}");
            Debug.LogError($"JSON: {jsonText}");
            
            // Fallback: ручной парсинг
            levels = ManualParseLevels(jsonText);
        }
        
        return levels;
    }


private List<SupabaseLevelData> ManualParseLevels(string jsonText)
{
    List<SupabaseLevelData> levels = new List<SupabaseLevelData>();
    
    try
    {
        // Упрощенный ручной парсинг
        if (string.IsNullOrEmpty(jsonText) || jsonText == "[]") return levels;
        
        string cleanJson = jsonText.Trim();
        if (cleanJson.StartsWith("[") && cleanJson.EndsWith("]"))
        {
            cleanJson = cleanJson.Substring(1, cleanJson.Length - 2);
        }
        
        string[] objects = cleanJson.Split(new[] { "}," }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string obj in objects)
        {
            string cleanObj = obj.Trim();
            if (cleanObj.StartsWith("{")) cleanObj = cleanObj.Substring(1);
            if (cleanObj.EndsWith("}")) cleanObj = cleanObj.Substring(0, cleanObj.Length - 1);
            
            SupabaseLevelData level = new SupabaseLevelData();
            
            string[] fields = cleanObj.Split(',');
            foreach (string field in fields)
            {
                string[] parts = field.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim().Trim('"');
                    string value = parts[1].Trim().Trim('"');
                    
                    switch (key)
                    {
                        case "id":
                            int.TryParse(value, out level.id);
                            break;
                        case "level_id":
                            level.level_id = value;
                            break;
                        case "level_name":
                            level.level_name = value;
                            break;
                        case "author":
                            level.author = value;
                            break;
                        case "author_id":
                            level.author_id = value;
                            break;
                        case "description":
                            level.description = value;
                            break;
                        case "level_data":
                            level.level_data = value;
                            break;
                        case "difficulty":
                            float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out level.difficulty);
                            break;
                        case "downloads":
                            int.TryParse(value, out level.downloads);
                            break;
                        case "likes":
                            int.TryParse(value, out level.likes);
                            break;
                        case "version":
                            int.TryParse(value, out level.version);
                            break;
                        case "is_rated":
                            bool.TryParse(value, out level.is_rated);
                            break;
                        case "is_weekly":
                            bool.TryParse(value, out level.is_weekly);
                            break;
                        case "created_at":
                            level.created_at = value;
                            break;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(level.level_id))
            {
                levels.Add(level);
            }
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"❌ Ошибка ручного парсинга: {e.Message}");
    }
    
    return levels;
}

    // Вспомогательный класс для парсинга
    [System.Serializable]
    private class SupabaseLevelList
    {
        public List<SupabaseLevelData> levels;
    }
    
    // 🧪 ТЕСТ ПОДКЛЮЧЕНИЯ
    public void TestConnection(System.Action<bool, string> callback)
    {
        StartCoroutine(TestConnectionCoroutine(callback));
    }
    
    private IEnumerator TestConnectionCoroutine(System.Action<bool, string> callback)
    {
        string url = $"{supabaseUrl}/rest/v1/{levelsTable}?select=count";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, "✅ Подключение к Supabase работает!");
            }
            else
            {
                callback?.Invoke(false, $"❌ Ошибка: {request.error}");
            }
        }
    }
}


[System.Serializable]
public class SupabaseLevelData
{
    public int id;                      // ЭТО ПОЛЕ ТОЛЬКО ДЛЯ ЧТЕНИЯ ИЗ БАЗЫ
    public string level_id;             // GUID - УНИКАЛЬНЫЙ ID ДЛЯ КАЖДОГО УРОВНЯ
    public string level_name;
    public string author;
    public string author_id;
    public string description;
    public string level_data;
    public float difficulty;
    public int downloads;
    public int likes;
    public int version;
    public bool is_weekly;
    public bool is_rated;
    public string created_at;
}