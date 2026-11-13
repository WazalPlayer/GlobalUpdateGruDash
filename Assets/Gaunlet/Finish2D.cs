using UnityEngine;
using UnityEngine.SceneManagement;

public class Finish2D : MonoBehaviour
{
    [Header("Настройки 2D финиша")]
    public string playerTag = "Player";
    public float delay = 1.5f;
    
    [Header("Какой уровень открываем")]
    public string levelToOpen = "";
    
    [Header("Визуальные эффекты (опционально)")]
    public ParticleSystem finishEffect;
    public AudioClip finishSound;
    
    private bool isActivated = false;
    
    void Start()
    {
        // Делаем видимым в редакторе
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0, 1, 0, 0.5f); // Полупрозрачный зеленый
        }
    }
    
    // Для 2D коллайдеров
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Финиш: OnTriggerEnter2D сработал! Объект: " + other.gameObject.name + ", тег: " + other.tag);
        
        if (!isActivated && other.CompareTag(playerTag))
        {
            Debug.Log("Финиш: Игрок найден! Запускаем...");
            FinishReached();
        }
    }
    
    // Для 3D коллайдеров (на всякий случай)
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Финиш: OnTriggerEnter сработал! Объект: " + other.gameObject.name + ", тег: " + other.tag);
        
        if (!isActivated && other.CompareTag(playerTag))
        {
            Debug.Log("Финиш: Игрок найден! Запускаем...");
            FinishReached();
        }
    }
    
    void FinishReached()
    {
        isActivated = true;
        Debug.Log("=== ФИНИШ ДОСТИГНУТ ===");
        
        // Воспроизводим эффекты
        PlayEffects();
        
        // Сохраняем прогресс
        if (!string.IsNullOrEmpty(levelToOpen))
        {
            if (GameData.Instance != null)
            {
                GameData.Instance.SaveLevelOpen(levelToOpen);
                Debug.Log("Прогресс сохранен: " + levelToOpen);
            }
            else
            {
                // Напрямую в PlayerPrefs если GameData не найден
                PlayerPrefs.SetInt("Open_" + levelToOpen, 1);
                PlayerPrefs.Save();
                Debug.Log("Прогресс сохранен напрямую: " + levelToOpen);
            }
        }
        else
        {
            Debug.LogError("Не указано имя уровня для открытия!");
        }
        
        // Ждем и загружаем меню
        Invoke("GoToMainMenu", delay);
    }
    
    void PlayEffects()
    {
        // Партиклы
        if (finishEffect != null)
        {
            finishEffect.Play();
        }
        
        // Звук
        if (finishSound != null)
        {
            AudioSource.PlayClipAtPoint(finishSound, transform.position);
        }
        
        // Визуальный эффект (меняем цвет)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.yellow; // Меняем цвет при активации
        }
    }
    
    void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
    
    void OnDrawGizmos()
    {
        // Рисуем зеленую рамку в редакторе
        Gizmos.color = Color.green;
        
        // Для 2D объекта
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            Gizmos.DrawWireCube(col2D.bounds.center, col2D.bounds.size);
        }
        
        // Для 3D объекта
        Collider col3D = GetComponent<Collider>();
        if (col3D != null)
        {
            Gizmos.DrawWireCube(col3D.bounds.center, col3D.bounds.size);
        }
        
        // Подпись в редакторе
        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(levelToOpen))
        {
            UnityEditor.Handles.Label(transform.position, 
                "ФИНИШ 2D\n" +
                "Открывает: " + levelToOpen + "\n" +
                "Тег игрока: " + playerTag);
        }
        #endif
    }
}