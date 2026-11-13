using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuestPrefabController : MonoBehaviour
{
    [Header("UI элементы")]
    public Text titleText;
    public Text descriptionText;
    public Text progressText;
    public Slider progressSlider;
    public Text rewardText;
    public Image backgroundImage;
    public GameObject completedPanel;
    public Button claimButton;
    public Text claimButtonText;
    public GameObject claimedPanel;

    [Header("Цвета")]
    public Color activeColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color completedColor = new Color(0.4f, 0.8f, 0.4f, 1f);
    public Color claimedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private QuestData currentQuestData;

    void Start()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClick);
        }
    }

    public void Initialize(QuestData questData)
    {
        if (questData == null)
        {
            Debug.LogError("❌ QuestPrefabController: QuestData is null!");
            return;
        }
        
        currentQuestData = questData;
        
        // Проверяем ID квеста
        if (currentQuestData.id <= 0)
        {
            Debug.LogError($"❌ QuestPrefabController: Получен квест с ID={currentQuestData.id}. Title: '{currentQuestData.title}'");
            // Продолжаем, но предупреждаем
        }
        
        UpdateUI();
        
        Debug.Log($"✅ QuestPrefabController: Инициализирован квест ID={currentQuestData.id}, Title='{currentQuestData.title}'");
    }

    void UpdateUI()
    {
        if (currentQuestData == null)
        {
            Debug.LogError("❌ QuestPrefabController: currentQuestData is null!");
            return;
        }

        // Заголовок
        if (titleText != null)
            titleText.text = currentQuestData.title;
        
        // Описание
        if (descriptionText != null)
        {
            string typeDescription = GetQuestTypeDescription(currentQuestData.questType);
            descriptionText.text = $"{typeDescription}: {currentQuestData.currentProgress}/{currentQuestData.targetCount}";
        }
        
        // Прогресс
        if (progressSlider != null)
        {
            float progress = currentQuestData.targetCount > 0 ? 
                (float)currentQuestData.currentProgress / currentQuestData.targetCount : 0f;
            progressSlider.value = progress;
        }
        
        if (progressText != null)
            progressText.text = $"{currentQuestData.currentProgress}/{currentQuestData.targetCount}";
        
        // Награда
        if (rewardText != null)
            rewardText.text = $"+{currentQuestData.rewardStars} ⭐";
        
        // Статус выполнения
        bool isCompleted = currentQuestData.isCompleted;
        bool isClaimed = currentQuestData.rewardClaimed;
        
        Debug.Log($"🔄 QuestPrefabController UpdateUI: Квест '{currentQuestData.title}' - Completed: {isCompleted}, Claimed: {isClaimed}");
        
        // ГАРАНТИРОВАННОЕ скрытие кнопки Claim, если награда уже получена
        if (isClaimed)
        {
            // Скрываем кнопку и панель "Выполнено"
            if (completedPanel != null)
                completedPanel.SetActive(false);
            
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(false);
                claimButton.interactable = false;
            }
            
            // Показываем панель "Получено"
            if (claimedPanel != null)
                claimedPanel.SetActive(true);
        }
        else
        {
            // Показываем кнопку только если квест выполнен И награда еще не получена
            if (completedPanel != null)
                completedPanel.SetActive(isCompleted);
            
            if (claimButton != null)
            {
                bool shouldShowClaimButton = isCompleted && !isClaimed;
                claimButton.gameObject.SetActive(shouldShowClaimButton);
                claimButton.interactable = shouldShowClaimButton;
                
                if (claimButtonText != null)
                {
                    claimButtonText.text = "Забрать";
                }
            }
            
            // Скрываем панель "Получено"
            if (claimedPanel != null)
                claimedPanel.SetActive(false);
        }
        
        // Цвет фона в зависимости от статуса
        if (backgroundImage != null)
        {
            if (isClaimed)
            {
                backgroundImage.color = claimedColor;
            }
            else if (isCompleted)
            {
                backgroundImage.color = completedColor;
            }
            else
            {
                backgroundImage.color = activeColor;
            }
        }
    }

    string GetQuestTypeDescription(QuestType type)
    {
        switch (type)
        {
            case QuestType.TouchFinish:
                return "Достигнуть финиша";
            case QuestType.TotalClicks:
                return "Сделать прыжков";
            case QuestType.DemonsCompleted:
                return "Пройти демонов";
            default:
                return "Выполнить задание";
        }
    }

    // Вызывается при нажатии кнопки "Забрать"
    public void OnClaimButtonClick()
    {
        if (currentQuestData == null)
        {
            Debug.LogError("❌ QuestPrefabController: currentQuestData is null!");
            return;
        }
        
        Debug.Log($"🎯 QuestPrefabController: Нажата кнопка 'Забрать' для квеста: '{currentQuestData.title}', ID: {currentQuestData.id}");
        
        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: если награда уже получена, не обрабатываем нажатие
        if (currentQuestData.rewardClaimed)
        {
            Debug.LogWarning($"⚠️ QuestPrefabController: Награда уже получена для квеста '{currentQuestData.title}'");
            UpdateUI(); // Обновляем UI, чтобы скрыть кнопку
            return;
        }
        
        // Проверка ID квеста
        if (currentQuestData.id <= 0)
        {
            Debug.LogError($"❌ QuestPrefabController: Неверный ID квеста: {currentQuestData.id}. Квест: '{currentQuestData.title}'");
            return;
        }
        
        // Проверка доступности QuestsManager
        if (QuestsManager.Instance == null)
        {
            Debug.LogError("❌ QuestPrefabController: QuestsManager.Instance не доступен!");
            return;
        }
        
        // Проверяем, можно ли получить награду
        if (currentQuestData.isCompleted && !currentQuestData.rewardClaimed)
        {
            // Вызываем получение награды
            QuestsManager.Instance.ClaimQuestReward(currentQuestData.id);
            
            // НЕМЕДЛЕННО обновляем UI после получения награды
            StartCoroutine(UpdateUIAfterDelay());
            
            Debug.Log($"✅ QuestPrefabController: Награда запрошена для квеста: {currentQuestData.title}");
        }
        else
        {
            string status = currentQuestData.rewardClaimed ? "уже получена" : "квест не выполнен";
            Debug.Log($"⚠️ QuestPrefabController: Награда не может быть получена: {status}");
            
            // Принудительно обновляем UI
            UpdateUI();
        }
    }

    // Обновление UI с небольшой задержкой для гарантии обновления данных
    IEnumerator UpdateUIAfterDelay()
    {
        yield return new WaitForSeconds(0.1f); // Небольшая задержка
        UpdateUI();
    }

    // Получение текущих данных квеста
    public QuestData GetCurrentQuestData()
    {
        return currentQuestData;
    }

    // Получение ID квеста
    public int GetQuestId()
    {
        return currentQuestData?.id ?? 0;
    }
}