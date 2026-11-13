using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LevelMenu : MonoBehaviour
{
    [Header("UI элементы")]
    public GameObject localLevelsPanel;
    public Transform contentParent;
    public GameObject levelEntryPrefab;
    public Button closeButton;
    public Text statusText;
    public Button refreshButton;

    private LevelCreator levelCreator;

    void Start()
    {
        levelCreator = FindObjectOfType<LevelCreator>();
        if (levelCreator == null)
            Debug.LogError("LevelCreator не найден в сцене!");

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshList);

        localLevelsPanel.SetActive(false);
    }

    public void ShowLocalLevels()
    {
        localLevelsPanel.SetActive(true);
        RefreshList();
    }

    void ClosePanel()
    {
        localLevelsPanel.SetActive(false);
    }

    public void RefreshList()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        string levelsPath = Path.Combine(Application.persistentDataPath, "LocalLevels");
        if (!Directory.Exists(levelsPath))
        {
            Directory.CreateDirectory(levelsPath);
            ShowStatus("Нет сохранённых уровней");
            return;
        }

        string[] files = Directory.GetFiles(levelsPath, "*.json");
        if (files.Length == 0)
        {
            ShowStatus("Нет сохранённых уровней");
            return;
        }

        bool anyLoaded = false;
        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            LocalLevelMeta meta = JsonUtility.FromJson<LocalLevelMeta>(json);
            if (meta == null || string.IsNullOrEmpty(meta.level_data))
                continue;

            GameObject entryObj = Instantiate(levelEntryPrefab, contentParent);
            LocalLevelEntry entry = entryObj.GetComponent<LocalLevelEntry>();
            if (entry != null)
            {
                entry.Setup(meta, file, this);
            }
            anyLoaded = true;
        }

        if (!anyLoaded)
            ShowStatus("Нет сохранённых уровней");
        else
            ShowStatus($"Загружено {contentParent.childCount} уровней");
    }

    public void LoadLevel(string filePath)
    {
        if (levelCreator != null)
        {
            levelCreator.LoadLocalLevel(filePath);
            ClosePanel();
        }
        else
        {
            Debug.LogError("Нет ссылки на LevelCreator");
        }
    }

    public void DeleteLevel(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                ShowStatus("Уровень удалён");
                RefreshList();   // Обновляем список после удаления
            }
            else
            {
                ShowStatus("Файл не найден");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка удаления: {e.Message}");
            ShowStatus("Ошибка при удалении");
        }
    }

    void ShowStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}