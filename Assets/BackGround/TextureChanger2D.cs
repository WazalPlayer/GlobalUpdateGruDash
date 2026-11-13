using UnityEngine;
using UnityEngine.UI;

public class TextureChanger2D : MonoBehaviour
{
    [Header("Настройки объекта")]
    [SerializeField] private SpriteRenderer targetRenderer; // SpriteRenderer для 2D объекта
    
    [Header("Спрайты")]
    [SerializeField] private Sprite defaultSprite; // Дефолтный спрайт
    [SerializeField] private Sprite[] availableSprites; // Доступные спрайты для смены
    
    [Header("UI Кнопки (опционально)")]
    [SerializeField] private Button[] spriteButtons; // Кнопки для выбора спрайтов
    [SerializeField] private Button resetButton; // Кнопка сброса
    
    void Start()
    {
        // Проверяем наличие SpriteRenderer
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                Debug.LogError("TextureChanger2D: Нет SpriteRenderer на объекте!");
                return;
            }
        }
        
        // Устанавливаем дефолтный спрайт
        SetDefaultSprite();
        
        // Настраиваем кнопки, если они есть
        SetupButtons();
    }
    
    void SetupButtons()
    {
        // Настройка кнопок выбора спрайтов
        if (spriteButtons != null && spriteButtons.Length > 0)
        {
            for (int i = 0; i < spriteButtons.Length; i++)
            {
                int spriteIndex = i; // Локальная копия для замыкания
                
                if (spriteButtons[i] != null)
                {
                    // Проверяем, есть ли спрайт для этой кнопки
                    if (spriteIndex < availableSprites.Length)
                    {
                        spriteButtons[i].onClick.AddListener(() => ChangeSprite(spriteIndex));
                    }
                    else
                    {
                        spriteButtons[i].interactable = false;
                        Debug.LogWarning($"TextureChanger2D: Нет спрайта для кнопки {i}");
                    }
                }
            }
        }
        
        // Настройка кнопки сброса
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(SetDefaultSprite);
        }
    }
    
    // Метод для смены спрайта по индексу
    public void ChangeSprite(int spriteIndex)
    {
        if (targetRenderer == null)
        {
            Debug.LogError("SpriteRenderer не найден!");
            return;
        }
        
        if (spriteIndex >= 0 && spriteIndex < availableSprites.Length && availableSprites[spriteIndex] != null)
        {
            targetRenderer.sprite = availableSprites[spriteIndex];
            Debug.Log($"Спрайт изменен на: {availableSprites[spriteIndex].name}");
        }
        else
        {
            Debug.LogError($"Неверный индекс спрайта: {spriteIndex} или спрайт отсутствует");
        }
    }
    
    // Метод для смены спрайта по ссылке
    public void ChangeSprite(Sprite newSprite)
    {
        if (targetRenderer == null)
        {
            Debug.LogError("SpriteRenderer не найден!");
            return;
        }
        
        if (newSprite != null)
        {
            targetRenderer.sprite = newSprite;
            Debug.Log($"Спрайт изменен на: {newSprite.name}");
        }
    }
    
    // Метод для установки дефолтного спрайта
    public void SetDefaultSprite()
    {
        if (targetRenderer != null && defaultSprite != null)
        {
            targetRenderer.sprite = defaultSprite;
            Debug.Log("Установлен дефолтный спрайт");
        }
    }
    
    // Автоматическое создание UI кнопок (опционально)
    [ContextMenu("Создать UI кнопки")]
    void CreateUIButtons()
    {
        Debug.Log("Для создания кнопок используйте UI Button через GameObject > UI > Button");
        Debug.Log("Затем перетащите кнопки в массив spriteButtons в инспекторе");
    }
}