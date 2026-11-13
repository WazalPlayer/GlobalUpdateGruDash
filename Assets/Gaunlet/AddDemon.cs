using UnityEngine;

public class AddDemon : MonoBehaviour
{
    [Header("Настройки демона")]
    public string demonName = "UnnamedDemon"; // Уникальное имя демона
    
    [Header("Визуальные эффекты")]
    public ParticleSystem collectEffect;
    public AudioClip collectSound;
    public GameObject visualObject; // Визуальная часть демона
    
    [Header("Настройки столкновения")]
    public bool destroyOnCollect = true;
    public bool useTrigger = true; // Использовать триггер или коллайдер
    
    private bool isCollected = false;
    
    void Start()
    {
        // Проверяем, был ли демон уже пройден
        CheckIfAlreadyCollected();
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger || isCollected) return;
        
        // Проверяем, что столкнулся игрок
        if (other.CompareTag("Player"))
        {
            CollectDemon();
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (useTrigger || isCollected) return;
        
        // Проверяем, что столкнулся игрок
        if (collision.gameObject.CompareTag("Player"))
        {
            CollectDemon();
        }
    }
    
    private void CheckIfAlreadyCollected()
    {
        // Проверяем по имени демона
        string demonKey = $"Demon_{demonName}";
        
        if (PlayerPrefs.HasKey(demonKey))
        {
            Debug.Log($"ℹ️ Демон '{demonName}' уже был пройден ранее");
            isCollected = true;
            
            // Делаем объект неактивным, если нужно
            if (destroyOnCollect && visualObject != null)
            {
                visualObject.SetActive(false);
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }
    
    private void CollectDemon()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        Debug.Log($"👹 Начало сбора демона: {demonName}");
        
        // Проверяем, не был ли демон уже собран (дополнительная проверка)
        string demonKey = $"Demon_{demonName}";
        if (PlayerPrefs.HasKey(demonKey))
        {
            Debug.LogWarning($"⚠️ Демон '{demonName}' уже был собран! Пропускаем...");
            PlayCollectEffects();
            DeactivateObject();
            return;
        }
        
        // Добавляем демона через AccountsManager
        if (AccountsManager.Instance != null)
        {
            Debug.Log($"👹 Добавляем демона через AccountsManager: {demonName}");
            AccountsManager.Instance.AddPlayerDemon(demonName);
        }
        else
        {
            Debug.LogError("❌ AccountsManager не найден!");
            
            // Резервный вариант: добавляем напрямую в PlayerPrefs
            PlayerPrefs.SetInt(demonKey, 1);
            int currentDemons = PlayerPrefs.GetInt("PlayerDemons", 0);
            PlayerPrefs.SetInt("PlayerDemons", currentDemons + 1);
            PlayerPrefs.Save();
            
            Debug.Log($"👹 Демон добавлен напрямую: {demonName}. Всего демонов: {currentDemons + 1}");
        }
        
        // Визуальные эффекты
        PlayCollectEffects();
        
        // Деактивируем объект
        DeactivateObject();
        
        Debug.Log($"✅ Демон '{demonName}' успешно собран!");
    }
    
    private void PlayCollectEffects()
    {
        // Проигрываем эффект частиц
        if (collectEffect != null)
        {
            ParticleSystem effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + 1f);
        }
        
        // Проигрываем звук
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
    }
    
    private void DeactivateObject()
    {
        // Деактивируем визуальную часть
        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }
        else
        {
            GetComponent<Renderer>().enabled = false;
        }
        
        // Отключаем коллайдеры
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        
        Collider collider3D = GetComponent<Collider>();
        if (collider3D != null) collider3D.enabled = false;
        
        // Уничтожаем объект, если нужно
        if (destroyOnCollect)
        {
            Destroy(gameObject, 2f); // Задержка для эффектов
        }
    }
    
    // Метод для ручного добавления демона (например, через триггер)
    public void AddDemonManually()
    {
        if (!isCollected)
        {
            CollectDemon();
        }
    }
    
    // Метод для сброса демона (для тестирования)
    public void ResetDemon()
    {
        string demonKey = $"Demon_{demonName}";
        PlayerPrefs.DeleteKey(demonKey);
        PlayerPrefs.Save();
        isCollected = false;
        
        Debug.Log($"♻️ Демон '{demonName}' сброшен");
        
        // Реактивируем объект
        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }
        else
        {
            GetComponent<Renderer>().enabled = true;
        }
        
        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null) collider2D.enabled = true;
        
        Collider collider3D = GetComponent<Collider>();
        if (collider3D != null) collider3D.enabled = true;
    }
    
    // Отображаем имя демона в инспекторе
    void OnValidate()
    {
        if (string.IsNullOrEmpty(demonName))
        {
            demonName = gameObject.name.Replace("Demon_", "").Replace("demon_", "");
        }
    }
}