using UnityEngine;

public class ModeChanger : MonoBehaviour
{
    [Header("Настройки")]
    public PlayerController.PlayerMode newMode;
    public bool changeOnCollision = true;
    public bool changeOnTrigger = true;
    public bool oneTimeUse = true;
    public bool useCustomParticles = false;
    public ParticleSystem customParticles;
    
    [Header("Визуальные эффекты")]
    public float colorChangeDuration = 0.5f;
    public Color cubeColor = Color.red;
    public Color shipColor = Color.blue;
    public Color waveColor = Color.yellow;
    public Color ufoColor = Color.magenta;
    public Color spiderColor = Color.green;
    
    private SpriteRenderer spriteRenderer;
    private bool used = false;
    private PlayerController player;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
        
        // Автоматически добавляем тег для обнаружения
        gameObject.tag = "ModePortal";
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            switch (newMode)
            {
                case PlayerController.PlayerMode.Cube:
                    spriteRenderer.color = cubeColor;
                    break;
                case PlayerController.PlayerMode.Ship:
                    spriteRenderer.color = shipColor;
                    break;
                case PlayerController.PlayerMode.Wave:
                    spriteRenderer.color = waveColor;
                    break;
                case PlayerController.PlayerMode.UFO:
                    spriteRenderer.color = ufoColor;
                    break;
                case PlayerController.PlayerMode.Spider:
                    spriteRenderer.color = spiderColor;
                    break;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!changeOnCollision || used) return;
        TryChangeMode(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!changeOnTrigger || used) return;
        TryChangeMode(other.gameObject);
    }

    private void TryChangeMode(GameObject obj)
    {
        player = obj.GetComponent<PlayerController>();
        if (player != null)
        {
            ChangeMode();
        }
    }

    public void ChangeMode()
    {
        if (used && oneTimeUse) return;
        
        // Воспроизводим частицы
        if (useCustomParticles && customParticles != null)
        {
            customParticles.Play();
        }
        
        // Меняем режим игрока
        player.ChangeMode(newMode);
        
        // Меняем цвет игрока на короткое время
        StartCoroutine(FlashPlayerColor());
        
        if (oneTimeUse)
        {
            used = true;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        }
    }

    private System.Collections.IEnumerator FlashPlayerColor()
    {
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite == null) yield break;
        
        Color originalColor = playerSprite.color;
        Color targetColor = GetModeColor(newMode);
        
        // Плавное изменение цвета к целевому
        float elapsed = 0f;
        while (elapsed < colorChangeDuration / 2f)
        {
            playerSprite.color = Color.Lerp(originalColor, targetColor, elapsed / (colorChangeDuration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Плавное возвращение к исходному цвету
        elapsed = 0f;
        while (elapsed < colorChangeDuration / 2f)
        {
            playerSprite.color = Color.Lerp(targetColor, originalColor, elapsed / (colorChangeDuration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerSprite.color = originalColor;
    }

    private Color GetModeColor(PlayerController.PlayerMode mode)
    {
        switch (mode)
        {
            case PlayerController.PlayerMode.Cube: return cubeColor;
            case PlayerController.PlayerMode.Ship: return shipColor;
            case PlayerController.PlayerMode.Wave: return waveColor;
            case PlayerController.PlayerMode.UFO: return ufoColor;
            case PlayerController.PlayerMode.Spider: return spiderColor;
            default: return Color.white;
        }
    }

    public void ResetChanger()
    {
        used = false;
        if (spriteRenderer != null) 
        {
            spriteRenderer.enabled = true;
            UpdateVisuals(); // Восстанавливаем визуал
        }
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = true;
    }
}