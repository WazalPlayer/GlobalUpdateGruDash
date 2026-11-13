using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TranslatableText : MonoBehaviour
{
    [Header("Настройки перевода")]
    public string translationKey;

    private Text uiText;
    private TextMeshProUGUI tmpText;
    private TextMeshPro tmpWorldText;

    void Start()
    {
        // Получаем компоненты текста
        uiText = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();
        tmpWorldText = GetComponent<TextMeshPro>();

        // Первоначальное обновление текста
        UpdateText();
    }

    public void UpdateText()
    {
        string translatedText = TranslationManager.Instance.GetText(translationKey);

        if (uiText != null)
            uiText.text = translatedText;
        
        if (tmpText != null)
            tmpText.text = translatedText;
            
        if (tmpWorldText != null)
            tmpWorldText.text = translatedText;
    }
}