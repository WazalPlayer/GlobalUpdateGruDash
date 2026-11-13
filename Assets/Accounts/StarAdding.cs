using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StarAdding : MonoBehaviour
{
    [Header("Настройки")]
    public int starsPerTouch = 10; // Звёзд за одно касание
    public bool autoSave = true; // Автоматически сохранять в базу
    public bool destroyOnTouch = false; // Уничтожать объект при касании
    public bool canTouchMultipleTimes = false; // Можно касаться несколько раз
    public float touchCooldown = 0.5f; // Задержка между касаниями
    
    [Header("UI элементы")]
    public Text starsText;
    public GameObject starEffectPrefab;
    public AudioClip touchSound;
    
    [Header("Визуальные эффекты")]
    public ParticleSystem touchParticles;
    public Animator touchAnimator;
    public string touchAnimationName = "Touch";
    public float flashDuration = 0.2f;
    public Color flashColor = Color.yellow;
    
    private int currentStars = 0;
    private bool canTouch = true;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private const string STARS_KEY = "PlayerStars";
    
    void Start()
    {
        // Получаем компоненты
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // Сохраняем оригинальный цвет
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Загружаем звёзды из PlayerPrefs
        // Если ключа нет - используем 0, иначе загружаем сохранённое значение
        currentStars = PlayerPrefs.GetInt(STARS_KEY, 0);
        UpdateStarsUI();
        
        // Инициализируем звук
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        Debug.Log($"⭐ StarAdding инициализирован. Текущие звёзды: {currentStars}");
    }
    
    // 2D столкновение с триггером
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HandlePlayerTouch(other.transform.position);
        }
    }
    
    // 2D столкновение с коллайдером
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerTouch(collision.contacts[0].point);
        }
    }
    
    // Обработка касания игрока
    void HandlePlayerTouch(Vector3 contactPoint)
    {
        if (!canTouch) return;
        
        // Добавляем звёзды
        AddStars(starsPerTouch, contactPoint);
        
        // Визуальные эффекты
        StartCoroutine(FlashEffect());
        
        // Звуковой эффект
        if (touchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(touchSound);
        }
        
        // Частицы
        if (touchParticles != null)
        {
            touchParticles.Play();
        }
        
        // Анимация
        if (touchAnimator != null && !string.IsNullOrEmpty(touchAnimationName))
        {
            touchAnimator.Play(touchAnimationName);
        }
        
        // Обработка уничтожения
        if (destroyOnTouch)
        {
            StartCoroutine(DestroyAfterDelay());
        }
        
        // Задержка перед следующим касанием
        if (!canTouchMultipleTimes && !destroyOnTouch)
        {
            canTouch = false;
            StartCoroutine(EnableTouchAfterDelay(touchCooldown));
        }
        else if (canTouchMultipleTimes)
        {
            canTouch = false;
            StartCoroutine(EnableTouchAfterDelay(touchCooldown));
        }
    }
    
    // Добавить звёзды
    public void AddStars(int amount, Vector3? position = null)
    {
        currentStars += amount;
        
        // Сохраняем в PlayerPrefs
        PlayerPrefs.SetInt(STARS_KEY, currentStars);
        PlayerPrefs.Save();
        
        // Обновляем UI
        UpdateStarsUI();
        
        // Показываем эффект
        if (starEffectPrefab != null)
        {
            Vector3 spawnPosition = position ?? transform.position;
            GameObject effect = Instantiate(starEffectPrefab, spawnPosition, Quaternion.identity);
            
            // Уничтожаем эффект через 2 секунды
            Destroy(effect, 2f);
        }
        
        // Автоматически сохраняем в базу
        if (autoSave && AccountsManager.Instance != null)
        {
            // Устанавливаем новые звёзды в AccountsManager
            AccountsManager.Instance.SetPlayerStars(currentStars);
            
            // Сохраняем аккаунт в базу
            AccountsManager.Instance.SaveAccountData();
        }
        
        Debug.Log($"⭐ Добавлено {amount} звёзд. Всего: {currentStars}");
    }
    
    // Установить количество звёзд
    public void SetStars(int amount)
    {
        currentStars = amount;
        
        // Сохраняем в PlayerPrefs
        PlayerPrefs.SetInt(STARS_KEY, currentStars);
        PlayerPrefs.Save();
        
        // Обновляем UI
        UpdateStarsUI();
        
        // Автоматически сохраняем в базу
        if (autoSave && AccountsManager.Instance != null)
        {
            AccountsManager.Instance.SetPlayerStars(amount);
            AccountsManager.Instance.SaveAccountData();
        }
    }
    
    // Получить текущее количество звёзд
    public int GetStars()
    {
        return currentStars;
    }
    
    // Списать звёзды
    public bool SpendStars(int amount)
    {
        if (currentStars >= amount)
        {
            currentStars -= amount;
            
            // Сохраняем в PlayerPrefs
            PlayerPrefs.SetInt(STARS_KEY, currentStars);
            PlayerPrefs.Save();
            
            // Обновляем UI
            UpdateStarsUI();
            
            // Автоматически сохраняем в базу
            if (autoSave && AccountsManager.Instance != null)
            {
                AccountsManager.Instance.SetPlayerStars(currentStars);
                AccountsManager.Instance.SaveAccountData();
            }
            
            Debug.Log($"⭐ Списано {amount} звёзд. Осталось: {currentStars}");
            return true;
        }
        
        Debug.Log($"❌ Недостаточно звёзд. Нужно: {amount}, есть: {currentStars}");
        return false;
    }
    
    // Обновление UI
    private void UpdateStarsUI()
    {
        if (starsText != null)
        {
            starsText.text = $"⭐ {currentStars}";
        }
    }
    
    // Эффект вспышки
    private IEnumerator FlashEffect()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    // Включить возможность касания через задержку
    private IEnumerator EnableTouchAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canTouch = true;
    }
    
    // Уничтожить объект с задержкой
    private IEnumerator DestroyAfterDelay()
    {
        // Отключаем коллайдеры
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Если есть аниматор, можно запустить анимацию уничтожения
        if (touchAnimator != null)
        {
            touchAnimator.SetTrigger("Destroy");
        }
        
        // Ждем, пока воспроизведется звук
        if (touchSound != null)
        {
            yield return new WaitForSeconds(touchSound.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Уничтожаем объект
        Destroy(gameObject);
    }
    
    // Сбросить звёзды (для тестирования)
    public void ResetStars()
    {
        currentStars = 0;
        PlayerPrefs.SetInt(STARS_KEY, currentStars);
        PlayerPrefs.Save();
        UpdateStarsUI();
        
        if (autoSave && AccountsManager.Instance != null)
        {
            AccountsManager.Instance.SetPlayerStars(currentStars);
            AccountsManager.Instance.SaveAccountData();
        }
    }
    
    // Сохранить в базу
    public void SaveToDatabase()
    {
        if (AccountsManager.Instance != null)
        {
            AccountsManager.Instance.SetPlayerStars(currentStars);
            AccountsManager.Instance.SaveAccountData();
        }
    }
    
    // Включить/выключить возможность касания
    public void SetTouchEnabled(bool enabled)
    {
        canTouch = enabled;
    }
    
    // Проверка, можно ли касаться
    public bool IsTouchEnabled()
    {
        return canTouch;
    }
    
    // Сохраняем звёзды при уничтожении объекта
    void OnDestroy()
    {
        // Сохраняем звёзды при уничтожении объекта
        if (autoSave && AccountsManager.Instance != null)
        {
            AccountsManager.Instance.SetPlayerStars(currentStars);
            AccountsManager.Instance.SaveAccountData();
        }
    }
    
    // Отображение зоны действия (для редактора)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        // Отображаем 2D коллайдеры
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = (BoxCollider2D)collider;
                Vector3 center = transform.position + new Vector3(boxCollider.offset.x, boxCollider.offset.y, 0);
                Vector3 size = new Vector3(boxCollider.size.x, boxCollider.size.y, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
            else if (collider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = (CircleCollider2D)collider;
                Vector3 center = transform.position + new Vector3(circleCollider.offset.x, circleCollider.offset.y, 0);
                Gizmos.DrawWireSphere(center, circleCollider.radius);
            }
        }
    }
}