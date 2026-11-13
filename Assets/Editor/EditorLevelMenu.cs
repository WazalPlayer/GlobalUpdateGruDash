using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class EditorLevelMenu : MonoBehaviour
{
    public Transform content;
    public GameObject levelButtonPrefab;
    public Text noLevelsText;

    void Start()
    {
        LoadLevels();
    }

    void LoadLevels()
    {
        string path = Application.persistentDataPath;
        string[] files = Directory.GetFiles(path, "Level_*.json");

        if (files.Length == 0)
        {
            noLevelsText.gameObject.SetActive(true);
            return;
        }

        noLevelsText.gameObject.SetActive(false);

        foreach (string file in files)
        {
            GameObject button = Instantiate(levelButtonPrefab, content);
            string fileName = Path.GetFileNameWithoutExtension(file);
            button.GetComponentInChildren<Text>().text = fileName;
            button.GetComponent<Button>().onClick.AddListener(() => LoadLevel(file));
        }
    }

    void LoadLevel(string filePath)
    {
        string json = File.ReadAllText(filePath);
        EditorLevelFile data = JsonUtility.FromJson<EditorLevelFile>(json);
        
        Debug.Log("Загружаем уровень: " + data.levelName);
    }
}