using UnityEngine;

public class GravityOrb : MonoBehaviour
{
    [Header("Настройки гравитационного орба")]
    [SerializeField] private float activationForce = 5f; // Сила при активации
    [SerializeField] private bool destroyOnUse = false; // Уничтожать после использования
    [SerializeField] private float cooldownTime = 0.5f; // Время перезарядки
    [SerializeField] private bool isOneTimeUse = false; // Одноразовый (если не уничтожается)
    
    [Header("Визуальные эффекты")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color invertedColor = Color.magenta;
    [SerializeField] private ParticleSystem gravityParticles;
    [SerializeField] private AudioClip gravityFlipSound;
    
    [Header("Компоненты")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CircleCollider2D circleCollider;
    
    private bool isActive = true;
    private bool isOnCooldown = false;
    private float currentColorValue = 0f;
    private bool playerInTrigger = false;
    private PlayerController currentPlayer;
    
    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (circleCollider == null)
            circleCollider = GetComponent<CircleCollider2D>();
            
        UpdateVisual();
    }
    
    private void Update()
    {
        // Плавное изменение цвета
        if (isActive && !isOnCooldown)
        {
            currentColorValue = Mathf.PingPong(Time.time * 2f, 1f);
            spriteRenderer.color = Color.Lerp(normalColor, invertedColor, currentColorValue);
        }
        
        // Проверка нажатия, если игрок в триггере
        if (playerInTrigger && !isOnCooldown && isActive)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                ActivateGravityOrb();
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || isOnCooldown) return;
        
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            currentPlayer = other.GetComponent<PlayerController>();
            
            // Показать подсказку или эффект готовности
            ShowReadyEffect();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            currentPlayer = null;
            HideReadyEffect();
        }
    }
    
    private void ShowReadyEffect()
    {
        // Эффект готовности (например, изменение размера)
        transform.localScale = Vector3.one * 1.2f;
    }
    
    private void HideReadyEffect()
    {
        // Возвращаем нормальный размер
        transform.localScale = Vector3.one;
    }
    
    private void ActivateGravityOrb()
    {
        if (!isActive || isOnCooldown || currentPlayer == null) return;
        
        // Переворачиваем гравитацию через PlayerController
        FlipPlayerGravity(currentPlayer);
        
        // Применяем небольшую силу при перевороте
        ApplyFlipForce(currentPlayer);
        
        // Эффекты
        PlayEffects();
        
        // Перезарядка
        if (!destroyOnUse)
        {
            StartCoroutine(StartCooldown());
        }
        else
        {
            DestroyOrb();
        }
    }
    
    private void FlipPlayerGravity(PlayerController player)
    {
        // Для волны используем существующий метод FlipGravity
        if (player.currentMode == PlayerController.PlayerMode.Wave)
        {
            // Вызываем приватный метод FlipGravity через reflection
            // ИЛИ добавим публичный метод в PlayerController
            var flipGravityMethod = player.GetType().GetMethod("FlipGravity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (flipGravityMethod != null)
            {
                flipGravityMethod.Invoke(player, null);
            }
        }
        else
        {
            // Для других режимов просто инвертируем гравитацию
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale *= -1;
            }
            
            // Для паука и волны также переворачиваем спрайт
            if (player.currentMode == PlayerController.PlayerMode.Spider || 
                player.currentMode == PlayerController.PlayerMode.Wave)
            {
                SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
                if (playerSprite != null)
                {
                    playerSprite.flipY = !playerSprite.flipY;
                }
            }
        }
    }
    
    private void ApplyFlipForce(PlayerController player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Применяем небольшую вертикальную силу при перевороте
            Vector2 flipForce = new Vector2(0, activationForce);
            
            // Если гравитация перевернута, инвертируем силу
            if (player.IsUpsideDown)
            {
                flipForce.y *= -1;
            }
            
            rb.velocity = new Vector2(rb.velocity.x, flipForce.y);
        }
    }
    
    private void PlayEffects()
    {
        // Частицы
        if (gravityParticles != null)
        {
            ParticleSystem particles = Instantiate(gravityParticles, transform.position, Quaternion.identity);
            Destroy(particles.gameObject, 2f);
        }
        
        // Звук
        if (gravityFlipSound != null)
        {
            AudioSource.PlayClipAtPoint(gravityFlipSound, transform.position);
        }
        
        // Визуальный эффект
        StartCoroutine(PulseEffect());
    }
    
    private System.Collections.IEnumerator PulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.5f;
        
        // Быстрое мерцание
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        UpdateVisual();
        
        yield return new WaitForSeconds(0.1f);
        transform.localScale = originalScale;
    }
    
    private System.Collections.IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        isActive = false;
        playerInTrigger = false;
        
        // Меняем цвет на серый во время перезарядки
        spriteRenderer.color = Color.gray;
        
        yield return new WaitForSeconds(cooldownTime);
        
        isOnCooldown = false;
        isActive = true;
        UpdateVisual();
        
        // Если одноразовый, деактивируем навсегда
        if (isOneTimeUse)
        {
            isActive = false;
            spriteRenderer.color = Color.gray;
            circleCollider.enabled = false;
        }
    }
    
    private void DestroyOrb()
    {
        // Анимация исчезновения
        StartCoroutine(FadeAndDestroy());
    }
    
    private System.Collections.IEnumerator FadeAndDestroy()
    {
        float fadeTime = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = color;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    private void UpdateVisual()
    {
        if (spriteRenderer != null && isActive && !isOnCooldown)
        {
            spriteRenderer.color = normalColor;
        }
    }
    
    // Активация орба извне
    public void Activate()
    {
        if (isActive && !isOnCooldown)
        {
            ActivateGravityOrb();
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, GetComponent<CircleCollider2D>().radius);
        
        // Стрелка, показывающая направление силы
        Gizmos.color = Color.magenta;
        Vector3 direction = Vector3.up * activationForce * 0.1f;
        Gizmos.DrawLine(transform.position, transform.position + direction);
        DrawArrow(transform.position, direction);
    }
    
    private void DrawArrow(Vector3 position, Vector3 direction)
    {
        float arrowHeadLength = 0.25f;
        float arrowHeadAngle = 20.0f;
        
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        
        Gizmos.DrawRay(position + direction, right * arrowHeadLength);
        Gizmos.DrawRay(position + direction, left * arrowHeadLength);
    }
}