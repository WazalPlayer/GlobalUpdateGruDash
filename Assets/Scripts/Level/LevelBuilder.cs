using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LevelBuilder : MonoBehaviour
{
    [Header("Префабы объектов")]
    public GameObject[] objects;
    
    [Header("UI элементы")]
    public Button[] objectButtons;
    public Button saveButton;
    public Button clearButton;
    public InputField nameField;
    public Text counterText;

    private List<GameObject> placedObjects = new List<GameObject>();
    private int selectedIndex = -1;
    private int levelCount = 0;

    void Start()
    {
        levelCount = PlayerPrefs.GetInt("LevelCount", 0);
        
        for (int i = 0; i < objectButtons.Length; i++)
        {
            int index = i;
            objectButtons[i].onClick.AddListener(() => SelectObject(index));
        }

        saveButton.onClick.AddListener(SaveLevel);
        clearButton.onClick.AddListener(ClearAll);
        
        UpdateCounter();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && selectedIndex != -1)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            GameObject newObj = Instantiate(objects[selectedIndex], mousePos, Quaternion.identity);
            placedObjects.Add(newObj);
        }

        if (Input.GetMouseButtonDown(1))
        {
            RemoveObject();
        }
    }

    void SelectObject(int index)
    {
        selectedIndex = index;
        
        for (int i = 0; i < objectButtons.Length; i++)
        {
            objectButtons[i].image.color = i == index ? Color.green : Color.white;
        }
    }

    void RemoveObject()
    {
        if (placedObjects.Count > 0)
        {
            GameObject lastObj = placedObjects[placedObjects.Count - 1];
            placedObjects.Remove(lastObj);
            Destroy(lastObj);
        }
    }

    void ClearAll()
    {
        foreach (GameObject obj in placedObjects)
        {
            Destroy(obj);
        }
        placedObjects.Clear();
    }

    void SaveLevel()
    {
        if (levelCount >= 100) return;

        LevelData data = new LevelData();
        data.levelName = string.IsNullOrEmpty(nameField.text) ? "Level_" + (levelCount + 1) : nameField.text;
        data.objects = new List<ObjectData>();

        foreach (GameObject obj in placedObjects)
        {
            ObjectData objData = new ObjectData();
            objData.objectType = GetObjectType(obj);
            objData.x = obj.transform.position.x;
            objData.y = obj.transform.position.y;
            data.objects.Add(objData);
        }

        string json = JsonUtility.ToJson(data);
        string path = Path.Combine(Application.persistentDataPath, "Level_" + (levelCount + 1) + ".json");
        File.WriteAllText(path, json);

        levelCount++;
        PlayerPrefs.SetInt("LevelCount", levelCount);
        UpdateCounter();
        
        nameField.text = "";
    }

    int GetObjectType(GameObject obj)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (obj.name.StartsWith(objects[i].name))
            {
                return i;
            }
        }
        return -1;
    }

    void UpdateCounter()
    {
        counterText.text = "Уровней: " + levelCount + "/100";
    }
}

[System.Serializable]
public class LevelData
{
    public string levelName;
    public List<ObjectData> objects;
}

[System.Serializable]
public class ObjectData
{
    public int objectType;
    public float x;
    public float y;
}