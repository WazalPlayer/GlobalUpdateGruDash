using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarketplaceUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject marketplacePanel;
    public Transform levelsContainer;
    public Transform ratedLevelsContainer;  // Новый контейнер для рейтинговых уровней
    public Transform weeklyDemonContainer;  // Новый контейнер для недельных демонов
    public GameObject levelEntryPrefab;
    public InputField searchInput;
    public Dropdown sortDropdown;
    public Dropdown filterDropdown;
    public Text statusText;
    public Button refreshButton;
    public Button closeButton;
    
    [Header("Difficulty Sprites")]
    public Sprite[] difficultySprites;  // порядок: 0 - очень легко, 1 - легко, 2 - средне, 3 - сложно, 4 - эксперт, 5 - изи демон, 6 - хард демон, 7 - экстрим демон

    [Header("Detail Panel")]
    public Text detailLevelIdText;      // поле для отображения level_id
    public Button copyIdButton;         // кнопка копирования ID
    public GameObject detailPanel;               // панель детального просмотра
    public Text detailLevelNameText;
    public Text detailAuthorText;
    public Text detailDownloadsText;      // новое поле для загрузок
    public Text detailLikesText;  
    public Text detailDifficultyText;
    public Image detailDifficultyIndicator;
    public GameObject detailRatedIcon;
    public Button detailDownloadButton;
    public Button detailLikeButton;




    private SupabaseLevelData currentDetailLevel; // текущий отображаемый уровень

public Button detailCloseButton;
    [Header("References")]
    public LevelCreator levelCreator;

    [Header("Страницы уровней")]
    public Button prevPageButton;       // «Назад»
    public Button nextPageButton;       // «Дальше»
    public Text pageText;               // «Страница N»
    public int pageSize = 25;           // уровней на странице
    public int autoLoadLimit = 10000;   // сколько грузить сразу подряд, дальше — при прокрутке
    public float prefetchDelay = 0f;    // пауза между фоновыми страницами
    [Range(0f, 0.3f)] public float scrollThreshold = 0.05f; // порог подгрузки при прокрутке (0 = низ)
    public float retryDelay = 10f;      // повтор запроса, если сервер не отвечает

    private ScrollRect levelsScroll;
    private int page = 0;               // текущая страница для кнопок
    private int loadedPages = 0;        // сколько страниц основного списка загружено
    private bool hasMore = true;
    private bool pagingLoading = false;

    private List<SupabaseLevelData> allLevels = new List<SupabaseLevelData>();
    private List<SupabaseLevelData> filteredLevels = new List<SupabaseLevelData>();
    private List<SupabaseLevelData> ratedLevels = new List<SupabaseLevelData>();    // Список рейтинговых уровней
    private List<SupabaseLevelData> weeklyLevels = new List<SupabaseLevelData>();   // Список недельных демонов
    
    void Start()
    {
        // Настройка UI
        if (sortDropdown != null)
            sortDropdown.onValueChanged.AddListener((value) => OnSortChanged());
        
        if (filterDropdown != null)
            filterDropdown.onValueChanged.AddListener((value) => OnFilterChanged());
        
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);
            
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshLevels);
            
        if (copyIdButton != null)
            copyIdButton.onClick.AddListener(CopyLevelIdToClipboard);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideMarketplace);
        
                if (detailPanel != null)
        detailPanel.SetActive(false);
    
    // Назначаем обработчики для кнопок детальной панели
    if (detailDownloadButton != null)
        detailDownloadButton.onClick.AddListener(OnDetailDownloadClicked);
    if (detailLikeButton != null)
        detailLikeButton.onClick.AddListener(OnDetailLikeClicked);
    if (detailCloseButton != null)
        detailCloseButton.onClick.AddListener(OnDetailCloseClicked);

        // Инициализация dropdowns
        InitializeDropdowns();

        // Страницы: кнопки и загрузка
        if (levelsContainer != null) levelsScroll = levelsContainer.GetComponentInParent<ScrollRect>(true);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        StartCoroutine(StartupRoutine());
    }

    void Update()
    {
        // Подгрузка при прокрутке до конца списка
        if (levelsScroll == null || pagingLoading || !hasMore || loadedPages == 0) return;
        if (levelsScroll.verticalNormalizedPosition <= scrollThreshold)
            StartCoroutine(AppendRoutine());
    }

    // ---------- Страницы и загрузка уровней ----------

    // Старт и обновление: очистить список, загрузить приоритетные, затем первую страницу и фоновую догрузку
    private IEnumerator StartupRoutine()
    {
        pagingLoading = true;
        SetPagingButtons();
        allLevels.Clear();
        loadedPages = 0;

        // 1. Приоритетные: рейтинговые и «Изи демон» и выше — все сразу
        List<SupabaseLevelData> priority = null;
        yield return FetchWithRetry(
            done => SupabaseManager.Instance.GetPriorityLevels(done),
            list => priority = list);
        ShowPriorityLevels(priority);

        // 2. Первая страница основного списка
        pagingLoading = false;
        yield return ReplaceRoutine(0);

        // 3. Фоновая догрузка подряд до autoLoadLimit
        while (hasMore && loadedPages * pageSize < autoLoadLimit)
        {
            if (prefetchDelay > 0f) yield return new WaitForSeconds(prefetchDelay);
            else yield return null;
            if (pagingLoading) continue;
            yield return AppendRoutine();
        }
    }

    public void NextPage()
    {
        if (pagingLoading || !hasMore) return;
        StartCoroutine(ReplaceRoutine(page + 1));
    }

    public void PrevPage()
    {
        if (pagingLoading || page <= 0) return;
        StartCoroutine(ReplaceRoutine(page - 1));
    }

    // Заменить список страницей p
    private IEnumerator ReplaceRoutine(int p)
    {
        pagingLoading = true;
        SetPagingButtons();

        List<SupabaseLevelData> levels = null;
        yield return FetchWithRetry(
            done => SupabaseManager.Instance.GetLevelsPage(p * pageSize, pageSize + 1, done, SupabaseManager.RegularFilter),
            list => levels = list);

        hasMore = levels.Count > pageSize;
        if (hasMore) levels.RemoveRange(pageSize, levels.Count - pageSize);

        page = p;
        loadedPages = p + 1;
        ShowLevelPage(levels, false);
        if (levelsScroll != null) levelsScroll.verticalNormalizedPosition = 1f;

        pagingLoading = false;
        SetPagingButtons();
    }

    // Добавить следующую страницу в конец
    private IEnumerator AppendRoutine()
    {
        if (pagingLoading || !hasMore) yield break;
        pagingLoading = true;
        SetPagingButtons();

        int p = loadedPages;
        List<SupabaseLevelData> levels = null;
        yield return FetchWithRetry(
            done => SupabaseManager.Instance.GetLevelsPage(p * pageSize, pageSize + 1, done, SupabaseManager.RegularFilter),
            list => levels = list);

        hasMore = levels.Count > pageSize;
        if (hasMore) levels.RemoveRange(pageSize, levels.Count - pageSize);

        loadedPages = p + 1;
        ShowLevelPage(levels, true);

        pagingLoading = false;
        SetPagingButtons();
    }

    // Запрос с повтором: если сервер не ответил, ждём retryDelay и пробуем снова
    private IEnumerator FetchWithRetry(System.Action<System.Action<List<SupabaseLevelData>>> start, System.Action<List<SupabaseLevelData>> onOk)
    {
        while (true)
        {
            if (SupabaseManager.Instance == null)
            {
                SetStatus("Сервер не инициализирован, повтор через " + retryDelay + " с");
                yield return new WaitForSeconds(retryDelay);
                continue;
            }

            List<SupabaseLevelData> data = null;
            start(list => data = list);
            while (data == null) yield return null;

            if (!SupabaseManager.LastLevelsRequestFailed)
            {
                onOk(data);
                yield break;
            }

            SetStatus("Нет связи с сервером, повтор через " + retryDelay + " с");
            yield return new WaitForSeconds(retryDelay);
        }
    }

    private void SetPagingButtons()
    {
        if (prevPageButton != null) prevPageButton.interactable = !pagingLoading && page > 0;
        if (nextPageButton != null) nextPageButton.interactable = !pagingLoading && hasMore;
        if (pageText != null) pageText.text = $"Страница {page + 1}";
    }
    

    private void CopyLevelIdToClipboard()
{
    if (currentDetailLevel != null && !string.IsNullOrEmpty(currentDetailLevel.level_id))
    {
        GUIUtility.systemCopyBuffer = currentDetailLevel.level_id;
        SetStatus("✅ ID уровня скопирован в буфер обмена");
        Debug.Log($"📋 Скопирован ID: {currentDetailLevel.level_id}");
    }
    else
    {
        SetStatus("❌ Невозможно скопировать ID – уровень не выбран");
    }
}

    private void InitializeDropdowns()
    {
        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string> { "По дате", "По популярности", "По лайкам", "По сложности" });
        }
        
        if (filterDropdown != null)
        {
            filterDropdown.ClearOptions();
            filterDropdown.AddOptions(new List<string> { "Все уровни", "Мои уровни", "Легкие", "Популярные" });
        }
    }
    
 public void ShowLevelDetails(SupabaseLevelData level)
{
    if (level == null || detailPanel == null) return;
    
    currentDetailLevel = level;
    
    // Заголовок и автор
    if (detailLevelNameText != null) detailLevelNameText.text = level.level_name;
    if (detailAuthorText != null) detailAuthorText.text = $"Автор: {level.author}";
    
    // Отдельно загрузки и лайки
    if (detailDownloadsText != null) detailDownloadsText.text = $"Загрузки: {level.downloads}";
    if (detailLikesText != null) detailLikesText.text = $"Лайки: {level.likes}";
    
    // Сложность
    if (detailDifficultyText != null) detailDifficultyText.text = GetDifficultyText(level.difficulty);
    
    if (detailLevelIdText != null)
        detailLevelIdText.text = $"ID: {level.level_id}";

    // Индикатор сложности (спрайт)
    if (detailDifficultyIndicator != null)
    {
        detailDifficultyIndicator.sprite = GetDifficultySprite(level.difficulty);
        detailDifficultyIndicator.color = Color.white;
    }
    
    // Иконка рейтинга
    if (detailRatedIcon != null) detailRatedIcon.SetActive(level.is_rated);
    
    // Лайк недоступен для своих уровней
    string playerId = PlayerPrefs.GetString("PlayerID", "");
    bool isMyLevel = level.author_id == playerId;
    if (detailLikeButton != null) detailLikeButton.interactable = !isMyLevel;
    
    detailPanel.SetActive(true);
}

private string GetDifficultyText(float difficulty)
{
    if (difficulty <= 1.5f) return "Очень легко";
    if (difficulty <= 2.5f) return "Легко";
    if (difficulty <= 3.5f) return "Средне";
    if (difficulty <= 4.5f) return "Сложно";
    if (difficulty <= 5.5f) return "Эксперт";
    if (difficulty <= 6.5f) return "Изи Демон";
    if (difficulty <= 7.5f) return "Хард Демон";
    return "Экстрим Демон";
}

private void OnDetailDownloadClicked()
{
    if (currentDetailLevel != null)
        LoadLevelIntoEditor(currentDetailLevel);
}

private void OnDetailLikeClicked()
{
    if (currentDetailLevel != null && detailLikeButton != null && detailLikeButton.interactable)
    {
        detailLikeButton.interactable = false;
        LikeLevel(currentDetailLevel.level_id);
        
        // Локально увеличиваем счётчик и обновляем UI
        currentDetailLevel.likes++;
        if (detailLikesText != null)
            detailLikesText.text = $"Лайки: {currentDetailLevel.likes}";
    }
}

public Sprite GetDifficultySprite(float difficulty)
{
    if (difficultySprites == null || difficultySprites.Length == 0)
        return null;
    
    int index = 0;
    if (difficulty <= 1.5f) index = 0;
    else if (difficulty <= 2.5f) index = 1;
    else if (difficulty <= 3.5f) index = 2;
    else if (difficulty <= 4.5f) index = 3;
    else if (difficulty <= 5.5f) index = 4;
    else if (difficulty <= 6.5f) index = 5;
    else if (difficulty <= 7.5f) index = 6;
    else index = 7;

    if (index >= difficultySprites.Length)
        index = difficultySprites.Length - 1;
    
    return difficultySprites[index];

}

private void OnDetailCloseClicked()
{
    if (detailPanel != null)
        detailPanel.SetActive(false);
    currentDetailLevel = null;
}

    // 📱 ПОКАЗАТЬ МАРКЕТПЛЕЙС
    public void ShowMarketplace()
    {
        if (marketplacePanel != null)
        {
            marketplacePanel.SetActive(true);
            RefreshLevels();
        }
    }
    
    // 📱 СКРЫТЬ МАРКЕТПЛЕЙС
    public void HideMarketplace()
    {
        if (marketplacePanel != null)
        {
            marketplacePanel.SetActive(false);
        }
    }
    
    // 🔄 ОБНОВИТЬ УРОВНИ
    public void RefreshLevels()
    {
        SetStatus("Загрузка уровней...");
        StopAllCoroutines();
        StartCoroutine(StartupRoutine());
    }
    
    // Публичный вывод статуса (используется загрузчиком страниц)
    public void SetStatusMessage(string message)
    {
        SetStatus(message);
    }

    // Приоритетные уровни (рейтинговые и «Изи демон» и выше) — всегда полностью, в контейнер рейтинговых
    public void ShowPriorityLevels(List<SupabaseLevelData> levels)
    {
        if (ratedLevelsContainer == null) return;

        foreach (Transform child in ratedLevelsContainer) Destroy(child.gameObject);
        if (levels == null || levelEntryPrefab == null) return;

        foreach (var level in levels)
        {
            GameObject entry = Instantiate(levelEntryPrefab, ratedLevelsContainer);
            MarketplaceLevelEntryUI entryUI = entry.GetComponent<MarketplaceLevelEntryUI>();
            if (entryUI != null) entryUI.Setup(level, this);
        }
    }

    // Показать страницу уровней. append = true: добавить в конец без перерисовки всего списка
    public void ShowLevelPage(List<SupabaseLevelData> levels, bool append)
    {
        if (levels == null) levels = new List<SupabaseLevelData>();

        if (!append || allLevels == null)
        {
            allLevels = new List<SupabaseLevelData>(levels);
            ApplyFiltersAndSort();
        }
        else
        {
            allLevels.AddRange(levels);
            // Добавляем только новые карточки, остальные не трогаем (меньше лагов)
            if (levelEntryPrefab != null && levelsContainer != null)
            {
                foreach (var level in levels)
                {
                    GameObject entry = Instantiate(levelEntryPrefab, levelsContainer);
                    MarketplaceLevelEntryUI entryUI = entry.GetComponent<MarketplaceLevelEntryUI>();
                    if (entryUI != null) entryUI.Setup(level, this);
                }
            }
        }

        SetStatus($"Загружено {allLevels.Count} уровней");
    }

    private void OnLevelsLoaded(List<SupabaseLevelData> levels)
    {
        allLevels = levels ?? new List<SupabaseLevelData>();
        ApplyFiltersAndSort();
        
        // Обновляем отдельные списки
        UpdateRatedAndWeeklyLists();
        
        SetStatus($"Загружено {levels?.Count ?? 0} уровней");
    }
    
    // 🎮 ЗАГРУЗИТЬ УРОВЕНЬ В РЕДАКТОР - ДОЛЖЕН БЫТЬ PUBLIC!
    public void LoadLevelIntoEditor(SupabaseLevelData levelData)
    {
        if (levelCreator != null && levelData != null)
        {
            Debug.Log($"🔄 Загружаем уровень: {levelData.level_name}");
            
            // Обновляем статистику скачиваний
            SupabaseManager.Instance.IncrementDownloads(levelData.level_id);
            
            // Загружаем уровень в редактор
            levelCreator.LoadLevelFromSupabase(levelData);
            HideMarketplace();
            
            SetStatus($"Уровень '{levelData.level_name}' загружен!");
        }
        else
        {
            Debug.LogError("❌ LevelCreator не назначен или данные уровня пусты");
            SetStatus("❌ Ошибка загрузки уровня");
        }
    }
    
    // ⭐ ЛАЙКНУТЬ УРОВЕНЬ - ДОЛЖЕН БЫТЬ PUBLIC!
    public void LikeLevel(string levelId)
    {
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.LikeLevel(levelId, (success) => {
                if (success)
                {
                    SetStatus("✅ Лайк добавлен!");
                    // Обновляем список чтобы отобразить изменения
                    RefreshLevels();
                }
                else
                {
                    SetStatus("❌ Ошибка при добавлении лайка");
                }
            });
        }
    }
    
    // 📝 СОЗДАТЬ СПИСОК УРОВНЕЙ
    private void PopulateLevelsList()
    {
        // Очищаем контейнер
        foreach (Transform child in levelsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Создаем элементы UI
        foreach (var level in filteredLevels)
        {
            if (levelEntryPrefab != null)
            {
                GameObject entry = Instantiate(levelEntryPrefab, levelsContainer);
                MarketplaceLevelEntryUI entryUI = entry.GetComponent<MarketplaceLevelEntryUI>();
                
                if (entryUI != null)
                {
                    entryUI.Setup(level, this);
                }
                else
                {
                    Debug.LogError("❌ MarketplaceLevelEntryUI не найден на префабе");
                }
            }
        }
        
        SetStatus($"Показано {filteredLevels.Count} из {allLevels.Count} уровней");
    }
    
    // 📝 СОЗДАТЬ СПИСОК РЕЙТИНГОВЫХ УРОВНЕЙ
    private void PopulateRatedLevelsList()
    {
        // Очищаем контейнер
        if (ratedLevelsContainer != null)
        {
            foreach (Transform child in ratedLevelsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Создаем элементы UI
            foreach (var level in ratedLevels)
            {
                if (levelEntryPrefab != null)
                {
                    GameObject entry = Instantiate(levelEntryPrefab, ratedLevelsContainer);
                    MarketplaceLevelEntryUI entryUI = entry.GetComponent<MarketplaceLevelEntryUI>();
                    
                    if (entryUI != null)
                    {
                        entryUI.Setup(level, this);
                    }
                }
            }
            
            Debug.Log($"🟢 Заполнен список рейтинговых уровней: {ratedLevels.Count} шт");
        }
    }
    
    // 📝 СОЗДАТЬ СПИСОК НЕДЕЛЬНЫХ ДЕМОНОВ
    private void PopulateWeeklyLevelsList()
    {
        // Очищаем контейнер
        if (weeklyDemonContainer != null)
        {
            foreach (Transform child in weeklyDemonContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Создаем элементы UI
            foreach (var level in weeklyLevels)
            {
                if (levelEntryPrefab != null)
                {
                    GameObject entry = Instantiate(levelEntryPrefab, weeklyDemonContainer);
                    MarketplaceLevelEntryUI entryUI = entry.GetComponent<MarketplaceLevelEntryUI>();
                    
                    if (entryUI != null)
                    {
                        entryUI.Setup(level, this);
                    }
                }
            }
            
            Debug.Log($"🔵 Заполнен список недельных демонов: {weeklyLevels.Count} шт");
        }
    }
    
    // 🔄 ОБНОВИТЬ СПИСКИ РЕЙТИНГОВЫХ И НЕДЕЛЬНЫХ УРОВНЕЙ
// 🔄 ОБНОВИТЬ СПИСКИ РЕЙТИНГОВЫХ И НЕДЕЛЬНЫХ УРОВНЕЙ
private void UpdateRatedAndWeeklyLists()
{
    // Фильтруем рейтинговые уровни
    ratedLevels = allLevels.FindAll(level => level.is_rated);
    
    // Фильтруем недельные демоны
    weeklyLevels = allLevels.FindAll(level => level.is_weekly);
    
    // Заполняем контейнеры
    PopulateRatedLevelsList();
    PopulateWeeklyLevelsList();
}
    
    // 🔍 ФИЛЬТРАЦИЯ И СОРТИРОВКА
private void OnSearchChanged(string searchText)
{
    if (string.IsNullOrEmpty(searchText))
    {
        RefreshLevels();
    }
    else
    {
        SetStatus($"Поиск: {searchText}...");
        Debug.Log($"🔎 Поисковый запрос (сырой): '{searchText}', длина: {searchText?.Length}");
        SupabaseManager.Instance.SearchLevels(searchText, OnSearchResults);
    }
}
    
    private void OnSearchResults(List<SupabaseLevelData> results)
    {
        allLevels = results ?? new List<SupabaseLevelData>();
        ApplyFiltersAndSort();
        UpdateRatedAndWeeklyLists(); // Обновляем отдельные списки
        SetStatus($"Найдено {results?.Count ?? 0} уровней");
    }
    
    private void OnSortChanged()
    {
        ApplyFiltersAndSort();
    }
    
    private void OnFilterChanged()
    {
        ApplyFiltersAndSort();
    }
    
    private void ApplyFiltersAndSort()
    {
        filteredLevels = new List<SupabaseLevelData>(allLevels);
        
        // Применяем фильтры
        if (filterDropdown != null)
        {
            switch (filterDropdown.value)
            {
                case 1: // Только мои уровни
                    string playerId = PlayerPrefs.GetString("PlayerID", "");
                    filteredLevels = filteredLevels.FindAll(level => level.author_id == playerId);
                    break;
                    
                case 2: // Легкие
                    filteredLevels = filteredLevels.FindAll(level => level.difficulty <= 2f);
                    break;
                    
                case 3: // Популярные
                    filteredLevels = filteredLevels.FindAll(level => level.downloads >= 10);
                    break;
            }
        }
        
        // Применяем сортировку
        if (sortDropdown != null)
        {
            switch (sortDropdown.value)
            {
                case 0: // По дате
                    filteredLevels.Sort((a, b) => string.Compare(b.created_at, a.created_at));
                    break;
                    
                case 1: // По популярности
                    filteredLevels.Sort((a, b) => b.downloads.CompareTo(a.downloads));
                    break;
                    
                case 2: // По лайкам
                    filteredLevels.Sort((a, b) => b.likes.CompareTo(a.likes));
                    break;
                    
                case 3: // По сложности
                    filteredLevels.Sort((a, b) => a.difficulty.CompareTo(b.difficulty));
                    break;
            }
        }
        
        PopulateLevelsList();
    }
    
    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"🔔 MarketplaceUI: {message}");
    }
}