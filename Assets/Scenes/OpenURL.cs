using UnityEngine;
using UnityEngine.UI;

public class OpenURL : MonoBehaviour
{
    [Header("Ссылка для открытия")]
    public string url = "https://example.com";
    
    [Header("Кнопка (опционально)")]
    public Button button;
    
    private void Start()
    {
        // Если кнопка не назначена в инспекторе, ищем на этом GameObject
        if (button == null)
            button = GetComponent<Button>();
        
        // Добавляем обработчик нажатия
        if (button != null)
        {
            button.onClick.AddListener(OpenLink);
        }
    }
    
    public void OpenLink()
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
            Debug.Log("Открываю ссылку: " + url);
        }
        else
        {
            Debug.LogWarning("URL не указан!");
        }
    }
    
    // Метод для изменения URL во время выполнения
    public void SetURL(string newUrl)
    {
        url = newUrl;
    }
    
    private void OnDestroy()
    {
        // Убираем обработчик при уничтожении
        if (button != null)
            button.onClick.RemoveListener(OpenLink);
    }
}