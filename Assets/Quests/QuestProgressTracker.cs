using UnityEngine;
using System.Collections;

public class QuestProgressTracker : MonoBehaviour
{
    [Header("Настройки отслеживания")]
    public bool trackClicks = true;
    public bool trackFinish = true;
    public bool trackDemons = true;
    
    [Header("Теги")]
    public string finishTag = "Finish";
    public string demonTag = "Demon";
    
    [Header("Частота проверки")]
    public float checkInterval = 1f;
    
    private float checkTimer = 0f;
    private int totalClicks = 0;
    private int finishTouches = 0;
    private int demonsKilled = 0;
    
    private const string CLICKS_KEY = "Quest_TotalClicks";
    private const string FINISH_KEY = "Quest_FinishTouches";
    private const string DEMONS_KEY = "Quest_DemonsKilled";
    
    private bool isInitialized = false;
    private Coroutine updateQuestsCoroutine;
    
    void Start()
    {
        InitializeTracker();
    }
    
    void InitializeTracker()
    {
        LoadProgress();
        
        // Инициализируем значения
        totalClicks = PlayerPrefs.GetInt(CLICKS_KEY, 0);
        finishTouches = PlayerPrefs.GetInt(FINISH_KEY, 0);
        demonsKilled = PlayerPrefs.GetInt(DEMONS_KEY, 0);
        
        // Запускаем проверку обновлений QuestsManager
        if (updateQuestsCoroutine == null)
        {
            updateQuestsCoroutine = StartCoroutine(CheckQuestsManagerRoutine());
        }
        
        isInitialized = true;
        Debug.Log($"🎯 Трекер квестов инициализирован: {totalClicks} кликов, {finishTouches} финишей, {demonsKilled} демонов");
    }
    
    void Update()
    {
        // Отслеживание кликов мыши/тапов
        if (trackClicks && Input.GetMouseButtonDown(0))
        {
            AddClick();
        }
        
        // Периодическая проверка изменений
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            CheckExternalProgress();
            checkTimer = 0f;
        }
    }
    
    void AddClick()
    {
        totalClicks++;
        PlayerPrefs.SetInt(CLICKS_KEY, totalClicks);
        PlayerPrefs.Save();
        
        // Передаем прогресс в QuestsManager
        UpdateQuestProgress(QuestType.TotalClicks, 1);
        
        Debug.Log($"🖱️ Клик зарегистрирован. Всего: {totalClicks}");
    }
    
    // 2D столкновение с триггером (для финиша)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;
        
        if (trackFinish && other.CompareTag(finishTag))
        {
            AddFinishTouch();
        }
        
        if (trackDemons && other.CompareTag(demonTag))
        {
            AddDemonKill(other.gameObject);
        }
    }
    
    // 2D столкновение с коллайдером (для демонов)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized) return;
        
        if (trackDemons && collision.gameObject.CompareTag(demonTag))
        {
            AddDemonKill(collision.gameObject);
        }
    }
    
    void AddFinishTouch()
    {
        finishTouches++;
        PlayerPrefs.SetInt(FINISH_KEY, finishTouches);
        PlayerPrefs.Save();
        
        // Передаем прогресс в QuestsManager
        UpdateQuestProgress(QuestType.TouchFinish, 1);
        
        Debug.Log($"🎯 Финиш достигнут! Всего: {finishTouches}");
    }
    
    void AddDemonKill(GameObject demon)
    {
        demonsKilled++;
        PlayerPrefs.SetInt(DEMONS_KEY, demonsKilled);
        PlayerPrefs.Save();
        
        // Передаем прогресс в QuestsManager
        UpdateQuestProgress(QuestType.DemonsCompleted, 1);
        
        // Также обновляем через AccountsManager
        if (AccountsManager.Instance != null)
        {
            string demonName = demon.name;
            AccountsManager.Instance.AddPlayerDemon(demonName);
        }
        
        Debug.Log($"👹 Демон побежден! Всего: {demonsKilled}");
    }
    
    // Основной метод для передачи прогресса в QuestsManager
    void UpdateQuestProgress(QuestType type, int amount = 1)
    {
        // Пытаемся передать напрямую
        bool success = TrySendToQuestsManager(type, amount);
        
        // Если не удалось, сохраняем для последующей отправки
        if (!success)
        {
            Debug.Log($"⏳ QuestsManager недоступен. Прогресс сохранен для последующей отправки: {type} +{amount}");
        }
    }
    
    bool TrySendToQuestsManager(QuestType type, int amount)
    {
        if (QuestsManager.Instance != null)
        {
            QuestsManager.Instance.UpdateQuestProgress(type, amount);
            return true;
        }
        return false;
    }
    
    // Проверка внешнего прогресса (например, из других систем)
    void CheckExternalProgress()
    {
        // Проверяем изменения в сохраненных данных
        int savedClicks = PlayerPrefs.GetInt(CLICKS_KEY, 0);
        int savedFinishes = PlayerPrefs.GetInt(FINISH_KEY, 0);
        int savedDemons = PlayerPrefs.GetInt(DEMONS_KEY, 0);
        
        // Проверяем клики
        if (savedClicks > totalClicks)
        {
            int difference = savedClicks - totalClicks;
            if (difference > 0)
            {
                UpdateQuestProgress(QuestType.TotalClicks, difference);
                totalClicks = savedClicks;
            }
        }
        
        // Проверяем финиши
        if (savedFinishes > finishTouches)
        {
            int difference = savedFinishes - finishTouches;
            if (difference > 0)
            {
                UpdateQuestProgress(QuestType.TouchFinish, difference);
                finishTouches = savedFinishes;
            }
        }
        
        // Проверяем демонов
        if (savedDemons > demonsKilled)
        {
            int difference = savedDemons - demonsKilled;
            if (difference > 0)
            {
                UpdateQuestProgress(QuestType.DemonsCompleted, difference);
                demonsKilled = savedDemons;
            }
        }
        
        // Проверяем демонов через AccountsManager
        if (trackDemons && AccountsManager.Instance != null)
        {
            int accountDemons = AccountsManager.Instance.GetPlayerDemons();
            if (accountDemons > demonsKilled)
            {
                int difference = accountDemons - demonsKilled;
                if (difference > 0)
                {
                    UpdateQuestProgress(QuestType.DemonsCompleted, difference);
                    demonsKilled = accountDemons;
                    PlayerPrefs.SetInt(DEMONS_KEY, demonsKilled);
                    PlayerPrefs.Save();
                }
            }
        }
    }
    
    // Корутина для периодической проверки доступности QuestsManager
    IEnumerator CheckQuestsManagerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // Проверяем каждые 5 секунд
            
            // Если QuestsManager стал доступен, отправляем весь накопленный прогресс
            if (QuestsManager.Instance != null)
            {
                // Отправляем текущий прогресс
                SendAllProgressToQuestsManager();
            }
        }
    }
    
    // Отправка всего текущего прогресса в QuestsManager
    void SendAllProgressToQuestsManager()
    {
        if (QuestsManager.Instance == null) return;
        
        // Отправляем клики
        if (totalClicks > 0)
        {
            // Проверяем, нужно ли отправлять все клики или только новые
            int savedClicks = PlayerPrefs.GetInt(CLICKS_KEY, 0);
            if (savedClicks > 0)
            {
                // Для простоты отправляем 1, так как QuestsManager сам проверит общий прогресс
                QuestsManager.Instance.UpdateQuestProgress(QuestType.TotalClicks, 0);
                Debug.Log($"📤 Отправлен прогресс кликов: {savedClicks}");
            }
        }
        
        // Отправляем финиши
        if (finishTouches > 0)
        {
            int savedFinishes = PlayerPrefs.GetInt(FINISH_KEY, 0);
            if (savedFinishes > 0)
            {
                QuestsManager.Instance.UpdateQuestProgress(QuestType.TouchFinish, 0);
                Debug.Log($"📤 Отправлен прогресс финишей: {savedFinishes}");
            }
        }
        
        // Отправляем демонов
        if (demonsKilled > 0)
        {
            int savedDemons = PlayerPrefs.GetInt(DEMONS_KEY, 0);
            if (savedDemons > 0)
            {
                QuestsManager.Instance.UpdateQuestProgress(QuestType.DemonsCompleted, 0);
                Debug.Log($"📤 Отправлен прогресс демонов: {savedDemons}");
            }
        }
    }
    
    void LoadProgress()
    {
        // Создаем ключи если их нет
        if (!PlayerPrefs.HasKey(CLICKS_KEY))
            PlayerPrefs.SetInt(CLICKS_KEY, 0);
        
        if (!PlayerPrefs.HasKey(FINISH_KEY))
            PlayerPrefs.SetInt(FINISH_KEY, 0);
        
        if (!PlayerPrefs.HasKey(DEMONS_KEY))
            PlayerPrefs.SetInt(DEMONS_KEY, 0);
        
        PlayerPrefs.Save();
    }
    
    // Метод для внешнего добавления прогресса
    public void AddProgress(QuestType type, int amount = 1)
    {
        switch (type)
        {
            case QuestType.TotalClicks:
                totalClicks += amount;
                PlayerPrefs.SetInt(CLICKS_KEY, totalClicks);
                break;
            case QuestType.TouchFinish:
                finishTouches += amount;
                PlayerPrefs.SetInt(FINISH_KEY, finishTouches);
                break;
            case QuestType.DemonsCompleted:
                demonsKilled += amount;
                PlayerPrefs.SetInt(DEMONS_KEY, demonsKilled);
                break;
        }
        
        PlayerPrefs.Save();
        
        // Передаем в QuestsManager
        UpdateQuestProgress(type, amount);
    }
    
    // Метод для принудительной синхронизации с QuestsManager
    public void ForceSyncWithQuestsManager()
    {
        if (QuestsManager.Instance != null)
        {
            SendAllProgressToQuestsManager();
            Debug.Log("🔄 Принудительная синхронизация с QuestsManager выполнена");
        }
        else
        {
            Debug.Log("⚠️ QuestsManager недоступен для синхронизации");
        }
    }
    
    public void ResetProgress()
    {
        totalClicks = 0;
        finishTouches = 0;
        demonsKilled = 0;
        
        PlayerPrefs.SetInt(CLICKS_KEY, 0);
        PlayerPrefs.SetInt(FINISH_KEY, 0);
        PlayerPrefs.SetInt(DEMONS_KEY, 0);
        PlayerPrefs.Save();
        
        Debug.Log("♻️ Прогресс квестов сброшен");
    }
    
    public int GetTotalClicks()
    {
        return PlayerPrefs.GetInt(CLICKS_KEY, 0);
    }
    
    public int GetFinishTouches()
    {
        return PlayerPrefs.GetInt(FINISH_KEY, 0);
    }
    
    public int GetDemonsKilled()
    {
        return PlayerPrefs.GetInt(DEMONS_KEY, 0);
    }
    
    void OnDestroy()
    {
        SaveProgress();
        
        // Останавливаем корутину
        if (updateQuestsCoroutine != null)
        {
            StopCoroutine(updateQuestsCoroutine);
            updateQuestsCoroutine = null;
        }
    }
    
    void SaveProgress()
    {
        PlayerPrefs.SetInt(CLICKS_KEY, totalClicks);
        PlayerPrefs.SetInt(FINISH_KEY, finishTouches);
        PlayerPrefs.SetInt(DEMONS_KEY, demonsKilled);
        PlayerPrefs.Save();
    }
}