using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Настройки пада")]
    [SerializeField] private float jumpForce = 30f;
    [SerializeField] private bool isBouncy = true; // Пружинящий пад
    [SerializeField] private float cooldownTime = 0.2f; // Время перезарядки
    
    [Header("Компоненты")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem bounceParticles;
    [SerializeField] private AudioClip bounceSound;
    
    [Header("Визуальные эффекты")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.gray;
    
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    
    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (animator == null)
            animator = GetComponent<Animator>();
            
        spriteRenderer.color = activeColor;
    }
    
    private void Update()
    {
        // Обработка перезарядки
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                spriteRenderer.color = activeColor;
            }
            else
            {
                // Плавное изменение цвета во время перезарядки
                float t = cooldownTimer / cooldownTime;
                spriteRenderer.color = Color.Lerp(activeColor, inactiveColor, t);
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isOnCooldown)
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                ApplyJump(playerController);
                
                if (isBouncy)
                {
                    isOnCooldown = true;
                    cooldownTimer = cooldownTime;
                }
            }
        }
    }
    
    private void ApplyJump(PlayerController player)
    {
        // Определяем направление прыжка
        Vector2 jumpDirection = Vector2.up;
        
        // Учитываем перевернутую гравитацию
        bool isUpsideDown = false;
        
        // Получаем состояние перевернутости из PlayerController
        // Предполагается, что у PlayerController есть публичное поле isUpsideDown
        if (player.currentMode == PlayerController.PlayerMode.Wave || 
            player.currentMode == PlayerController.PlayerMode.Spider)
        {
            // Для волны и паука учитываем isUpsideDown
            // Здесь нужно получить доступ к isUpsideDown из PlayerController
            // Сделаем это через reflection или если поле публичное
            
            // Если поле isUpsideDown приватное, нужно сделать публичный метод или свойство
            // Для этого примера предположим, что есть публичное свойство
            // В реальном коде нужно добавить в PlayerController:
            // public bool IsUpsideDown => isUpsideDown;
            
            // Временное решение - попробовать получить через reflection
            var upsideDownField = player.GetType().GetField("isUpsideDown", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (upsideDownField != null)
            {
                isUpsideDown = (bool)upsideDownField.GetValue(player);
            }
        }
        
        if (isUpsideDown)
        {
            jumpDirection = Vector2.down;
        }
        
        // Применяем силу прыжка
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            if (isBouncy)
            {
                // Пружинящий прыжок - устанавливаем точную скорость
                playerRb.velocity = new Vector2(playerRb.velocity.x, jumpDirection.y * jumpForce);
            }
            else
            {
                // Обычный прыжок - добавляем силу
                playerRb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
            }
        }
        
        // Визуальные эффекты
        PlayEffects();
        
        // Анимация
        if (animator != null)
        {
            animator.SetTrigger("Bounce");
        }
    }
    
    private void PlayEffects()
    {
        // Частицы
        if (bounceParticles != null)
        {
            bounceParticles.Play();
        }
        
        // Звук
        if (bounceSound != null)
        {
            AudioSource.PlayClipAtPoint(bounceSound, transform.position);
        }
    }
    
    // Активация пада извне (например, по таймеру или триггеру)
    public void ActivatePad()
    {
        if (!isOnCooldown)
        {
            // Активируем пад
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 
                GetComponent<BoxCollider2D>().bounds.extents.y + 0.1f);
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Player"))
                {
                    PlayerController player = collider.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        ApplyJump(player);
                        break;
                    }
                }
            }
        }
    }
    
    // Сброс перезарядки
    public void ResetCooldown()
    {
        isOnCooldown = false;
        cooldownTimer = 0f;
        spriteRenderer.color = activeColor;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>().size);
        
        // Показываем силу прыжка
        Gizmos.color = Color.blue;
        Vector3 direction = transform.up * jumpForce * 0.05f;
        Gizmos.DrawLine(transform.position, transform.position + direction);
        Gizmos.DrawWireSphere(transform.position + direction, 0.1f);
    }
}