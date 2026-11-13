using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class DemonManager : MonoBehaviour
{
    public static DemonManager Instance { get; private set; }
    
    [Header("Настройки демонов")]
    public List<string> availableDemonNames = new List<string>();
    
    [Header("UI")]
    public GameObject demonCountUI;
    public Text demonCountText;
    
    void Awake()
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
    }
    
    void Start()
    {
        UpdateDemonCountUI();
    }
    
    public int GetTotalDemons()
    {
        return PlayerPrefs.GetInt("PlayerDemons", 0);
    }
    
    public List<string> GetCompletedDemons()
    {
        List<string> completedDemons = new List<string>();
        
        foreach (string demonName in availableDemonNames)
        {
            string demonKey = $"Demon_{demonName}";
            if (PlayerPrefs.HasKey(demonKey) && PlayerPrefs.GetInt(demonKey) == 1)
            {
                completedDemons.Add(demonName);
            }
        }
        
        return completedDemons;
    }
    
    public bool IsDemonCompleted(string demonName)
    {
        string demonKey = $"Demon_{demonName}";
        return PlayerPrefs.HasKey(demonKey) && PlayerPrefs.GetInt(demonKey) == 1;
    }
    
    public void UpdateDemonCountUI()
    {
        if (demonCountUI != null && demonCountText != null)
        {
            int totalDemons = GetTotalDemons();
            demonCountText.text = $"{totalDemons}";
            demonCountUI.SetActive(totalDemons > 0);
        }
    }
    
    public void ResetAllDemons()
    {
        // Сбрасываем счетчик демонов
        PlayerPrefs.SetInt("PlayerDemons", 0);
        
        // Сбрасываем все пройденные демоны
        foreach (string demonName in availableDemonNames)
        {
            string demonKey = $"Demon_{demonName}";
            PlayerPrefs.DeleteKey(demonKey);
        }
        
        PlayerPrefs.Save();
        
        Debug.Log("♻️ Все демоны сброшены");
        UpdateDemonCountUI();
    }
}