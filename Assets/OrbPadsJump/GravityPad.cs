using UnityEngine;

public class GravityPad : MonoBehaviour
{
    [Header("Настройки гравитационного пада")]
    [SerializeField] private float bounceForce = 15f; // Сила отскока при активации
    [SerializeField] private bool isBouncy = true; // Отскакивает ли игрок
    [SerializeField] private float cooldownTime = 0.3f; // Время перезарядки
    [SerializeField] private bool isOneWay = false; // Работает только в одну сторону
    [SerializeField] private bool startInverted = false; // Начинает с перевернутой гравитацией
    
    [Header("Визуальные эффекты")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.blue;
    [SerializeField] private Color activeColor = Color.magenta;
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private AudioClip activationSound;
    
    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string activationTrigger = "Activate";
    
    private bool isActive = true;
    private bool isOnCooldown = false;
    private bool isGravityInverted = false;
    
    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (animator == null)
            animator = GetComponent<Animator>();
            
        isGravityInverted = startInverted;
        UpdateVisual();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || isOnCooldown) return;
        
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ActivateGravityPad(player);
            }
        }
    }
    
    private void ActivateGravityPad(PlayerController player)
    {
        // Переворачиваем гравитацию
        FlipPlayerGravity(player);
        
        // Применяем силу отскока если нужно
        if (isBouncy)
        {
            ApplyBounceForce(player);
        }
        
        // Эффекты
        PlayEffects();
        
        // Анимация
        if (animator != null)
        {
            animator.SetTrigger(activationTrigger);
        }
        
        // Перезарядка
        StartCoroutine(StartCooldown());
        
        // Инвертируем состояние для следующего использования
        if (!isOneWay)
        {
            isGravityInverted = !isGravityInverted;
            UpdateVisual();
        }
    }
    
    private void FlipPlayerGravity(PlayerController player)
    {
        // Для волны используем существующий метод FlipGravity
        if (player.currentMode == PlayerController.PlayerMode.Wave)
        {
            var flipGravityMethod = player.GetType().GetMethod("FlipGravity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (flipGravityMethod != null)
            {
                flipGravityMethod.Invoke(player, null);
            }
        }
        else
        {
            // Для других режимов инвертируем гравитацию
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale *= -1;
            }
            
            // Переворачиваем спрайт для режимов, которые это поддерживают
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
    
    private void ApplyBounceForce(PlayerController player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Определяем направление отскока
            float direction = isGravityInverted ? -1f : 1f;
            Vector2 bounce = new Vector2(0, bounceForce * direction);
            
            // Применяем силу
            rb.velocity = new Vector2(rb.velocity.x, bounce.y);
        }
    }
    
    private void PlayEffects()
    {
        // Частицы
        if (activationParticles != null)
        {
            activationParticles.Play();
        }
        
        // Звук
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
        }
        
        // Визуальный эффект
        StartCoroutine(FlashEffect());
    }
    
    private System.Collections.IEnumerator FlashEffect()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = activeColor;
        
        yield return new WaitForSeconds(0.1f);
        
        spriteRenderer.color = originalColor;
    }
    
    private System.Collections.IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        isActive = false;
        
        // Меняем цвет во время перезарядки
        spriteRenderer.color = Color.gray;
        
        yield return new WaitForSeconds(cooldownTime);
        
        isOnCooldown = false;
        isActive = true;
        UpdateVisual();
    }
    
    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            if (isOneWay)
            {
                spriteRenderer.color = normalColor;
            }
            else
            {
                // Разные цвета для разных состояний гравитации
                spriteRenderer.color = isGravityInverted ? activeColor : normalColor;
            }
        }
        
        // Поворачиваем стрелку направления
        UpdateArrowDirection();
    }
    
    private void UpdateArrowDirection()
    {
        // Если есть дочерний объект со стрелкой, поворачиваем его
        Transform arrow = transform.Find("Arrow");
        if (arrow != null)
        {
            float angle = isGravityInverted ? 180f : 0f;
            arrow.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    // Сброс состояния пада
    public void ResetPad()
    {
        isActive = true;
        isOnCooldown = false;
        isGravityInverted = startInverted;
        UpdateVisual();
    }
    
    // Принудительная активация пада
    public void ActivatePad()
    {
        if (isActive && !isOnCooldown)
        {
            // Ищем игрока в зоне действия
            Collider2D[] colliders = Physics2D.OverlapBoxAll(
                transform.position, 
                GetComponent<BoxCollider2D>().size, 
                0f);
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Player"))
                {
                    PlayerController player = collider.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        ActivateGravityPad(player);
                        break;
                    }
                }
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = isGravityInverted ? activeColor : normalColor;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>().size);
        
        // Стрелка направления
        Gizmos.color = Color.white;
        Vector3 direction = isGravityInverted ? Vector3.down : Vector3.up;
        Gizmos.DrawLine(transform.position, transform.position + direction * 0.5f);
        
        // Стрелка на конце линии
        DrawArrow(transform.position + direction * 0.5f, direction * 0.2f);
    }
    
    private void DrawArrow(Vector3 position, Vector3 direction)
    {
        float arrowHeadLength = 0.1f;
        float arrowHeadAngle = 20.0f;
        
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        
        Gizmos.DrawRay(position, right * arrowHeadLength);
        Gizmos.DrawRay(position, left * arrowHeadLength);
    }
}