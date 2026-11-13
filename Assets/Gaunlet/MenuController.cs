using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Кнопки управления")]
    public Button refreshButton;
    public Button resetButton;
    public Button openAllButton;
    
    [Header("Текст прогресса")]
    public Text progressText;
    
    [Header("Автообновление")]
    public bool autoRefreshOnEnable = true;
    
    private void Start()
    {
        // Настраиваем кнопки управления
        SetupControlButtons();
        
        // Первоначальное обновление
        RefreshAllButtons();
    }
    
    private void OnEnable()
    {
        // При каждом открытии меню обновляем все кнопки
        if (autoRefreshOnEnable)
        {
            RefreshAllButtons();
        }
    }
    
    // Настройка кнопок управления
    private void SetupControlButtons()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(RefreshAllButtons);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetAllProgress);
        }
        
        if (openAllButton != null)
        {
            openAllButton.onClick.RemoveAllListeners();
            openAllButton.onClick.AddListener(OpenAllLevels);
        }
    }
    
    // Обновить все кнопки уровней
    public void RefreshAllButtons()
    {
        FixedLevelButton[] levelButtons = FindObjectsOfType<FixedLevelButton>(true);
        
        foreach (FixedLevelButton button in levelButtons)
        {
            button.UpdateButtonState();
        }
        
        UpdateProgressText();
        Debug.Log($"Обновлено {levelButtons.Length} кнопок уровней");
    }
    
    // Обновить текст прогресса
    private void UpdateProgressText()
    {
        if (progressText == null) return;
        
        FixedLevelButton[] buttons = FindObjectsOfType<FixedLevelButton>(true);
        int openCount = 0;
        
        foreach (FixedLevelButton button in buttons)
        {
            if (PlayerPrefs.GetInt("Open_" + button.levelName, 0) == 1)
            {
                openCount++;
            }
        }
        
        progressText.text = $"Прогресс: {openCount}/{buttons.Length}";
    }
    
    // Сбросить весь прогресс
    public void ResetAllProgress()
    {
        // Удаляем все ключи начинающиеся с "Open_"
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        // Открываем первый уровень по умолчанию
        FixedLevelButton[] buttons = FindObjectsOfType<FixedLevelButton>(true);
        if (buttons.Length > 0)
        {
            PlayerPrefs.SetInt("Open_" + buttons[0].levelName, 1);
            PlayerPrefs.Save();
        }
        
        RefreshAllButtons();
        Debug.Log("Весь прогресс сброшен!");
    }
    
    // Открыть все уровни
    public void OpenAllLevels()
    {
        FixedLevelButton[] buttons = FindObjectsOfType<FixedLevelButton>(true);
        
        foreach (FixedLevelButton button in buttons)
        {
            PlayerPrefs.SetInt("Open_" + button.levelName, 1);
        }
        
        PlayerPrefs.Save();
        RefreshAllButtons();
        Debug.Log($"Открыты все {buttons.Length} уровней");
    }
    
    // Проверить состояние сохранений
    public void DebugPlayerPrefs()
    {
        Debug.Log("=== DEBUG PlayerPrefs ===");
        
        FixedLevelButton[] buttons = FindObjectsOfType<FixedLevelButton>(true);
        foreach (FixedLevelButton button in buttons)
        {
            int value = PlayerPrefs.GetInt("Open_" + button.levelName, -1);
            Debug.Log($"{button.levelName}: {value}");
        }
        
        Debug.Log("=========================");
    }
}