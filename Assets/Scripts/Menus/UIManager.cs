using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Главное меню")]
    public GameObject mainMenuPanel;
    public Button playButton;
    public Button levelSelectButton;
    public Button settingsButton;
    public Button quitButton;
    
    [Header("Выбор уровня")]
    public GameObject levelSelectPanel;
    public Button[] levelButtons;
    public Button backFromLevelsButton;
    
    [Header("Настройки")]
    public GameObject settingsPanel;
    public Toggle soundToggle;
    public Toggle musicToggle;
    public Button iconSelectButton;
    public Button musicSelectButton;
    public Button backFromSettingsButton;
    
    [Header("Выбор иконки")]
    public GameObject iconSelectPanel;
    public Image[] iconImages;
    public Button[] iconSelectButtons;
    public Button backFromIconsButton;
    
    [Header("Выбор музыки")]
    public GameObject musicSelectPanel;
    public TextMeshProUGUI[] musicTrackNames;
    public Button[] musicSelectButtons;
    public Button backFromMusicButton;
    
    [Header("Игровой UI")]
    public GameObject gameUIPanel;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI levelText;
    
    [Header("Завершение уровня")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteText;
    public Button nextLevelButton;
    public Button mainMenuButton;
    
    private void Start()
    {
        // Инициализация кнопок
        playButton.onClick.AddListener(() => StartGame());
        levelSelectButton.onClick.AddListener(() => ShowLevelSelect());
        settingsButton.onClick.AddListener(() => ShowSettings());
        quitButton.onClick.AddListener(() => QuitGame());
        
        backFromLevelsButton.onClick.AddListener(() => ShowMainMenu());
        backFromSettingsButton.onClick.AddListener(() => ShowMainMenu());
        backFromIconsButton.onClick.AddListener(() => ShowSettings());
        backFromMusicButton.onClick.AddListener(() => ShowSettings());
        
        iconSelectButton.onClick.AddListener(() => ShowIconSelect());
        musicSelectButton.onClick.AddListener(() => ShowMusicSelect());
        
        nextLevelButton.onClick.AddListener(() => LoadNextLevel());
        mainMenuButton.onClick.AddListener(() => ShowMainMenu());
        
        // Инициализация кнопок уровней
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i + 1;
            levelButtons[i].onClick.AddListener(() => LoadLevel(levelIndex));
        }
        
        // Инициализация кнопок выбора иконок
        for (int i = 0; i < iconSelectButtons.Length; i++)
        {
            int iconIndex = i;
            iconSelectButtons[i].onClick.AddListener(() => SelectIcon(iconIndex));
        }
        
        // Инициализация кнопок выбора музыки
        for (int i = 0; i < musicSelectButtons.Length; i++)
        {
            int musicIndex = i;
            musicSelectButtons[i].onClick.AddListener(() => SelectMusic(musicIndex));
        }
        
        // Инициализация переключателей
        soundToggle.isOn = GameManager.Instance.isSoundOn;
        musicToggle.isOn = GameManager.Instance.isMusicOn;
        
        soundToggle.onValueChanged.AddListener((value) => GameManager.Instance.ToggleSound());
        musicToggle.onValueChanged.AddListener((value) => GameManager.Instance.ToggleMusic());
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Обновление текста монет
        coinsText.text = $"Монеты: {GameManager.Instance.totalCoins}";
        
        // Обновление текста уровня
        levelText.text = $"Уровень: {GameManager.Instance.currentLevel}";
        
        // Обновление иконок
        for (int i = 0; i < iconImages.Length; i++)
        {
            if (i < GameManager.Instance.playerIcons.Count)
            {
                iconImages[i].sprite = GameManager.Instance.playerIcons[i];
                iconImages[i].gameObject.SetActive(true);
            }
            else
            {
                iconImages[i].gameObject.SetActive(false);
            }
        }
        
        // Обновление названий треков
        for (int i = 0; i < musicTrackNames.Length; i++)
        {
            if (i < GameManager.Instance.musicTracks.Count)
            {
                musicTrackNames[i].text = GameManager.Instance.musicTracks[i].name;
                musicTrackNames[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                musicTrackNames[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }
    
    private void StartGame()
    {
        GameManager.Instance.LoadLevel(GameManager.Instance.currentLevel);
    }
    
    private void LoadLevel(int level)
    {
        GameManager.Instance.LoadLevel(level);
    }
    
    private void LoadNextLevel()
    {
        if (GameManager.Instance.HasNextLevel)
        {
            GameManager.Instance.LoadLevel(GameManager.Instance.currentLevel + 1);
        }
        else
        {
            ShowMainMenu();
        }
    }
    
    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        iconSelectPanel.SetActive(false);
        musicSelectPanel.SetActive(false);
        gameUIPanel.SetActive(false);
        levelCompletePanel.SetActive(false);
    }
    
    private void ShowLevelSelect()
    {
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        
        // Активация только доступных уровней
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i + 1;
            bool exists = level <= GameManager.Instance.maxLevel;
            levelButtons[i].gameObject.SetActive(exists);
            levelButtons[i].interactable = exists && level <= GameManager.Instance.currentLevel;
        }
    }
    
    private void ShowSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    private void ShowIconSelect()
    {
        settingsPanel.SetActive(false);
        iconSelectPanel.SetActive(true);
    }
    
    private void ShowMusicSelect()
    {
        settingsPanel.SetActive(false);
        musicSelectPanel.SetActive(true);
    }
    
    private void SelectIcon(int index)
    {
        GameManager.Instance.SelectIcon(index);
        UpdateUI();
    }
    
    private void SelectMusic(int index)
    {
        GameManager.Instance.SelectMusic(index);
        UpdateUI();
    }
    
    private void QuitGame()
    {
        Application.Quit();
    }
    
    public void ShowLevelComplete()
    {
        levelCompletePanel.SetActive(true);
        levelCompleteText.text = $"Уровень {GameManager.Instance.currentLevel} пройден!";
        
        // На последнем уровне кнопки "дальше" не должно быть
        nextLevelButton.gameObject.SetActive(GameManager.Instance.HasNextLevel);
    }
}