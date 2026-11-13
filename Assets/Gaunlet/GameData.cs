using UnityEngine;
using System.Collections.Generic;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }
    
    [System.Serializable]
    public class Level
    {
        public string levelName;           // Уникальное имя уровня
        public int sceneId;                // ID сцены в Build Settings
        public bool isOpenByDefault = false; // Открыт по умолчанию
    }
    
    [Header("Настройка уровней")]
    public List<Level> allLevels = new List<Level>();
    
    [Header("Дополнительные настройки")]
    public bool autoOpenFirstLevel = true; // Автооткрытие 1-го уровня
    public bool openAllLevelsByDefault = true; // Новый игрок сразу видит все уровни открытыми
    
    void Awake()
    {
        // Убираем синглтон и DontDestroyOnLoad
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        // УБИРАЕМ DontDestroyOnLoad - объект будет уничтожаться при загрузке новой сцены
        // DontDestroyOnLoad(gameObject);
        
        InitializePlayerPrefs();
        Debug.Log("GameData инициализирован на сцене: " + gameObject.scene.name);
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // Инициализация PlayerPrefs значениями по умолчанию
    void InitializePlayerPrefs()
    {
        bool hasAnySave = false;
        
        // Проверяем, есть ли хоть одно сохранение
        foreach (Level level in allLevels)
        {
            if (PlayerPrefs.HasKey("Open_" + level.levelName))
            {
                hasAnySave = true;
                break;
            }
        }
        
        // Если сохранений нет - создаем их из настроек по умолчанию
        if (!hasAnySave)
        {
            Debug.Log("Инициализация PlayerPrefs значениями по умолчанию");
            
            // Открываем первый уровень если включена опция
            if (autoOpenFirstLevel && allLevels.Count > 0)
            {
                PlayerPrefs.SetInt("Open_" + allLevels[0].levelName, 1);
            }

            // Все уровни открыты по умолчанию
            if (openAllLevelsByDefault)
            {
                foreach (Level level in allLevels)
                {
                    PlayerPrefs.SetInt("Open_" + level.levelName, 1);
                }
            }
            
            // Устанавливаем значения из isOpenByDefault
            foreach (Level level in allLevels)
            {
                if (level.isOpenByDefault)
                {
                    PlayerPrefs.SetInt("Open_" + level.levelName, 1);
                }
            }
            
            PlayerPrefs.Save();
        }
    }
    
    // Сохраняем что уровень открыт
    public void SaveLevelOpen(string levelName)
    {
        PlayerPrefs.SetInt("Open_" + levelName, 1);
        PlayerPrefs.Save();
        Debug.Log("СОХРАНИЛИ: Уровень " + levelName + " открыт");
    }
    
    // Проверяем открыт ли уровень
    public bool IsLevelOpen(string levelName)
    {
        return PlayerPrefs.GetInt("Open_" + levelName, 0) == 1;
    }
    
    // Получаем ID сцены по имени
    public int GetSceneId(string levelName)
    {
        foreach (Level level in allLevels)
        {
            if (level.levelName == levelName)
                return level.sceneId;
        }
        Debug.LogError("Уровень с именем '" + levelName + "' не найден!");
        return -1;
    }
    
    // Сброс прогресса с восстановлением настроек по умолчанию
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        InitializePlayerPrefs(); // Восстанавливаем настройки по умолчанию
        Debug.Log("Прогресс сброшен!");
    }
    
    // Отобразить текущее состояние всех уровней (для отладки)
    public void DebugLogLevels()
    {
        Debug.Log("=== ТЕКУЩЕЕ СОСТОЯНИЕ УРОВНЕЙ ===");
        foreach (Level level in allLevels)
        {
            bool isOpen = IsLevelOpen(level.levelName);
            Debug.Log(level.levelName + " (Сцена: " + level.sceneId + ") - " + 
                     (isOpen ? "ОТКРЫТ" : "ЗАКРЫТ"));
        }
        Debug.Log("=================================");
    }
}