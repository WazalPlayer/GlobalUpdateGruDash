using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Настройки игры")]
    public int totalCoins = 0;
    public int currentLevel = 1;
    public int maxLevel = 10;
    public int selectedIcon = 0;
    public int selectedMusic = 0;
    
    [Header("Списки контента")]
    public List<Sprite> playerIcons;
    public List<AudioClip> musicTracks;
    
    [Header("Текущие настройки")]
    public bool isSoundOn = true;
    public bool isMusicOn = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        DetectMaxLevel();
        LoadGameData();
    }

    // Считаем реальное количество уровней по сценам "Level_N" из Build Settings,
    // чтобы не загружать несуществующие сцены и не показывать лишние кнопки.
    private void DetectMaxLevel()
    {
        int count = 0;
        while (Application.CanStreamedLevelBeLoaded("Level_" + (count + 1)))
        {
            count++;
        }
        if (count > 0)
        {
            maxLevel = count;
        }
    }

    public bool IsFirstLevel => currentLevel <= 1;
    public bool HasNextLevel => currentLevel < maxLevel;

    private void LoadGameData()
    {
        // Загрузка сохраненных данных
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        selectedIcon = PlayerPrefs.GetInt("SelectedIcon", 0);
        selectedMusic = PlayerPrefs.GetInt("SelectedMusic", 0);
        isSoundOn = PlayerPrefs.GetInt("IsSoundOn", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
    }
    
    public void SaveGameData()
    {
        // Сохранение данных игры
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("SelectedIcon", selectedIcon);
        PlayerPrefs.SetInt("SelectedMusic", selectedMusic);
        PlayerPrefs.SetInt("IsSoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.SetInt("IsMusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public void LoadLevel(int level)
    {
        if (level >= 1 && level <= maxLevel && Application.CanStreamedLevelBeLoaded("Level_" + level))
        {
            currentLevel = level;
            SceneManager.LoadScene("Level_" + level);
        }
        else
        {
            Debug.LogWarning("Уровень " + level + " не найден в Build Settings");
        }
    }
    
    public void CompleteLevel(int coinsCollected)
    {
        totalCoins += coinsCollected;
        if (currentLevel < maxLevel)
        {
            currentLevel++;
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        }
        SaveGameData();
    }
    
    public void SelectIcon(int iconIndex)
    {
        if (iconIndex >= 0 && iconIndex < playerIcons.Count)
        {
            selectedIcon = iconIndex;
            SaveGameData();
        }
    }
    
    public void SelectMusic(int musicIndex)
    {
        if (musicIndex >= 0 && musicIndex < musicTracks.Count)
        {
            selectedMusic = musicIndex;
            SaveGameData();
        }
    }
    
    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        SaveGameData();
    }
    
    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        SaveGameData();
    }
}