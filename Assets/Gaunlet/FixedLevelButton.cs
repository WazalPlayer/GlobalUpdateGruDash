using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FixedLevelButton : MonoBehaviour
{
    [Header("Настройки уровня")]
    public string levelName = "";          // Уникальное имя уровня (должно совпадать с сохранением)
    public string displayName = "Уровень"; // Что показываем на кнопке
    public int sceneId = 1;               // ID сцены в Build Settings
    
    [Header("Иконки")]
    public Image buttonImage;             // Основное изображение кнопки
    public Sprite lockSprite;             // Спрайт для закрытого уровня
    public Sprite unlockSprite;           // Спрайт для открытого уровня
    
    [Header("Элементы UI")]
    public Text levelText;                // Текст на кнопке
    public GameObject lockOverlay;        // Объект замка поверх кнопки
    public Button button;                 // Компонент Button
    
    [Header("Цвета")]
    public Color normalColor = Color.white;
    public Color disabledColor = Color.gray;
    
    private void Start()
    {
        // Находим компоненты если не назначены
        if (button == null) button = GetComponent<Button>();
        if (levelText == null) levelText = GetComponentInChildren<Text>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        
        // Устанавливаем текст
        if (levelText != null)
        {
            levelText.text = !string.IsNullOrEmpty(displayName) ? displayName : levelName;
        }
        
        // Обновляем состояние кнопки
        UpdateButtonState();
        
        // Назначаем обработчик клика
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }
    
    private void OnEnable()
    {
        // При каждом включении объекта обновляем состояние
        UpdateButtonState();
    }
    
    // Обновляем состояние кнопки
    public void UpdateButtonState()
    {
        if (button == null) return;
        
        // Проверяем открыт ли уровень
        bool isOpen = IsLevelOpen();
        
        // Обновляем интерактивность кнопки
        button.interactable = isOpen;
        
        // Меняем цвет кнопки
        if (buttonImage != null)
        {
            buttonImage.color = isOpen ? normalColor : disabledColor;
        }
        
        // МЕНЯЕМ ИКОНКУ В ЗАВИСИМОСТИ ОТ СОСТОЯНИЯ
        if (buttonImage != null)
        {
            if (isOpen && unlockSprite != null)
            {
                buttonImage.sprite = unlockSprite;
            }
            else if (!isOpen && lockSprite != null)
            {
                buttonImage.sprite = lockSprite;
            }
        }
        
        // Показываем/скрываем замок поверх
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!isOpen);
        }
        
        // Логируем для отладки
        Debug.Log($"Кнопка '{levelName}': {(isOpen ? "ОТКРЫТА" : "ЗАКРЫТА")}, PlayerPrefs: {PlayerPrefs.GetInt("Open_" + levelName, 0)}");
    }
    
    // Проверяем открыт ли уровень
    private bool IsLevelOpen()
    {
        // Проверяем в PlayerPrefs
        return PlayerPrefs.GetInt("Open_" + levelName, 0) == 1;
    }
    
    // При клике на кнопку
    public void OnClick()
    {
        if (IsLevelOpen())
        {
            Debug.Log($"Загружаем сцену {sceneId} ({levelName})");
            SceneManager.LoadScene(sceneId);
        }
        else
        {
            Debug.Log($"Уровень {levelName} закрыт!");
        }
    }
    
    // Принудительно открыть уровень
    public void ForceOpen()
    {
        PlayerPrefs.SetInt("Open_" + levelName, 1);
        PlayerPrefs.Save();
        UpdateButtonState();
        Debug.Log($"Уровень {levelName} принудительно открыт");
    }
    
    // Принудительно закрыть уровень
    public void ForceClose()
    {
        PlayerPrefs.SetInt("Open_" + levelName, 0);
        PlayerPrefs.Save();
        UpdateButtonState();
        Debug.Log($"Уровень {levelName} принудительно закрыт");
    }
}