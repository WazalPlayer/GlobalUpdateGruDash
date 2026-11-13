using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

public class QuestsManager : MonoBehaviour
{
    public static QuestsManager Instance { get; private set; }

    [Header("Синхронизация прогресса")]
    public bool autoSyncProgress = true;
    public float syncInterval = 10f;

    [Header("Настройки UI")]
    public Transform questsContainer;
    public GameObject questPrefab;
    public Text activeQuestsCountText;
    public Text refreshTimeText;
    public Button refreshQuestsButton;
    public Button clearQuestsButton;
    public GameObject confirmationPanel;
    public Text confirmationText;

    [Header("Настройки квестов")]
    public List<QuestData> allQuests = new List<QuestData>();
    public int maxActiveQuests = 6;
    public float questRefreshTime = 86400f;

    [Header("Очистка квестов")]
    public bool enableQuestClearing = true;
    public int clearQuestsCost = 100;
    public float clearCooldown = 300f;

    [Header("Обнуление прогресса")]
    public bool enableAutoReset = true;
    public float resetInterval = 3600f; // 1 час в секундах
    public Text resetTimeText;
    
    private List<Quest> activeQuests = new List<Quest>();
    private const string QUESTS_KEY = "PlayerQuests";
    private const string QUESTS_REFRESH_KEY = "QuestsRefreshTime";
    private const string LAST_CLEAR_TIME_KEY = "LastClearTime";
    private const string LAST_RESET_TIME_KEY = "LastProgressResetTime";
    private const string PROGRESS_RESET_COUNTER_KEY = "ProgressResetCounter";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Инициализируем ID квестов
            InitializeQuestIds();
            InitializeQuests();
            LoadQuests();
            
            Debug.Log("✅ QuestsManager Awake: инициализирован");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("⚠️ QuestsManager Awake: дубликат уничтожен");
            return;
        }
    }

    void Start()
    {
        UpdateQuestsUI();
        
        // Настраиваем кнопки
        if (refreshQuestsButton != null)
        {
            refreshQuestsButton.onClick.AddListener(RefreshQuestsManually);
        }
        
        if (clearQuestsButton != null)
        {
            clearQuestsButton.onClick.AddListener(ShowClearConfirmation);
            UpdateClearButtonState();
        }
        
        // Скрываем панель подтверждения
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        // Проверяем и выполняем сброс прогресса при старте
        if (enableAutoReset)
        {
            CheckAndResetProgress();
        }

        // Загружаем и синхронизируем прогресс
        LoadAndSyncProgress();
        
        // Запускаем автоматическую синхронизацию
        if (autoSyncProgress)
        {
            StartCoroutine(AutoSyncProgressRoutine());
        }
        
        // Запускаем обновление UI времени
        StartCoroutine(UpdateRefreshTimeUI());
        
        // Запускаем обновление UI времени сброса
        if (enableAutoReset)
        {
            StartCoroutine(UpdateResetTimeUI());
        }
        
        Debug.Log($"✅ QuestsManager Start: запущен. Активных квестов: {activeQuests.Count}");
    }

    // Инициализация ID квестов - исправляет проблему с ID=0
    void InitializeQuestIds()
    {
        // Если квесты уже есть, убедимся что у них правильные ID
        if (allQuests.Count > 0)
        {
            for (int i = 0; i < allQuests.Count; i++)
            {
                // Если ID не установлен (0 или меньше), устанавливаем его
                if (allQuests[i].id <= 0)
                {
                    allQuests[i].id = i + 1;
                    Debug.Log($"🔧 QuestsManager: Установлен ID {i + 1} для квеста '{allQuests[i].title}'");
                }
            }
        }
    }

    // Загрузка и синхронизация прогресса из PlayerPrefs
    void LoadAndSyncProgress()
    {
        Debug.Log("🔄 Синхронизация прогресса квестов...");
        
        // Загружаем общий прогресс из PlayerPrefs
        int totalClicks = PlayerPrefs.GetInt("Quest_TotalClicks", 0);
        int finishTouches = PlayerPrefs.GetInt("Quest_FinishTouches", 0);
        int demonsKilled = PlayerPrefs.GetInt("Quest_DemonsKilled", 0);
        
        // Обновляем квесты на основе загруженного прогресса
        SyncQuestProgress(QuestType.TotalClicks, totalClicks);
        SyncQuestProgress(QuestType.TouchFinish, finishTouches);
        SyncQuestProgress(QuestType.DemonsCompleted, demonsKilled);
        
        Debug.Log($"📊 Прогресс синхронизирован: Клики={totalClicks}, Финиши={finishTouches}, Демоны={demonsKilled}");
    }
    
    // Синхронизация прогресса для определенного типа квеста
    void SyncQuestProgress(QuestType type, int totalProgress)
    {
        bool updated = false;
        
        foreach (Quest quest in activeQuests)
        {
            if (quest.data.questType == type && !quest.data.isCompleted)
            {
                // Устанавливаем прогресс, но не больше целевого значения
                int oldProgress = quest.data.currentProgress;
                quest.data.currentProgress = Mathf.Min(totalProgress, quest.data.targetCount);
                
                // Проверяем выполнение квеста
                if (quest.data.currentProgress >= quest.data.targetCount && !quest.data.isCompleted)
                {
                    quest.data.isCompleted = true;
                    Debug.Log($"✅ Квест '{quest.data.title}' выполнен при синхронизации!");
                    updated = true;
                }
                
                if (oldProgress != quest.data.currentProgress)
                {
                    updated = true;
                }
            }
        }
        
        if (updated)
        {
            SaveQuests();
            UpdateQuestsUI();
        }
    }
    
    // Корутина для автоматической синхронизации прогресса
    IEnumerator AutoSyncProgressRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(syncInterval);
            
            if (autoSyncProgress)
            {
                LoadAndSyncProgress();
                Debug.Log("🔄 Автоматическая синхронизация прогресса выполнена");
            }
        }
    }

    // Проверка и выполнение сброса прогресса
    void CheckAndResetProgress()
    {
        if (!enableAutoReset) return;
        
        if (ShouldResetProgress())
        {
            ResetAllProgress();
            SaveLastResetTime();
            Debug.Log("🔄 Прогресс квестов обнулен (автоматический сброс)");
        }
    }

    // Проверка, нужно ли выполнить сброс прогресса
    bool ShouldResetProgress()
    {
        if (!PlayerPrefs.HasKey(LAST_RESET_TIME_KEY))
            return true;

        string lastResetDate = PlayerPrefs.GetString(LAST_RESET_TIME_KEY, "");
        if (string.IsNullOrEmpty(lastResetDate))
            return true;

        try
        {
            DateTime lastReset = DateTime.Parse(lastResetDate);
            DateTime now = DateTime.Now;
            TimeSpan timeSinceReset = now - lastReset;
            
            return timeSinceReset.TotalSeconds >= resetInterval;
        }
        catch
        {
            return true;
        }
    }

    // Сброс всего прогресса
    public void ResetAllProgress()
    {
        // Сбрасываем прогресс всех квестов
        foreach (Quest quest in activeQuests)
        {
            quest.data.currentProgress = 0;
            quest.data.isCompleted = false;
            // НЕ сбрасываем полученные награды, только прогресс
            // quest.data.rewardClaimed = false;
        }
        
        // Сбрасываем счетчики в PlayerPrefs
        PlayerPrefs.SetInt("Quest_TotalClicks", 0);
        PlayerPrefs.SetInt("Quest_FinishTouches", 0);
        PlayerPrefs.SetInt("Quest_DemonsKilled", 0);
        
        // Увеличиваем счетчик сбросов
        int resetCount = PlayerPrefs.GetInt(PROGRESS_RESET_COUNTER_KEY, 0);
        PlayerPrefs.SetInt(PROGRESS_RESET_COUNTER_KEY, resetCount + 1);
        
        // Сохраняем изменения
        SaveQuests();
        PlayerPrefs.Save();
        
        // Обновляем UI
        UpdateQuestsUI();
        
        Debug.Log($"♻️ Весь прогресс сброшен. Сбросов выполнено: {resetCount + 1}");
    }

    // Сохранение времени последнего сброса
    void SaveLastResetTime()
    {
        PlayerPrefs.SetString(LAST_RESET_TIME_KEY, DateTime.Now.ToString("o"));
        PlayerPrefs.Save();
    }

    // Получение времени до следующего сброса
    TimeSpan GetTimeUntilReset()
    {
        if (!PlayerPrefs.HasKey(LAST_RESET_TIME_KEY))
            return TimeSpan.Zero;

        string lastResetDate = PlayerPrefs.GetString(LAST_RESET_TIME_KEY, "");
        if (string.IsNullOrEmpty(lastResetDate))
            return TimeSpan.Zero;

        try
        {
            DateTime lastReset = DateTime.Parse(lastResetDate);
            DateTime now = DateTime.Now;
            TimeSpan timeSinceReset = now - lastReset;
            TimeSpan timeUntilReset = TimeSpan.FromSeconds(resetInterval) - timeSinceReset;
            
            return timeUntilReset.TotalSeconds > 0 ? timeUntilReset : TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    // Корутина для обновления UI времени сброса
    IEnumerator UpdateResetTimeUI()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (resetTimeText != null && enableAutoReset)
            {
                TimeSpan timeUntilReset = GetTimeUntilReset();
                
                if (timeUntilReset.TotalSeconds > 0)
                {
                    resetTimeText.text = $"Сброс через: {FormatTimeSpan(timeUntilReset)}";
                }
                else
                {
                    resetTimeText.text = "Сброс прогресса...";
                    // Выполняем сброс, если время вышло
                    CheckAndResetProgress();
                }
            }
        }
    }

    void InitializeQuests()
    {
        // Если список квестов пустой, создаем стандартные квесты
        if (allQuests.Count == 0)
        {
            Debug.Log("📝 QuestsManager: Создаем стандартные квесты...");
            
            // Создаем все 6 квестов с правильными ID
            allQuests = new List<QuestData>
            {
                new QuestData
                {
                    id = 1,
                    title = "Новичок финиша",
                    description = "Достигнуть финиша 5 раз",
                    questType = QuestType.TouchFinish,
                    targetCount = 5,
                    rewardStars = 50,
                    isCompleted = false,
                    rewardClaimed = false
                },
                new QuestData
                {
                    id = 2,
                    title = "Быстрые пальцы",
                    description = "Сделать 100 прыжков",
                    questType = QuestType.TotalClicks,
                    targetCount = 100,
                    rewardStars = 75,
                    isCompleted = false,
                    rewardClaimed = false
                },
                new QuestData
                {
                    id = 3,
                    title = "Охотник на демонов",
                    description = "Пройти 3 демонов",
                    questType = QuestType.DemonsCompleted,
                    targetCount = 3,
                    rewardStars = 100,
                    isCompleted = false,
                    rewardClaimed = false
                },
                new QuestData
                {
                    id = 4,
                    title = "Эксперт финиша",
                    description = "Достигнуть финиша 20 раз",
                    questType = QuestType.TouchFinish,
                    targetCount = 20,
                    rewardStars = 200,
                    isCompleted = false,
                    rewardClaimed = false
                },
                new QuestData
                {
                    id = 5,
                    title = "Мастер кликов",
                    description = "Сделать 500 прыжков",
                    questType = QuestType.TotalClicks,
                    targetCount = 500,
                    rewardStars = 300,
                    isCompleted = false,
                    rewardClaimed = false
                },
                new QuestData
                {
                    id = 6,
                    title = "Истребитель демонов",
                    description = "Пройти 10 демонов",
                    questType = QuestType.DemonsCompleted,
                    targetCount = 10,
                    rewardStars = 500,
                    isCompleted = false,
                    rewardClaimed = false
                }
            };
            
            Debug.Log($"📋 Создано {allQuests.Count} стандартных квестов");
        }
        else
        {
            Debug.Log($"📋 Используются {allQuests.Count} предустановленных квестов");
        }
    }

    void LoadQuests()
    {
        // Проверяем, нужно ли обновить квесты
        if (ShouldRefreshQuests())
        {
            GenerateAllQuests();
            SaveQuestsRefreshTime();
        }
        else
        {
            LoadSavedQuests();
        }
        
        Debug.Log($"📥 Загружено {activeQuests.Count} квестов");
    }

    bool ShouldRefreshQuests()
    {
        if (!PlayerPrefs.HasKey(QUESTS_REFRESH_KEY))
            return true;

        string lastRefreshDate = PlayerPrefs.GetString(QUESTS_REFRESH_KEY, "");
        if (string.IsNullOrEmpty(lastRefreshDate))
            return true;

        try
        {
            DateTime lastRefresh = DateTime.Parse(lastRefreshDate);
            DateTime now = DateTime.Now;
            TimeSpan timeSinceRefresh = now - lastRefresh;
            
            return timeSinceRefresh.TotalSeconds >= questRefreshTime;
        }
        catch
        {
            return true;
        }
    }

    // Генерация ВСЕХ квестов
    void GenerateAllQuests()
    {
        activeQuests.Clear();
        
        if (allQuests.Count == 0)
        {
            Debug.LogError("❌ QuestsManager: Не могу создать квесты - список allQuests пустой!");
            return;
        }
        
        Debug.Log($"🔧 QuestsManager: Создаем квесты из списка ({allQuests.Count} квестов)...");
        
        // Добавляем ВСЕ квесты из allQuests
        for (int i = 0; i < allQuests.Count; i++)
        {
            QuestData questData = allQuests[i];
            
            // Проверяем ID квеста
            if (questData.id <= 0)
            {
                Debug.LogWarning($"⚠️ QuestsManager: Квест '{questData.title}' имеет ID={questData.id}. Исправляю на ID={i + 1}");
                questData.id = i + 1;
            }
            
            QuestData newQuestData = new QuestData
            {
                id = questData.id,
                title = questData.title,
                description = questData.description,
                questType = questData.questType,
                targetCount = questData.targetCount,
                currentProgress = 0,
                rewardStars = questData.rewardStars,
                isCompleted = false,
                rewardClaimed = false
            };
            
            Quest newQuest = new Quest(newQuestData);
            activeQuests.Add(newQuest);
            
            Debug.Log($"➕ Создан квест: ID={newQuestData.id}, Title='{newQuestData.title}'");
        }
        
        SaveQuests();
        Debug.Log($"🔄 Сгенерированы ВСЕ квесты: {activeQuests.Count}");
    }

    void LoadSavedQuests()
    {
        string questsJson = PlayerPrefs.GetString(QUESTS_KEY, "");
        
        if (!string.IsNullOrEmpty(questsJson))
        {
            try
            {
                QuestSaveData saveData = JsonUtility.FromJson<QuestSaveData>(questsJson);
                if (saveData != null && saveData.activeQuests != null)
                {
                    activeQuests = saveData.activeQuests;
                    
                    // Проверяем ID загруженных квестов
                    foreach (Quest quest in activeQuests)
                    {
                        if (quest.data.id <= 0)
                        {
                            Debug.LogWarning($"⚠️ QuestsManager: Загружен квест с ID=0: '{quest.data.title}'");
                        }
                    }
                    
                    Debug.Log($"📥 Загружено {activeQuests.Count} квестов из сохранения");
                }
                else
                {
                    Debug.LogWarning("⚠️ QuestsManager: Сохраненные данные повреждены. Генерируем новые квесты.");
                    GenerateAllQuests();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Ошибка загрузки квестов: {e.Message}");
                GenerateAllQuests();
            }
        }
        else
        {
            Debug.Log("ℹ️ QuestsManager: Нет сохраненных квестов. Генерируем новые.");
            GenerateAllQuests();
        }
    }

    void SaveQuests()
    {
        QuestSaveData saveData = new QuestSaveData
        {
            activeQuests = activeQuests
        };
        
        string questsJson = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(QUESTS_KEY, questsJson);
        PlayerPrefs.Save();
        
        Debug.Log($"💾 Квесты сохранены: {activeQuests.Count}");
    }

    void SaveQuestsRefreshTime()
    {
        PlayerPrefs.SetString(QUESTS_REFRESH_KEY, DateTime.Now.ToString("o"));
        PlayerPrefs.Save();
    }

    // Показать панель подтверждения очистки
    public void ShowClearConfirmation()
    {
        if (!enableQuestClearing)
        {
            Debug.Log("⚠️ Очистка квестов отключена");
            return;
        }

        if (IsClearCooldownActive())
        {
            TimeSpan cooldown = GetClearCooldownRemaining();
            Debug.Log($"⏳ Очистка будет доступна через: {FormatTimeSpan(cooldown)}");
            return;
        }

        if (confirmationPanel != null && confirmationText != null)
        {
            confirmationText.text = $"Очистить все квесты?\nСтоимость: {clearQuestsCost} ⭐\n\nВсе неполученные награды будут потеряны!";
            confirmationPanel.SetActive(true);
        }
    }

    // Подтверждение очистки квестов
    public void ConfirmClearQuests()
    {
        if (AccountsManager.Instance != null)
        {
            int currentStars = AccountsManager.Instance.GetPlayerStars();
            
            if (currentStars >= clearQuestsCost)
            {
                AccountsManager.Instance.AddPlayerStars(-clearQuestsCost);
                AccountsManager.Instance.SaveAccountData();
                
                ClearAllQuests();
                
                PlayerPrefs.SetString(LAST_CLEAR_TIME_KEY, DateTime.Now.ToString("o"));
                PlayerPrefs.Save();
                
                Debug.Log($"♻️ Квесты очищены. Списано {clearQuestsCost} звёзд");
            }
            else
            {
                Debug.Log($"❌ Недостаточно звёзд для очистки квестов. Нужно: {clearQuestsCost}, есть: {currentStars}");
            }
        }
        else
        {
            ClearAllQuests();
            Debug.Log("♻️ Квесты очищены (без списания звёзд)");
        }
        
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        
        UpdateClearButtonState();
    }

    // Отмена очистки квестов
    public void CancelClearQuests()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
        Debug.Log("❌ Очистка квестов отменена");
    }

    // Очистка всех квестов
    public void ClearAllQuests()
    {
        foreach (Quest quest in activeQuests)
        {
            quest.data.currentProgress = 0;
            quest.data.isCompleted = false;
            quest.data.rewardClaimed = false;
        }
        
        SaveQuests();
        UpdateQuestsUI();
        
        Debug.Log("♻️ Все квесты очищены");
    }

    // Проверка кулдауна очистки
    bool IsClearCooldownActive()
    {
        if (!PlayerPrefs.HasKey(LAST_CLEAR_TIME_KEY))
            return false;

        string lastClearDate = PlayerPrefs.GetString(LAST_CLEAR_TIME_KEY, "");
        if (string.IsNullOrEmpty(lastClearDate))
            return false;

        try
        {
            DateTime lastClear = DateTime.Parse(lastClearDate);
            DateTime now = DateTime.Now;
            TimeSpan timeSinceClear = now - lastClear;
            
            return timeSinceClear.TotalSeconds < clearCooldown;
        }
        catch
        {
            return false;
        }
    }

    // Получение оставшегося времени кулдауна
    TimeSpan GetClearCooldownRemaining()
    {
        if (!PlayerPrefs.HasKey(LAST_CLEAR_TIME_KEY))
            return TimeSpan.Zero;

        string lastClearDate = PlayerPrefs.GetString(LAST_CLEAR_TIME_KEY, "");
        if (string.IsNullOrEmpty(lastClearDate))
            return TimeSpan.Zero;

        try
        {
            DateTime lastClear = DateTime.Parse(lastClearDate);
            DateTime now = DateTime.Now;
            TimeSpan timeSinceClear = now - lastClear;
            TimeSpan cooldownRemaining = TimeSpan.FromSeconds(clearCooldown) - timeSinceClear;
            
            return cooldownRemaining.TotalSeconds > 0 ? cooldownRemaining : TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    // Обновление состояния кнопки очистки
    void UpdateClearButtonState()
    {
        if (clearQuestsButton == null) return;
        
        bool hasUnclaimedRewards = activeQuests.Exists(q => q.data.isCompleted && !q.data.rewardClaimed);
        bool cooldownActive = IsClearCooldownActive();
        
        clearQuestsButton.interactable = hasUnclaimedRewards && !cooldownActive && enableQuestClearing;
        
        Text buttonText = clearQuestsButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            if (cooldownActive)
            {
                TimeSpan cooldown = GetClearCooldownRemaining();
                buttonText.text = $"Очистка через\n{FormatTimeSpan(cooldown)}";
            }
            else if (!hasUnclaimedRewards)
            {
                buttonText.text = "Нет наград";
            }
            else
            {
                buttonText.text = $"Очистить квесты\n({clearQuestsCost} ⭐)";
            }
        }
    }

    // Обновление UI квестов
    public void UpdateQuestsUI()
    {
        if (questsContainer == null)
        {
            Debug.LogError("❌ Quest container не назначен!");
            return;
        }
        
        if (questPrefab == null)
        {
            Debug.LogError("❌ Quest prefab не назначен!");
            return;
        }
        
        // Очищаем контейнер
        foreach (Transform child in questsContainer)
        {
            Destroy(child.gameObject);
        }
        
        Debug.Log($"🔄 Обновление UI квестов. Всего квестов: {activeQuests.Count}");
        
        // Проверяем ID всех квестов перед отображением
        foreach (Quest quest in activeQuests)
        {
            if (quest.data.id <= 0)
            {
                Debug.LogError($"❌ Квест '{quest.data.title}' имеет ID={quest.data.id}! Исправьте в инспекторе.");
            }
        }
        
        // Создаем префабы для ВСЕХ активных квестов
        foreach (Quest quest in activeQuests)
        {
            GameObject questUI = Instantiate(questPrefab, questsContainer);
            QuestPrefabController controller = questUI.GetComponent<QuestPrefabController>();
            
            if (controller != null)
            {
                controller.Initialize(quest.data);
            }
            else
            {
                Debug.LogError($"❌ QuestPrefabController не найден на префабе квеста {quest.data.title}");
            }
        }
        
        // Обновляем счетчик активных квестов
        if (activeQuestsCountText != null)
        {
            int completedCount = activeQuests.FindAll(q => q.data.isCompleted).Count;
            int claimedCount = activeQuests.FindAll(q => q.data.rewardClaimed).Count;
            int unclaimedCount = completedCount - claimedCount;
            
            activeQuestsCountText.text = $"Квесты: {unclaimedCount}/{activeQuests.Count}";
        }
        
        UpdateClearButtonState();
    }

    // Обновление времени до обновления квестов
    IEnumerator UpdateRefreshTimeUI()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (refreshTimeText != null)
            {
                TimeSpan timeUntilRefresh = GetTimeUntilRefresh();
                
                if (timeUntilRefresh.TotalSeconds > 0)
                {
                    refreshTimeText.text = $"Обновление через: {FormatTimeSpan(timeUntilRefresh)}";
                }
                else
                {
                    refreshTimeText.text = "Квесты можно обновить!";
                }
            }
            
            UpdateClearButtonState();
        }
    }

    TimeSpan GetTimeUntilRefresh()
    {
        if (!PlayerPrefs.HasKey(QUESTS_REFRESH_KEY))
            return TimeSpan.Zero;

        string lastRefreshDate = PlayerPrefs.GetString(QUESTS_REFRESH_KEY, "");
        if (string.IsNullOrEmpty(lastRefreshDate))
            return TimeSpan.Zero;

        try
        {
            DateTime lastRefresh = DateTime.Parse(lastRefreshDate);
            DateTime now = DateTime.Now;
            TimeSpan timeSinceRefresh = now - lastRefresh;
            TimeSpan timeUntilRefresh = TimeSpan.FromSeconds(questRefreshTime) - timeSinceRefresh;
            
            return timeUntilRefresh.TotalSeconds > 0 ? timeUntilRefresh : TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return string.Format("{0:00}ч {1:00}м", (int)timeSpan.TotalHours, timeSpan.Minutes);
        }
        else
        {
            return string.Format("{0:00}м {1:00}с", timeSpan.Minutes, timeSpan.Seconds);
        }
    }

    // Обновление прогресса квеста
    public void UpdateQuestProgress(QuestType type, int amount = 1)
    {
        bool progressMade = false;
        
        foreach (Quest quest in activeQuests)
        {
            if (!quest.data.isCompleted && quest.data.questType == type)
            {
                int oldProgress = quest.data.currentProgress;
                quest.data.currentProgress += amount;
                
                if (quest.data.currentProgress > quest.data.targetCount)
                    quest.data.currentProgress = quest.data.targetCount;
                
                if (quest.data.currentProgress >= quest.data.targetCount && !quest.data.isCompleted)
                {
                    quest.data.isCompleted = true;
                    Debug.Log($"✅ Квест '{quest.data.title}' выполнен!");
                }
                
                if (oldProgress != quest.data.currentProgress)
                {
                    progressMade = true;
                }
            }
        }
        
        if (progressMade)
        {
            // Сохраняем общий прогресс в PlayerPrefs
            SaveOverallProgress(type);
            
            SaveQuests();
            UpdateQuestsUI();
        }
    }

    // Сохранение общего прогресса в PlayerPrefs
    void SaveOverallProgress(QuestType type)
    {
        string key = "";
        
        switch (type)
        {
            case QuestType.TotalClicks:
                key = "Quest_TotalClicks";
                break;
            case QuestType.TouchFinish:
                key = "Quest_FinishTouches";
                break;
            case QuestType.DemonsCompleted:
                key = "Quest_DemonsKilled";
                break;
        }
        
        if (!string.IsNullOrEmpty(key))
        {
            int currentValue = PlayerPrefs.GetInt(key, 0);
            PlayerPrefs.SetInt(key, currentValue + 1);
            PlayerPrefs.Save();
        }
    }

    // Получение награды за квест
    public void ClaimQuestReward(int questId)
    {
        if (questId <= 0)
        {
            Debug.LogError($"❌ Неверный ID квеста: {questId}");
            return;
        }
        
        Quest quest = activeQuests.Find(q => q.data.id == questId);
        
        if (quest == null)
        {
            Debug.LogError($"❌ Квест с ID {questId} не найден. Всего квестов: {activeQuests.Count}");
            return;
        }
        
        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: если награда уже получена, ничего не делаем
        if (quest.data.rewardClaimed)
        {
            Debug.LogWarning($"⚠️ Награда за квест '{quest.data.title}' уже была получена ранее!");
            UpdateQuestsUI(); // Обновляем UI
            return;
        }
        
        if (!quest.data.isCompleted)
        {
            Debug.LogError($"❌ Квест '{quest.data.title}' еще не выполнен");
            return;
        }
        
        // Выдаем звезды
        if (AccountsManager.Instance != null)
        {
            int currentStars = AccountsManager.Instance.GetPlayerStars();
            AccountsManager.Instance.SetPlayerStars(currentStars + quest.data.rewardStars);
            AccountsManager.Instance.SaveAccountData();
            
            Debug.Log($"⭐ Начислено {quest.data.rewardStars} звезд за квест '{quest.data.title}'. Всего звезд: {currentStars + quest.data.rewardStars}");
        }
        else
        {
            // Если AccountsManager недоступен, сохраняем локально
            int currentStars = PlayerPrefs.GetInt("PlayerStars", 0);
            PlayerPrefs.SetInt("PlayerStars", currentStars + quest.data.rewardStars);
            PlayerPrefs.Save();
            
            Debug.Log($"⭐ Начислено {quest.data.rewardStars} звезд локально за квест '{quest.data.title}'");
        }
        
        // Отмечаем квест как полученный
        quest.data.rewardClaimed = true;
        
        SaveQuests();
        UpdateQuestsUI();
        
        Debug.Log($"🎁 Награда получена за квест '{quest.data.title}': {quest.data.rewardStars} звёзд, rewardClaimed={quest.data.rewardClaimed}");
    }

    // Получение данных квеста по ID
    public QuestData GetQuestData(int questId)
    {
        if (questId <= 0) return null;
        
        Quest quest = activeQuests.Find(q => q.data.id == questId);
        return quest?.data;
    }

    // Ручное обновление квестов
    public void RefreshQuestsManually()
    {
        TimeSpan timeUntilRefresh = GetTimeUntilRefresh();
        
        if (timeUntilRefresh.TotalSeconds <= 0)
        {
            GenerateAllQuests();
            SaveQuestsRefreshTime();
            UpdateQuestsUI();
            Debug.Log("🔄 Квесты обновлены вручную");
        }
        else
        {
            Debug.Log($"⏳ Квесты можно обновить через: {FormatTimeSpan(timeUntilRefresh)}");
        }
    }

    // Ручной сброс прогресса (для тестирования)
    public void ManualResetProgress()
    {
        ResetAllProgress();
        SaveLastResetTime();
        Debug.Log("🔄 Прогресс вручную сброшен");
    }

    void OnDestroy()
    {
        SaveQuests();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

[System.Serializable]
public class Quest
{
    public QuestData data;
    
    public Quest(QuestData questData)
    {
        data = questData;
    }
}

[System.Serializable]
public class QuestData
{
    public int id;
    public string title;
    public string description;
    public QuestType questType;
    public int targetCount;
    public int currentProgress;
    public int rewardStars;
    public bool isCompleted;
    public bool rewardClaimed;
}

[System.Serializable]
public enum QuestType
{
    TouchFinish,
    TotalClicks,
    DemonsCompleted
}

[System.Serializable]
public class QuestSaveData
{
    public List<Quest> activeQuests = new List<Quest>();
}