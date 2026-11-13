using UnityEngine;

public class Orb : MonoBehaviour
{
    [Header("Настройки орба")]
    [SerializeField] private float jumpForce = 25f;
    [SerializeField] private float maxHoldTime = 0.5f;
    [SerializeField] private bool isTimed = false;
    [SerializeField] private bool destroyOnTouch = true;
    [SerializeField] private Color normalColor = Color.yellow;
    [SerializeField] private Color chargedColor = Color.red;
    
    [Header("Компоненты")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem touchParticles;
    [SerializeField] private AudioClip touchSound;
    
    private bool isPlayerInOrb = false;
    private bool isActivated = false;
    private float holdTimer = 0f;
    private PlayerController playerController;
    private bool isHolding = false;
    
    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        spriteRenderer.color = normalColor;
    }
    
    private void Update()
    {
        // Если игрок в орбе и не активирован
        if (isPlayerInOrb && !isActivated)
        {
            if (isTimed)
            {
                // Для timed орба - проверяем начало нажатия
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    isHolding = true;
                    holdTimer = 0f;
                }
                
                // Если удерживаем - считаем время
                if (isHolding && (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)))
                {
                    holdTimer += Time.deltaTime;
                    float holdPercent = Mathf.Clamp01(holdTimer / maxHoldTime);
                    spriteRenderer.color = Color.Lerp(normalColor, chargedColor, holdPercent);
                    
                    // Если отпустили - активируем
                    if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space))
                    {
                        float finalForce = jumpForce * (0.5f + holdPercent * 0.5f);
                        ActivateOrb(finalForce);
                        isHolding = false;
                    }
                }
            }
            else
            {
                // Для обычного орба - активируем при нажатии
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    ActivateOrb(jumpForce);
                }
            }
        }
        
        // Если игрок вышел из орба во время удержания (только для timed)
        if (!isPlayerInOrb && isHolding && !isActivated)
        {
            isHolding = false;
            holdTimer = 0f;
            spriteRenderer.color = normalColor;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                isPlayerInOrb = true;
                
                // Визуальный эффект - подсветка
                if (!isTimed)
                {
                    spriteRenderer.color = chargedColor;
                }
                
                // Если орб мгновенный и игрок уже держит кнопку - активируем сразу
                if (!isTimed && (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)))
                {
                    ActivateOrb(jumpForce);
                }
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInOrb = false;
            
            // Сбрасываем таймер если не активирован
            if (!isActivated)
            {
                isHolding = false;
                holdTimer = 0f;
                spriteRenderer.color = normalColor;
            }
        }
    }
    
    private void ActivateOrb(float force)
    {
        if (isActivated || playerController == null) return;
        
        isActivated = true;
        isHolding = false;
        
        // Применяем силу отталкивания
        Vector2 jumpDirection = Vector2.up;
        
        // Учитываем перевернутую гравитацию
        if (playerController.currentMode == PlayerController.PlayerMode.Wave || 
            playerController.currentMode == PlayerController.PlayerMode.Spider)
        {
            if (playerController.IsUpsideDown)
            {
                jumpDirection = Vector2.down;
            }
        }
        
        // Применяем силу к игроку
        Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.velocity = new Vector2(playerRb.velocity.x, jumpDirection.y * force);
        }
        
        // Эффекты
        PlayEffects();
        
        // Уничтожаем орб если нужно
        if (destroyOnTouch)
        {
            StartCoroutine(DestroyOrb());
        }
        else
        {
            // Возвращаем нормальное состояние через время
            StartCoroutine(ResetAfterActivation());
        }
    }
    
    private void PlayEffects()
    {
        // Частицы
        if (touchParticles != null)
        {
            ParticleSystem.MainModule main = touchParticles.main;
            main.startColor = spriteRenderer.color;
            touchParticles.Play();
        }
        
        // Звук
        if (touchSound != null)
        {
            AudioSource.PlayClipAtPoint(touchSound, transform.position);
        }
        
        // Визуальный эффект
        StartCoroutine(PulseEffect());
    }
    
    private System.Collections.IEnumerator PulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer.color;
        
        // Быстрое увеличение и изменение цвета
        transform.localScale = originalScale * 1.5f;
        spriteRenderer.color = Color.white;
        
        yield return new WaitForSeconds(0.1f);
        
        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;
    }
    
    private System.Collections.IEnumerator ResetAfterActivation()
    {
        // Ждем немного перед сбросом
        yield return new WaitForSeconds(0.3f);
        
        // Сбрасываем состояние
        isActivated = false;
        isPlayerInOrb = false;
        isHolding = false;
        holdTimer = 0f;
        spriteRenderer.color = normalColor;
    }
    
    private System.Collections.IEnumerator DestroyOrb()
    {
        // Визуальный эффект исчезновения
        float fadeTime = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 0.5f;
        
        while (elapsed < fadeTime)
        {
            if (spriteRenderer != null)
            {
                // Плавное исчезновение
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = color;
                
                // Уменьшение размера
                transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / fadeTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    // Активация орба извне (например, триггером)
    public void Activate()
    {
        if (!isActivated && playerController != null)
        {
            ActivateOrb(jumpForce);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetComponent<CircleCollider2D>().radius);
        
        // Показываем направление силы
        Gizmos.color = Color.red;
        Vector3 direction = Vector3.up * jumpForce * 0.05f;
        Gizmos.DrawLine(transform.position, transform.position + direction);
        Gizmos.DrawWireSphere(transform.position + direction, 0.1f);
    }
}