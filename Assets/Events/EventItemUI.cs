using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventItemUI : MonoBehaviour
{
    public int eventId { get; private set; }

    [Header("UI Elements")]
    public TextMeshProUGUI eventNameText;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI progressText;
    public Slider progressSlider;
    public Image medalIcon;
    public Image eventImage;
    public Button depositButton;
    public Button claimButton;
    public GameObject claimedLabel;

    private EventData currentEvent;

    void Awake()
    {
        // Дополнительная защита: если кнопки не назначены в инспекторе, ищем их на дочерних объектах
        if (depositButton == null) depositButton = GetComponentInChildren<Button>("DepositButton");
        if (claimButton == null) claimButton = GetComponentInChildren<Button>("ClaimButton");
        if (medalIcon == null) medalIcon = GetComponentInChildren<Image>("MedalIcon");
        if (eventImage == null) eventImage = GetComponentInChildren<Image>("EventImage");
    }

    public void Initialize(EventData eventData, Sprite secondaryImage)
    {
        currentEvent = eventData;
        eventId = eventData.id;

        // Название и цель
        if (eventNameText != null) eventNameText.text = eventData.event_name;
        if (goalText != null) goalText.text = $"Цель: {eventData.goal}";
        
        // Прогресс
        UpdateProgress(eventData.current_deposit, eventData.goal);

        // Иконка медали (с отладочным логом)
        if (medalIcon != null)
        {
            Sprite medalSpr = MedalsManager.Instance?.GetMedalSprite(eventData.medal_id);
            if (medalSpr != null)
                medalIcon.sprite = medalSpr;
            else
                Debug.LogWarning($"[EventItemUI] Медаль ID {eventData.medal_id} не найдена в MedalsManager");
        }

        // Второстепенное фото
        if (eventImage != null && secondaryImage != null)
            eventImage.sprite = secondaryImage;

        // Привязываем обработчики кнопок
        if (depositButton != null)
        {
            depositButton.onClick.RemoveAllListeners(); // на случай повторной инициализации
            depositButton.onClick.AddListener(() => {
                if (EventsSystemManager.Instance != null)
                    EventsSystemManager.Instance.ShowDepositPanel(this);
                else
                    Debug.LogError("EventsSystemManager.Instance == null!");
            });
        }
        else
            Debug.LogError($"[EventItemUI] depositButton не назначен в префабе для ивента {eventData.event_name}");

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => {
                if (EventsSystemManager.Instance != null)
                    EventsSystemManager.Instance.ClaimReward(eventId, currentEvent.medal_id, this);
                else
                    Debug.LogError("EventsSystemManager.Instance == null!");
            });
        }
        else
            Debug.LogError($"[EventItemUI] claimButton не назначен в префабе для ивента {eventData.event_name}");
    }

    public void UpdateProgress(int current, int goal)
    {
        if (progressSlider != null) progressSlider.value = (float)current / goal;
        if (progressText != null) progressText.text = $"{current} / {goal}";
    }

    public void SetDepositButtonActive(bool active)
    {
        if (depositButton != null) depositButton.gameObject.SetActive(active);
    }

    public void SetClaimButtonActive(bool active)
    {
        if (claimButton != null) claimButton.gameObject.SetActive(active);
    }

    public void SetClaimedLabel(bool claimed)
    {
        if (claimedLabel != null) claimedLabel.SetActive(claimed);
    }

    public void DisableClaimButton()
    {
        SetClaimButtonActive(false);
        SetClaimedLabel(true);
    }

    // Вспомогательный метод для поиска компонента по имени (если не назначено в инспекторе)
    private T GetComponentInChildren<T>(string name) where T : Component
    {
        foreach (T comp in GetComponentsInChildren<T>(true))
        {
            if (comp.gameObject.name == name)
                return comp;
        }
        return null;
    }
}