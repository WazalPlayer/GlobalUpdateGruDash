using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class TranslationManager : MonoBehaviour
{
    [System.Serializable]
    public class TranslationItem
    {
        public string key;
        public string russianText;
        public string englishText;
    }

    [Header("Настройки перевода")]
    public List<TranslationItem> translations = new List<TranslationItem>();
    
    public enum Language
    {
        Russian,
        English
    }
    
    private Language currentLanguage = Language.Russian;
    private Dictionary<string, TranslationItem> translationDict;

    // Ключ для сохранения языка
    private const string LANGUAGE_KEY = "CurrentLanguage";

    public static TranslationManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionary();
            LoadLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeDictionary()
    {
        translationDict = new Dictionary<string, TranslationItem>();
        foreach (var item in translations)
        {
            translationDict[item.key] = item;
        }
    }

    void LoadLanguage()
    {
        // Загружаем сохраненный язык, по умолчанию русский
        int savedLanguage = PlayerPrefs.GetInt(LANGUAGE_KEY, 0);
        currentLanguage = (Language)savedLanguage;
        
        // Обновляем все тексты
        UpdateAllTexts();
    }

    void SaveLanguage()
    {
        // Сохраняем текущий язык
        PlayerPrefs.SetInt(LANGUAGE_KEY, (int)currentLanguage);
        PlayerPrefs.Save();
    }

    public void SetLanguage(Language language)
    {
        if (currentLanguage == language) return;
        
        currentLanguage = language;
        SaveLanguage();
        UpdateAllTexts();
        
        Debug.Log($"Язык изменен на: {currentLanguage}");
    }

    public string GetText(string key)
    {
        if (translationDict.ContainsKey(key))
        {
            switch (currentLanguage)
            {
                case Language.Russian:
                    return translationDict[key].russianText;
                case Language.English:
                    return translationDict[key].englishText;
            }
        }
        Debug.LogWarning($"Перевод не найден для ключа: {key}");
        return $"MISSING: {key}";
    }

    void UpdateAllTexts()
    {
        // Находим все текстовые элементы с компонентом TranslatableText
        TranslatableText[] allTexts = FindObjectsOfType<TranslatableText>(true);
        foreach (var textComponent in allTexts)
        {
            textComponent.UpdateText();
        }
    }

    // Метод для получения текущего языка
    public Language GetCurrentLanguage()
    {
        return currentLanguage;
    }

    // Методы для кнопок UI
    public void SetRussian()
    {
        SetLanguage(Language.Russian);
    }

    public void SetEnglish()
    {
        SetLanguage(Language.English);
    }

    // Метод для переключения языка
    public void ToggleLanguage()
    {
        if (currentLanguage == Language.Russian)
            SetEnglish();
        else
            SetRussian();
    }
}