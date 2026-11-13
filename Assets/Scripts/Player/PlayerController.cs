using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public enum PlayerMode { Cube, Ship, Wave, UFO, Spider, Ball }
    
    [Header("Настройки")]
    public float moveSpeed = 8f;
    // Куб: высота ≈2.1 юнита, полное время в воздухе ≈1.1 с (g = 2H/t², v = g·t, t = 0.55 с)
    public float jumpForce = 7.6f;
    [SerializeField] private float cubeGravity = 13.9f;      // м/с², высота прыжка = v²/(2g) ≈ 2.1 юнита
    public float shipFlyForce = 15f;
    public float shipGravityScale = 2f;
    public float waveGravityScale = 1.5f;
    public float ufoJumpForce = 10f;
    public float ufoGravityScale = 0.5f;
    public float spiderJumpForce = 14f;
    public float spiderGravityScale = 3f;
    public float ballGravityScale = 4f;
    public float ballMaxSpeed = 20f;
    public PlayerMode currentMode = PlayerMode.Cube;
    public Button practiceToggleButton; // кнопка для включения/выключения практики
    
    [Header("Ball Flip Force")]
public float ballFlipForce = 4f;   // начальная скорость при флипе, если текущая скорость мала

    [Header("Компоненты")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite cubeSprite;
    [SerializeField] private Sprite shipSprite;
    [SerializeField] private Sprite waveSprite;
    [SerializeField] private Sprite ufoSprite;
    [SerializeField] private Sprite spiderSprite;
    [SerializeField] private Sprite ballSprite;
    [SerializeField] private ParticleSystem crashParticles;
    
    [Header("Отдельный спрайт для режима куба")]
    [SerializeField] private SpriteRenderer cubeModeSpriteRenderer;
    [SerializeField] private bool useSeparateCubeSprite = true;
    [SerializeField] private Sprite cubeModeSprite;
    [SerializeField] private float cubeSpriteRotationSpeed = 360f; // Степень в секунду
    [SerializeField] private float cubeSpriteRotationSmoothness = 10f;
    // Вращение куба: ровно 90° за прыжок. Крутится с постоянной скоростью первые cubeSpinFraction
    // времени полёта (чуть быстрее), затем держит угол до приземления.
    private float CubeSpinSpeed
    {
        get
        {
            float airtime = (cubeGravity > 0f) ? 2f * jumpForce / cubeGravity : 0f;
            float spinTime = airtime * cubeSpinFraction;
            return spinTime > 0f ? 90f / spinTime : 0f;
        }
    }
    private bool cubeWasGrounded = true;                    // для мгновенного округления при касании
    private float cubeSpunThisJump = 0f;                    // сколько уже довернули за текущий прыжок
    [Header("Geometry Dash feel (куб)")]
    [SerializeField] private float cubeSpinFraction = 0.8f; // доля времени полёта, за которую куб делает 90°
    [SerializeField] private float cubeMaxFallSpeed = 20f;  // максимальная скорость падения куба
    [SerializeField] private bool holdToJump = false;      // зажатие прыгает само при приземлении (выключено: подозрение на подбросы)
    [SerializeField] private Vector3 cubeSpriteOffset = Vector3.zero;
    [SerializeField] private bool inheritMainSpriteColor = true;
    
    [Header("Коллайдеры для каждого режима")]
    [SerializeField] private Collider2D cubeCollider;
    [SerializeField] private Collider2D shipCollider;
    [SerializeField] private Collider2D waveCollider;
    [SerializeField] private Collider2D ufoCollider;
    [SerializeField] private Collider2D spiderCollider;
    [SerializeField] private Collider2D ballCollider;

    [Header("Ground Check")]
    [SerializeField] private float coyoteTime = 0.15f;   // время буфера прыжка
    private float coyoteTimer;

    [Header("Ball Coyote Time")]
    [SerializeField] private float ballCoyoteTime = 0.15f;
    private float ballCoyoteTimer;
    private bool ballGrounded;

    [Header("Иконка куба для UFO и Ship")]
    [SerializeField] private SpriteRenderer cubeIconRenderer;
    [SerializeField] private Vector3 cubeIconOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float cubeIconScale = 0.5f;
    [SerializeField] private bool showCubeIconInShipMode = true;
    [SerializeField] private bool showCubeIconInUFOMode = true;
    
    [Header("Wave Trail Effects")]
    [SerializeField] private GameObject waveTrailPrefab;
    [SerializeField] private float trailSpawnInterval = 0.03f;
    [SerializeField] private float trailLifeTime = 0.8f;
    [SerializeField] private Color trailStartColor = Color.yellow;
    [SerializeField] private Color trailEndColor = new Color(1, 1, 0, 0);
    
    [Header("Wave Settings")]
    [SerializeField] private float waveHoldForce = 18f;
    [SerializeField] private float waveMaxSpeed = 15f;
    [SerializeField] private float waveSmoothRotation = 8f;
    [SerializeField] private float waveReleaseGravity = 2f;
    [SerializeField] private float waveDrag = 0.3f;
    
    [Header("Wave Rotation Settings")]
    [SerializeField] private float waveMaxRotationAngle = 45f;
    [SerializeField] private float waveRotationSpeed = 10f;
    
    [Header("Ship Rotation Settings")]
    [SerializeField] private float shipMaxRotationAngle = 25f;
    [SerializeField] private float shipRotationSpeed = 5f;
    
    [Header("Cube Jump Settings")]
    [SerializeField] private float cubeJumpRotationSpeed = 15f;
    [SerializeField] private float cubeMaxRotationAngle = 180f;
    
    [Header("Ball Settings")]
    [SerializeField] private float ballRotationSpeed = 300f;
    [SerializeField] private float ballBounceForce = 5f;
    [SerializeField] private float ballMinBounceSpeed = 3f;
    [SerializeField] private float ballTrailInterval = 0.05f;
    [SerializeField] private GameObject ballTrailPrefab;
    [SerializeField] private Color ballTrailColor = Color.green;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ballGroundCheckDistance = 0.3f;
    [SerializeField] private float ballFlipCooldown = 0.2f;
    
    [Header("Gravity Zone Animation")]
    [SerializeField] private float gravityZoneRotationSpeed = 360f;
    [SerializeField] private float gravityZoneScaleSpeed = 2f;
    [SerializeField] private float gravityZoneScaleAmount = 0.1f;
    [SerializeField] private Color gravityZoneColor = new Color(1f, 0.8f, 0.2f, 1f);
    
    [Header("Cube Stuck Detection")]
    [SerializeField] private float cubeStuckTimeThreshold = 0.05f;
    [SerializeField] private float cubeStuckYCorrection = 0.005f;
    [SerializeField] private float cubeStuckXDeltaThreshold = 0.001f;
    
    [Header("Editor Mode")]
    [SerializeField] private bool IsEditor = false;
    [SerializeField] private Vector2 editorRespawnPosition = Vector2.zero;
    [SerializeField] private bool showRespawnPreview = true;
    [SerializeField] private Color respawnPreviewColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private bool resetModeOnRespawn = true;

    [Header("Level Progress")]
    public Transform levelFinish;               // финишный объект уровня
    private LevelGenerator levelGenerator;      // ссылка на генератор (если нужна)

    [Header("Practice Mode")]
public GameObject practicePointMarkerPrefab; // префаб маркера стартовой точки
private List<GameObject> practiceMarkers = new List<GameObject>(); // список созданных маркеров

    [Header("Respawn")]
public Vector3 levelStartPosition;          // будет сохранена в Start()
private float initialMoveSpeed;             // исходная скорость игрока
public AudioSource respawnAudioSource;      // источник музыки при респавне

[Header("Practice Mode")]
public bool isPracticeMode = false;
public GameObject practicePanel;            // ссылка на префаб PracticeUI
public Button practiceSetPosButton;
public Button practiceRemovePosButton;
public KeyCode practiceSetPosKey = KeyCode.E;
public KeyCode practiceRemovePosKey = KeyCode.X;

[System.Serializable]
public struct PracticeStartPoint
{
    public Vector3 position;
    public PlayerMode mode;
    public float moveSpeed;
}
private List<PracticeStartPoint> practicePoints = new List<PracticeStartPoint>();
    
    // Изначальный режим игры (устанавливается при старте или в редакторе)
    private PlayerMode initialMode = PlayerMode.Cube;
    
    private bool isDead = false;
    private bool isGrounded = false;
    private bool isFlying = false;
    private bool isUpsideDown = false;
    private bool isHoldingWave = false;
    private bool isBallBouncing = false;
    private float trailTimer = 0f;
    private float ballTrailTimer = 0f;
    private List<GameObject> activeTrails = new List<GameObject>();
    private List<GameObject> ballTrails = new List<GameObject>();
    private float waveRotationVelocity;
    private float waveHoldTimer = 0f;
    private Vector3 originalScale;
    private float cubeJumpRotation = 0f;
    private bool isJumping = false;
    private float ballRotationAngle = 0f;
    private Vector2 lastBallVelocity;
    private bool wasBallGrounded = false;
    private bool canFlipBallGravity = true;
    private float ballFlipTimer = 0f;
    private bool isInGravityZone = false;
    private Color originalBallColor;
    private bool hasCubeIconBeenInitialized = false;
    
    // Переменные для отдельного спрайта куба
    private Quaternion targetCubeSpriteRotation = Quaternion.identity;
    private Quaternion currentCubeSpriteRotation = Quaternion.identity;
    private int currentCubeRotationStep = 0;
    private bool isCubeSpriteRotating = false;
    private float cubeSpriteRotationProgress = 0f;
    
    // Список активных ModeChanger'ов для сброса при респавне
    private List<ModeChanger> activeModeChangers = new List<ModeChanger>();
    
    // Публичное свойство для доступа к состоянию перевернутости из других скриптов
    public bool IsUpsideDown => isUpsideDown;
    
    // Переменные для отслеживания застревания куба
    private Vector2 lastCubePosition;
    private float cubeStuckTimer = 0f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();


        originalScale = transform.localScale;
        
        // Сохраняем изначальный режим из инспектора
        initialMode = currentMode;
        
        // Установка слоя земли по умолчанию, если не задан
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground", "Platform");
        }
        
        // Сохраняем исходный цвет шара
        if (spriteRenderer != null)
        {
            originalBallColor = spriteRenderer.color;
        }
        
        // Инициализируем иконку куба
        InitializeCubeIcon();
        
        // Инициализируем отдельный спрайт для куба
        InitializeCubeModeSpriteRenderer();
        
        // Находим все коллайдеры, если они не заданы в инспекторе
        FindAllColliders();
        
        // Инициализируем позицию для отслеживания застревания
        lastCubePosition = transform.position;
    }

    private void InitializeCubeModeSpriteRenderer()
    {
        // Если отдельный спрайт для куба включен, но не задан - создаем его
        if (useSeparateCubeSprite && cubeModeSpriteRenderer == null)
        {
            GameObject cubeSpriteObject = new GameObject("CubeModeSprite");
            cubeSpriteObject.transform.SetParent(transform);
            cubeSpriteObject.transform.localPosition = cubeSpriteOffset;
            cubeSpriteObject.transform.localScale = Vector3.one;
            cubeSpriteObject.transform.localRotation = Quaternion.identity;
            
            cubeModeSpriteRenderer = cubeSpriteObject.AddComponent<SpriteRenderer>();
            cubeModeSpriteRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 1;
            
            // Устанавливаем спрайт, если задан
            if (cubeModeSprite != null)
            {
                cubeModeSpriteRenderer.sprite = cubeModeSprite;
            }
            else if (cubeSprite != null)
            {
                cubeModeSpriteRenderer.sprite = cubeSprite;
            }
            
            // Наследуем цвет от основного спрайта
            if (inheritMainSpriteColor && spriteRenderer != null)
            {
                cubeModeSpriteRenderer.color = spriteRenderer.color;
            }
            
            Debug.Log("Создан отдельный спрайт для режима куба", cubeSpriteObject);
        }
        else if (cubeModeSpriteRenderer != null)
        {
            // Настраиваем существующий спрайт
            cubeModeSpriteRenderer.transform.localPosition = cubeSpriteOffset;
            
            // Устанавливаем спрайт, если задан
            if (cubeModeSprite != null)
            {
                cubeModeSpriteRenderer.sprite = cubeModeSprite;
            }
            else if (cubeSprite != null && cubeModeSpriteRenderer.sprite == null)
            {
                cubeModeSpriteRenderer.sprite = cubeSprite;
            }
            
            // Наследуем цвет от основного спрайта
            if (inheritMainSpriteColor && spriteRenderer != null)
            {
                cubeModeSpriteRenderer.color = spriteRenderer.color;
            }
        }
        
        // Изначально скрываем отдельный спрайт куба
        if (cubeModeSpriteRenderer != null)
        {
            cubeModeSpriteRenderer.enabled = false;
        }
    }

    private void FindAllColliders()
    {
        // Находим или создаем коллайдеры для каждого режима
        Collider2D[] allColliders = GetComponents<Collider2D>();
        
        // По умолчанию используем первый найденный коллайдер как основной
        if (cubeCollider == null && allColliders.Length > 0)
        {
            cubeCollider = allColliders[0];
        }
        
        // Если у нас есть дочерние объекты с коллайдерами, можно искать по тегам или именам
        foreach (Transform child in transform)
        {
            Collider2D childCollider = child.GetComponent<Collider2D>();
            if (childCollider != null)
            {
                if (child.name.Contains("Cube") && cubeCollider == null) cubeCollider = childCollider;
                if (child.name.Contains("Ship") && shipCollider == null) shipCollider = childCollider;
                if (child.name.Contains("Wave") && waveCollider == null) waveCollider = childCollider;
                if (child.name.Contains("UFO") && ufoCollider == null) ufoCollider = childCollider;
                if (child.name.Contains("Spider") && spiderCollider == null) spiderCollider = childCollider;
                if (child.name.Contains("Ball") && ballCollider == null) ballCollider = childCollider;
            }
        }
        
        // Если какие-то коллайдеры не найдены, создаем их как дочерние объекты
        if (cubeCollider == null) cubeCollider = CreateColliderForMode("Cube", PlayerMode.Cube);
        if (shipCollider == null) shipCollider = CreateColliderForMode("Ship", PlayerMode.Ship);
        if (waveCollider == null) waveCollider = CreateColliderForMode("Wave", PlayerMode.Wave);
        if (ufoCollider == null) ufoCollider = CreateColliderForMode("UFO", PlayerMode.UFO);
        if (spiderCollider == null) spiderCollider = CreateColliderForMode("Spider", PlayerMode.Spider);
        if (ballCollider == null) ballCollider = CreateColliderForMode("Ball", PlayerMode.Ball);
        
        // Выключаем все коллайдеры кроме текущего режима
        SetActiveCollider(currentMode);
    }

    private Collider2D CreateColliderForMode(string modeName, PlayerMode mode)
    {
        GameObject colliderObj = new GameObject(modeName + "Collider");
        colliderObj.transform.SetParent(transform);
        colliderObj.transform.localPosition = Vector3.zero;
        colliderObj.transform.localRotation = Quaternion.identity;
        colliderObj.transform.localScale = Vector3.one;
        
        Collider2D newCollider = null;
        
        switch (mode)
        {
            case PlayerMode.Cube:
                BoxCollider2D cubeBox = colliderObj.AddComponent<BoxCollider2D>();
                cubeBox.size = new Vector2(0.8f, 0.8f);
                newCollider = cubeBox;
                break;
                
            case PlayerMode.Ship:
                BoxCollider2D shipBox = colliderObj.AddComponent<BoxCollider2D>();
                shipBox.size = new Vector2(0.8f, 0.6f);
                newCollider = shipBox;
                break;
                
            case PlayerMode.Wave:
                BoxCollider2D waveBox = colliderObj.AddComponent<BoxCollider2D>();
                waveBox.size = new Vector2(0.7f, 0.5f);
                newCollider = waveBox;
                break;
                
            case PlayerMode.UFO:
                CircleCollider2D ufoCircle = colliderObj.AddComponent<CircleCollider2D>();
                ufoCircle.radius = 0.4f;
                newCollider = ufoCircle;
                break;
                
            case PlayerMode.Spider:
                BoxCollider2D spiderBox = colliderObj.AddComponent<BoxCollider2D>();
                spiderBox.size = new Vector2(0.6f, 0.4f);
                newCollider = spiderBox;
                break;
                
            case PlayerMode.Ball:
                CircleCollider2D ballCircle = colliderObj.AddComponent<CircleCollider2D>();
                ballCircle.radius = 0.4f;
                newCollider = ballCircle;
                break;
        }
        
        return newCollider;
    }

    private void SetActiveCollider(PlayerMode mode)
    {
        // Выключаем все коллайдеры
        if (cubeCollider != null) cubeCollider.enabled = false;
        if (shipCollider != null) shipCollider.enabled = false;
        if (waveCollider != null) waveCollider.enabled = false;
        if (ufoCollider != null) ufoCollider.enabled = false;
        if (spiderCollider != null) spiderCollider.enabled = false;
        if (ballCollider != null) ballCollider.enabled = false;
        
        // Включаем коллайдер для текущего режима
        switch (mode)
        {
            case PlayerMode.Cube:
                if (cubeCollider != null) cubeCollider.enabled = true;
                break;
            case PlayerMode.Ship:
                if (shipCollider != null) shipCollider.enabled = true;
                break;
            case PlayerMode.Wave:
                if (waveCollider != null) waveCollider.enabled = true;
                break;
            case PlayerMode.UFO:
                if (ufoCollider != null) ufoCollider.enabled = true;
                break;
            case PlayerMode.Spider:
                if (spiderCollider != null) spiderCollider.enabled = true;
                break;
            case PlayerMode.Ball:
                if (ballCollider != null) ballCollider.enabled = true;
                break;
        }
    }

    private void Start()
    {
        UpdateVisuals();
        UpdatePhysics();

        levelStartPosition = transform.position;
        initialMoveSpeed = moveSpeed;

            if (practiceToggleButton != null)
        practiceToggleButton.onClick.AddListener(TogglePracticeMode);
    if (practiceSetPosButton != null)
        practiceSetPosButton.onClick.AddListener(SetPracticePoint);
    if (practiceRemovePosButton != null)
        practiceRemovePosButton.onClick.AddListener(RemoveLastPracticePoint);

    // Панель практики по умолчанию неактивна
    if (practicePanel != null)
        practicePanel.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        
        rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
        
        if (currentMode == PlayerMode.Ship)
        {
            if (isFlying)
            {
                rb.AddForce(Vector2.up * shipFlyForce * Time.fixedDeltaTime * 60f);
            }
            rb.velocity = new Vector2(rb.velocity.x, 
                Mathf.Clamp(rb.velocity.y, -moveSpeed * 1.5f, moveSpeed * 1.5f));
            
            // Поворот кораблика в зависимости от направления полета
            HandleShipRotation();
        }
        else if (currentMode == PlayerMode.Wave)
        {
            HandleWavePhysics();
            
            // Поворот волны в зависимости от направления движения
            HandleWaveRotation();
            
            // Спавн трейлов
            if (waveTrailPrefab != null)
            {
                trailTimer += Time.fixedDeltaTime;
                if (trailTimer >= trailSpawnInterval)
                {
                    SpawnWaveTrail();
                    trailTimer = 0f;
                }
            }
        }
        else if (currentMode == PlayerMode.Spider)
        {
            rb.velocity = new Vector2(rb.velocity.x, 
                Mathf.Clamp(rb.velocity.y, -moveSpeed * 2f, moveSpeed * 2f));
        }
        else if (currentMode == PlayerMode.UFO)
        {
            rb.velocity = new Vector2(rb.velocity.x, 
                Mathf.Clamp(rb.velocity.y, -moveSpeed, moveSpeed));
        }
        else if (currentMode == PlayerMode.Cube)
        {
            // Куб сам ведёт вертикальную скорость: гравитация Unity выключена,
            // поэтому высота и время прыжка не зависят от внешних сил и импульсов.
            rb.gravityScale = 0f;
            float vy = rb.velocity.y;
            if (isGrounded && vy <= 0f)
                vy = 0f;
            else
                vy -= cubeGravity * Time.fixedDeltaTime;
            vy = Mathf.Clamp(vy, -cubeMaxFallSpeed, jumpForce);
            rb.velocity = new Vector2(rb.velocity.x, vy);

            // Обработка вращения куба при прыжке (только визуально через отдельный спрайт)
            HandleCubeSpriteRotation();
            ClearTrails();
            
            // Проверка застревания куба по оси X
            CheckCubeStuck();
        }
        else if (currentMode == PlayerMode.Ball)
        {
            HandleBallPhysics();
            
                // Обновляем буфер прыжка (coyote time) только здесь, на основе FixedDeltaTime
  /*  if (ballGrounded)
        ballCoyoteTimer = ballCoyoteTime;
    else
        ballCoyoteTimer -= Time.fixedDeltaTime;*/

            // Анимация вращения в зоне гравитации
            if (isInGravityZone)
            {
                HandleGravityZoneAnimation();
            }
            
            // Спавн трейлов для шара
            if (ballTrailPrefab != null)
            {
                ballTrailTimer += Time.fixedDeltaTime;
                if (ballTrailTimer >= ballTrailInterval)
                {
                    SpawnBallTrail();
                    ballTrailTimer = 0f;
                }
            }
            
            // Обновление таймера задержки переворота
            if (ballFlipTimer > 0)
            {
                ballFlipTimer -= Time.fixedDeltaTime;
                if (ballFlipTimer <= 0)
                {
                    canFlipBallGravity = true;
                }
            }
        }
        else
        {
            ClearTrails();
        }
        
        // Сохраняем скорость шара для определения отскока
        if (currentMode == PlayerMode.Ball)
        {
            lastBallVelocity = rb.velocity;
        }
        
        // Обновляем позицию иконки куба
        UpdateCubeIconPosition();
    }
    
    private void HandleCubeSpriteRotation()
    {
        if (!useSeparateCubeSprite || cubeModeSpriteRenderer == null) return;

        Transform spin = cubeModeSpriteRenderer.transform;
        if (!isGrounded)
        {
            // В воздухе — постоянная угловая скорость, но не больше 90° за прыжок
            float step = Mathf.Min(CubeSpinSpeed * Time.fixedDeltaTime, 90f - cubeSpunThisJump);
            if (step > 0f)
            {
                spin.localRotation *= Quaternion.Euler(0f, 0f, -step);
                cubeSpunThisJump += step;
            }
        }
        else if (!cubeWasGrounded)
        {
            // Момент касания земли: сразу округляем угол до ближайшего кратного 90°
            float z = spin.localEulerAngles.z;
            spin.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round(z / 90f) * 90f);
            cubeSpunThisJump = 0f;
        }
        cubeWasGrounded = isGrounded;
    }
    
    public void RotateCubeSprite90Degrees()
    {
        if (!useSeparateCubeSprite || cubeModeSpriteRenderer == null) return;
        
        // Увеличиваем шаг вращения
        currentCubeRotationStep++;
        
        // Вычисляем целевой угол поворота (90 градусов за шаг)
        float targetAngle = currentCubeRotationStep * 90f;
        
        // Создаем кватернион для целевого поворота
        targetCubeSpriteRotation = Quaternion.Euler(0f, 0f, targetAngle);
        
        // Начинаем анимацию вращения
        isCubeSpriteRotating = true;
        cubeSpriteRotationProgress = 0f;
        
        // Запоминаем текущее вращение как стартовую точку
        currentCubeSpriteRotation = cubeModeSpriteRenderer.transform.localRotation;
    }
    
    private void CheckCubeStuck()
    {
        if (currentMode != PlayerMode.Cube) return;
        
        // Получаем текущую позицию
        Vector2 currentPosition = transform.position;
        
        // Вычисляем изменение позиции по X
        float xDelta = Mathf.Abs(currentPosition.x - lastCubePosition.x);
        
        // Если изменение по X меньше порогового значения
        if (xDelta < cubeStuckXDeltaThreshold)
        {
            // Увеличиваем таймер застревания
            cubeStuckTimer += Time.fixedDeltaTime;
            
            // Если таймер превысил порог
            if (cubeStuckTimer > cubeStuckTimeThreshold)
            {
                // Больше не телепортируем куб вверх: это и загоняло его внутрь блоков.
                // Столкновение с препятствием обрабатывается в OnCollisionEnter2D (смерть).
                cubeStuckTimer = 0f;
                lastCubePosition = transform.position;
            }
        }
        else
        {
            // Если куб движется, сбрасываем таймер
            cubeStuckTimer = 0f;
            lastCubePosition = currentPosition;
        }
    }

    private void InitializeCubeIcon()
    {
        if (cubeIconRenderer == null)
        {
            // Создаем объект для иконки куба, если он не задан
            GameObject cubeIconObject = new GameObject("CubeIcon");
            cubeIconObject.transform.SetParent(transform);
            cubeIconObject.transform.localPosition = cubeIconOffset;
            cubeIconObject.transform.localScale = Vector3.one * cubeIconScale;
            cubeIconObject.transform.localRotation = Quaternion.identity;
            
            cubeIconRenderer = cubeIconObject.AddComponent<SpriteRenderer>();
            cubeIconRenderer.sortingOrder = 1;
            
            Debug.Log("Создана иконка куба как дочерний объект", cubeIconObject);
        }
        else
        {
            // Настраиваем существующий рендерер
            cubeIconRenderer.transform.localPosition = cubeIconOffset;
            cubeIconRenderer.transform.localScale = Vector3.one * cubeIconScale;
        }
        
        hasCubeIconBeenInitialized = true;
        
        // Сразу скрываем иконку, она будет показана только в нужных режимах
        if (cubeIconRenderer != null)
        {
            cubeIconRenderer.enabled = false;
        }
    }

    private void UpdateCubeIconPosition()
    {
        if (cubeIconRenderer == null || !cubeIconRenderer.enabled) return;
        
        // Обновляем позицию иконки относительно игрока
        cubeIconRenderer.transform.localPosition = cubeIconOffset;
        
        // Иконка всегда должна смотреть вверх (не вращаться вместе с игроком)
        cubeIconRenderer.transform.rotation = Quaternion.identity;
    }

    private void HandleShipRotation()
    {
        float targetRotation = 0f;
        
        if (isFlying)
        {
            // При полете вверх - поднимаем нос
            targetRotation = shipMaxRotationAngle;
        }
        else if (rb.velocity.y < -1f)
        {
            // При падении - опускаем нос
            targetRotation = -shipMaxRotationAngle;
        }
        else
        {
            // При горизонтальном полете - нормальное положение
            targetRotation = 0f;
        }
        
        // Плавный поворот
        float currentRotation = transform.eulerAngles.z;
        if (currentRotation > 180f) currentRotation -= 360f;
        
        float newRotation = Mathf.Lerp(currentRotation, targetRotation, shipRotationSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newRotation);
    }

    private void HandleWaveRotation()
    {
        float targetRotation = 0f;
        
        // Определяем направление движения с учетом гравитации
        float verticalVelocity = rb.velocity.y;
        if (isUpsideDown)
        {
            // Если перевернуты, инвертируем восприятие направления
            verticalVelocity = -verticalVelocity;
        }
        
        // Устанавливаем целевой угол в зависимости от направления движения
        if (verticalVelocity > 0.5f)
        {
            // Движемся вверх - поворачиваем вверх
            targetRotation = waveMaxRotationAngle;
        }
        else if (verticalVelocity < -0.5f)
        {
            // Движемся вниз - поворачиваем вниз
            targetRotation = -waveMaxRotationAngle;
        }
        else
        {
            // Нейтральное положение
            targetRotation = 0f;
        }
        
        // Плавный поворот с учетом текущего вращения
        float currentRotation = transform.eulerAngles.z;
        if (currentRotation > 180f) currentRotation -= 360f;
        
        // Применяем сглаживание вращения
        float newRotation = Mathf.Lerp(currentRotation, targetRotation, waveRotationSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newRotation);
    }

    private void HandleBallPhysics()
    {
        /* Проверяем, находится ли шар рядом с землей (если не в зоне гравитации)
        if (!isInGravityZone)
        {
            CheckBallGroundProximity();
        }
        
        //* Вращение шара в зависимости от скорости (если не в зоне гравитации)
        if (!isInGravityZone && Mathf.Abs(rb.velocity.y) > 0.1f)
        {
            float rotationDirection = rb.velocity.y > 0 ? 1f : -1f;
            ballRotationAngle += ballRotationSpeed * rotationDirection * Time.fixedDeltaTime;
            
            // Нормализуем угол
            if (ballRotationAngle > 360f) ballRotationAngle -= 360f;
            if (ballRotationAngle < -360f) ballRotationAngle += 360f;
            
            transform.rotation = Quaternion.Euler(0f, 0f, ballRotationAngle);
        }*/
        
        // Ограничение максимальной скорости шара
        if (Mathf.Abs(rb.velocity.y) > ballMaxSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Sign(rb.velocity.y) * ballMaxSpeed);
        }
        
        // Автоматический отскок при приземлении
        if (isGrounded && !wasBallGrounded && Mathf.Abs(lastBallVelocity.y) > ballMinBounceSpeed)
        {
            BallBounce();
        }
        
        wasBallGrounded = isGrounded;
    }

    private void HandleGravityZoneAnimation()
    {
        if (currentMode != PlayerMode.Ball || !isInGravityZone) return;
        
        // Вращение шара в зоне гравитации
        float rotationDirection = (rb.gravityScale >= 0) ? 1f : -1f;
        ballRotationAngle += gravityZoneRotationSpeed * rotationDirection * Time.fixedDeltaTime;
        
        // Нормализуем угол
        if (ballRotationAngle > 360f) ballRotationAngle -= 360f;
        if (ballRotationAngle < -360f) ballRotationAngle += 360f;
        
        transform.rotation = Quaternion.Euler(0f, 0f, ballRotationAngle);
        
        // Пульсация масштаба
        float scaleFactor = 1f + Mathf.Sin(Time.time * gravityZoneScaleSpeed) * gravityZoneScaleAmount;
        transform.localScale = originalScale * scaleFactor;
        
        // Меняем цвет шара в зоне гравитации
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(originalBallColor, gravityZoneColor, 
                Mathf.PingPong(Time.time * 2f, 1f));
        }
    }

private void CheckBallGroundProximity()
{
    if (currentMode != PlayerMode.Ball || isInGravityZone) return;

    Vector2 checkDirection = rb.gravityScale > 0 ? Vector2.down : Vector2.up;
    RaycastHit2D hit = Physics2D.Raycast(
        transform.position, 
        checkDirection, 
        ballGroundCheckDistance, 
        groundLayer
    );

    Debug.DrawRay(transform.position, checkDirection * ballGroundCheckDistance, 
                 hit.collider != null ? Color.green : Color.red);

    ballGrounded = (hit.collider != null);
    canFlipBallGravity = ballGrounded;   // оставляем основную переменную
}

   private void HandleWavePhysics()
{
    // Мгновенно задаём вертикальную скорость в зависимости от удержания и ориентации
    float direction = isHoldingWave ? 1f : -1f;   // вверх / вниз
    if (isUpsideDown) direction = -direction;     // инверсия при перевороте гравитации

    float targetVerticalSpeed = direction * moveSpeed;
    rb.velocity = new Vector2(moveSpeed, targetVerticalSpeed);

    // Отключаем физическую гравитацию и сопротивление — теперь всем управляем мы
    rb.gravityScale = 0f;
    rb.drag = 0f;

    // Масштаб возвращаем к исходному (без деформаций от скорости)
    transform.localScale = originalScale;
}

  private void Update()
{
    if (isDead) return;
    
    // Горячие клавиши режима практики (работают всегда, но действия выполняются только при isPracticeMode)
    if (isPracticeMode)
    {
        if (Input.GetKeyDown(practiceSetPosKey))
            SetPracticePoint();
        if (Input.GetKeyDown(practiceRemovePosKey))
            RemoveLastPracticePoint();
    }

        if (isGrounded)
        coyoteTimer = coyoteTime;
    else
        coyoteTimer -= Time.deltaTime;

        // Обновляем ballGrounded каждый кадр

if (ballGrounded)
    ballCoyoteTimer = ballCoyoteTime;
else
    ballCoyoteTimer -= Time.deltaTime;


    
    // Обработка ввода для волны - можно зажимать сколько угодно
    if (currentMode == PlayerMode.Wave)
    {
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            if (!isHoldingWave)
            {
                StartWaveHold();
            }
        }
        else
        {
            if (isHoldingWave)
            {
                EndWaveHold();
            }
        }
    }
    else
    {
        // Куб: зажатие прыгает само, как в Geometry Dash
        if (currentMode == PlayerMode.Cube && holdToJump &&
            (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) &&
            (isGrounded || coyoteTimer > 0))
        {
            Jump();
            coyoteTimer = 0;
        }

        // Обычная обработка для других режимов
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            switch (currentMode)
            {
                case PlayerMode.Cube:
    if (isGrounded || coyoteTimer > 0)   // <-- было: if (isGrounded)
    {
        Jump();
        coyoteTimer = 0;                 // сбрасываем буфер, чтобы не было двойных прыжков
    }
    break;
                    
                case PlayerMode.Ship:
                    isFlying = true;
                    rb.gravityScale = 0f;

                   /* if (rb.velocity.y < 0f)
                        rb.velocity = new Vector2(rb.velocity.x, 0f);*/
                    break;
                    
                case PlayerMode.UFO:
                    UFOJump();
                    break;
                    
                case PlayerMode.Spider:
                    SpiderJump();
                    break;
                    
                case PlayerMode.Ball:
                   if (isInGravityZone || ballCoyoteTimer > 0)
{
    FlipBallGravity(ballCoyoteTimer > 0);   // принудительно, если сработал буфер
    ballCoyoteTimer = 0;
}
                    break;
            }
        }
        
        if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space)))
        {
            if (currentMode == PlayerMode.Ship)
            {
                isFlying = false;
                rb.gravityScale = shipGravityScale;
            }
        }
    }
    
    // Быстрое перемещение в редакторе (для отладки)
    if (IsEditor && Input.GetKeyDown(KeyCode.R))
    {
        TeleportToRespawn();
    }
}

public void FlipBallGravity(bool forceGroundCheck = false)
{
    // Защита от повторного срабатывания (кулдаун)
    if (ballFlipTimer > 0) return;

    // Если не принудительно, требуем canFlipBallGravity или зону гравитации
    if (!forceGroundCheck && !canFlipBallGravity && !isInGravityZone) return;

    // Инвертируем гравитацию
    rb.gravityScale = -rb.gravityScale;

    // Даём толчок в направлении новой гравитации
    float currentVert = rb.velocity.y;
    float targetSpeed = ballFlipForce;   // всегда даём полный импульс
    if (rb.gravityScale > 0)   // новая гравитация вниз
        rb.velocity = new Vector2(rb.velocity.x, -targetSpeed);
    else                       // новая гравитация вверх
        rb.velocity = new Vector2(rb.velocity.x, targetSpeed);

    ballFlipTimer = ballFlipCooldown;
    canFlipBallGravity = false;

    StartCoroutine(BallClickEffect());
}

    private void BallBounce()
    {
        // Автоматический отскок от земли с эффектом
        rb.velocity = new Vector2(rb.velocity.x, -rb.velocity.y * 0.7f);
        isBallBouncing = true;
        
        StartCoroutine(BallBounceEffect());
    }

    private IEnumerator BallClickEffect()
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.15f;
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = targetScale;
        
        // Возвращаем к нормальному размеру
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }

    private IEnumerator BallBounceEffect()
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        float duration = 0.08f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = targetScale;
        
        // Возвращаем к нормальному размеру
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
        isBallBouncing = false;
    }

    private void StartWaveHold()
    {
        isHoldingWave = true;
        waveHoldTimer = 0f;
        
        if (spriteRenderer != null)
        {
            StartCoroutine(WaveHoldEffect());
        }
    }

    private void EndWaveHold()
    {
        isHoldingWave = false;
        waveHoldTimer = 0f;
        
        if (spriteRenderer != null)
        {
            StartCoroutine(WaveReleaseEffect());
        }
    }

    private IEnumerator WaveHoldEffect()
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.05f;
        
        float duration = 0.15f;
        float elapsed = 0f;
        
        while (elapsed < duration && isHoldingWave)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (isHoldingWave)
        {
            transform.localScale = targetScale;
        }
    }

    private IEnumerator WaveReleaseEffect()
    {
        Vector3 currentScale = transform.localScale;
        Vector3 targetScale = originalScale;
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(currentScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = targetScale;
    }

    private void UFOJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, ufoJumpForce);
    }

    private void SpiderJump()
    {
        if (isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, isUpsideDown ? -spiderJumpForce : spiderJumpForce);
        }
    }

    private void SpawnWaveTrail()
    {
        if (waveTrailPrefab == null) return;
        
        GameObject trail = Instantiate(waveTrailPrefab, transform.position, transform.rotation);
        SpriteRenderer trailRenderer = trail.GetComponent<SpriteRenderer>();
        
        if (trailRenderer != null)
        {
            trailRenderer.sprite = waveSprite;
            trailRenderer.flipY = isUpsideDown;
            trailRenderer.color = trailStartColor;
            
            float randomScale = Random.Range(0.7f, 1.1f);
            float speedScale = Mathf.Clamp(Mathf.Abs(rb.velocity.y) / waveMaxSpeed, 0.5f, 1.2f);
            trail.transform.localScale = originalScale * randomScale * speedScale * 0.8f;
        }
        
        StartCoroutine(FadeAndDestroyTrail(trail, trailRenderer));
        activeTrails.Add(trail);
    }

    private void SpawnBallTrail()
    {
        if (ballTrailPrefab == null) return;
        
        GameObject trail = Instantiate(ballTrailPrefab, transform.position, Quaternion.identity);
        SpriteRenderer trailRenderer = trail.GetComponent<SpriteRenderer>();
        
        if (trailRenderer != null)
        {
            trailRenderer.sprite = ballSprite;
            
            // Меняем цвет трейла в зависимости от нахождения в зоне гравитации
            if (isInGravityZone)
            {
                trailRenderer.color = gravityZoneColor;
            }
            else
            {
                trailRenderer.color = ballTrailColor;
            }
            
            // Размер трейла зависит от скорости
            float speedScale = Mathf.Clamp(Mathf.Abs(rb.velocity.y) / ballMaxSpeed, 0.3f, 1f);
            
            // В зоне гравитации делаем трейлы больше
            if (isInGravityZone)
            {
                speedScale *= 1.2f;
            }
            
            trail.transform.localScale = originalScale * 0.6f * speedScale;
        }
        
        StartCoroutine(FadeAndDestroyBallTrail(trail, trailRenderer));
        ballTrails.Add(trail);
    }

    private IEnumerator FadeAndDestroyTrail(GameObject trail, SpriteRenderer renderer)
    {
        float elapsed = 0f;
        Vector3 startScale = trail.transform.localScale;
        Vector3 endScale = startScale * 0.3f;
        
        while (elapsed < trailLifeTime)
        {
            if (renderer != null && trail != null)
            {
                float t = elapsed / trailLifeTime;
                
                renderer.color = Color.Lerp(trailStartColor, trailEndColor, t);
                trail.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                
                Color currentColor = renderer.color;
                currentColor.a = Mathf.Lerp(1f, 0f, t * 1.5f);
                renderer.color = currentColor;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (trail != null)
        {
            activeTrails.Remove(trail);
            Destroy(trail);
        }
    }

    private IEnumerator FadeAndDestroyBallTrail(GameObject trail, SpriteRenderer renderer)
    {
        float elapsed = 0f;
        float ballTrailLifeTime = trailLifeTime * 0.8f;
        Vector3 startScale = trail.transform.localScale;
        Vector3 endScale = startScale * 0.2f;
        
        Color startColor = renderer != null ? renderer.color : ballTrailColor;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsed < ballTrailLifeTime)
        {
            if (renderer != null && trail != null)
            {
                float t = elapsed / ballTrailLifeTime;
                
                renderer.color = Color.Lerp(startColor, endColor, t);
                trail.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (trail != null)
        {
            ballTrails.Remove(trail);
            Destroy(trail);
        }
    }

    private void ClearTrails()
    {
        foreach (GameObject trail in activeTrails)
        {
            if (trail != null) Destroy(trail);
        }
        activeTrails.Clear();
        trailTimer = 0f;
        
        foreach (GameObject trail in ballTrails)
        {
            if (trail != null) Destroy(trail);
        }
        ballTrails.Clear();
        ballTrailTimer = 0f;
    }

    public void FlipGravity()
    {
        if (currentMode == PlayerMode.Wave || currentMode == PlayerMode.Spider)
        {
            isUpsideDown = !isUpsideDown;
            spriteRenderer.flipY = isUpsideDown;
            
            float currentGravity = isHoldingWave ? waveGravityScale : waveReleaseGravity;
            rb.gravityScale = isUpsideDown ? -currentGravity : currentGravity;
            
            // При перевороте инвертируем вращение
            transform.rotation = Quaternion.Euler(0f, 0f, -transform.eulerAngles.z);
        }
        else if (currentMode == PlayerMode.Ball)
        {
            // Для шара переворот гравитации работает иначе
            // Теперь шар может менять гравитацию только если рядом с землей или в зоне гравитации
            if ((canFlipBallGravity || isInGravityZone) && ballFlipTimer <= 0)
            {
                rb.gravityScale = -rb.gravityScale;
                rb.velocity = new Vector2(rb.velocity.x, -rb.velocity.y);
                
                // Устанавливаем задержку
                ballFlipTimer = ballFlipCooldown;
                canFlipBallGravity = false;
            }
        }
        else
        {
            // Для других режимов просто инвертируем гравитацию
            rb.gravityScale *= -1;
        }
    }

    // Есть ли контакт, у которого поверхность смотрит вверх (куб приземлился на блок)
    private bool HasTopContact(Collision2D collision)
    {
        foreach (ContactPoint2D c in collision.contacts)
        {
            if (c.normal.y > 0.3f) return true;
        }
        return false;
    }

    // Довернуть спрайт по часовой стрелке до ближайшей грани (кратной 90°) и встать на неё
    private void StandOnNextFaceClockwise()
    {
        isGrounded = true;
        cubeWasGrounded = true;
        cubeSpunThisJump = 0f;
        isJumping = false;

        if (rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(0f, rb.velocity.y));
        }

        if (!useSeparateCubeSprite || cubeModeSpriteRenderer == null) return;

        Transform spin = cubeModeSpriteRenderer.transform;
        // Unity: положительный Z — против часовой стрелки, поэтому по часовой угол уменьшается
        float z = Mathf.DeltaAngle(0f, spin.localEulerAngles.z);
        float target = Mathf.Floor(z / 90f) * 90f;
        if (Mathf.Abs(target - z) < 1f) target -= 90f; // всегда поворачиваем хотя бы немного
        spin.localRotation = Quaternion.Euler(0f, 0f, target);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && !isDead)
        {
            bool isSpike = collision.gameObject.name.ToLower().Contains("spike");

            if (currentMode == PlayerMode.Cube && !isSpike && HasTopContact(collision))
            {
                // Куб упал ребром/углом на блок: не разбиваемся, а встаём на грань,
                // до которой он довернётся по часовой стрелке
                StandOnNextFaceClockwise();
            }
            else if (currentMode == PlayerMode.Ship || currentMode == PlayerMode.Wave ||
                currentMode == PlayerMode.UFO || currentMode == PlayerMode.Spider || currentMode == PlayerMode.Ball)
            {
                ContactPoint2D contact = collision.contacts[0];
                if (Mathf.Abs(contact.normal.x) > 0.5f)
                {
                    Die();
                }
            }
            else
            {
                Die();
            }
        }
        
        // Проверка земли для разных режимов
        if (collision.contacts.Length > 0)
        {
            if ((currentMode == PlayerMode.Cube || currentMode == PlayerMode.Spider || currentMode == PlayerMode.Ball) && 
                (collision.contacts[0].normal.y > 0.5f || collision.contacts[0].normal.y < -0.5f))
            {
                isGrounded = true;
                
                if (currentMode == PlayerMode.Cube)
                {
                    isJumping = false;
                    cubeJumpRotation = 0f;
                    transform.rotation = Quaternion.identity;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (currentMode == PlayerMode.Cube || currentMode == PlayerMode.Spider || currentMode == PlayerMode.Ball)
        {
            isGrounded = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("GravityPortal"))
        {
            if (currentMode == PlayerMode.Wave)
            {
                FlipGravity();
            }
            else if (currentMode == PlayerMode.Ball)
            {
                // Для шара гравитационные порталы временно разрешают менять гравитацию
                canFlipBallGravity = true;
                ballFlipTimer = 0f; // Сбрасываем таймер
            }
        }
        else if (other.CompareTag("ModePortal"))
        {
            // Получаем ModeChanger и добавляем в список активных
            ModeChanger changer = other.GetComponent<ModeChanger>();
            if (changer != null && !activeModeChangers.Contains(changer))
            {
                activeModeChangers.Add(changer);
            }
        }
        else if (other.CompareTag("GravityZone"))
        {
            // Зона, где шар может менять гравитацию в любое время
            if (currentMode == PlayerMode.Ball)
            {
                isInGravityZone = true;
                canFlipBallGravity = true;
                ballFlipTimer = 0f;
                
                // Включаем анимацию вращения
                StartCoroutine(GravityZoneEnterEffect());
            }
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("GravityZone"))
        {
            // Выход из зоны, где шар может менять гравитацию
            if (currentMode == PlayerMode.Ball)
            {
                isInGravityZone = false;
                
                // Возвращаем нормальную проверку
                CheckBallGroundProximity();
                
                // Возвращаем нормальный цвет и масштаб
                StartCoroutine(GravityZoneExitEffect());
            }
        }
    }

    private IEnumerator GravityZoneEnterEffect()
    {
        // Эффект входа в зону гравитации
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            Color targetColor = gravityZoneColor;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(currentColor, targetColor, elapsed / duration);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator GravityZoneExitEffect()
    {
        // Эффект выхода из зоны гравитации
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(currentColor, originalBallColor, elapsed / duration);
                }
                
                // Плавно возвращаем масштаб к нормальному
                float scaleProgress = elapsed / duration;
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, scaleProgress);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Убеждаемся, что цвет и масштаб вернулись к исходным
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalBallColor;
            }
            transform.localScale = originalScale;
        }
    }

private void Die()
{
    // Приоритет практики: если активен режим практики, респавним на точку практики,
    // даже если включен флаг IsEditor
    if (isPracticeMode)
    {
        isDead = true;
        isHoldingWave = false;
        isJumping = false;
        isBallBouncing = false;

        if (crashParticles != null)
            crashParticles.Play();

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        if (useSeparateCubeSprite && cubeModeSpriteRenderer != null)
            cubeModeSpriteRenderer.enabled = false;
        if (cubeIconRenderer != null)
            cubeIconRenderer.enabled = false;

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        ClearTrails();

        if (respawnAudioSource != null)
            PlayRespawnMusicAtProgress();

        Invoke(nameof(RespawnAtPracticePoint), 0.5f);
        return;
    }

    // Обычное поведение редактора (без практики)
    if (IsEditor)
    {
        TeleportToRespawn();
        return;
    }

    // Обычный режим (не практика, не редактор)
    isDead = true;
    isHoldingWave = false;
    isJumping = false;
    isBallBouncing = false;

    if (crashParticles != null)
        crashParticles.Play();

    if (spriteRenderer != null)
        spriteRenderer.enabled = false;
    if (useSeparateCubeSprite && cubeModeSpriteRenderer != null)
        cubeModeSpriteRenderer.enabled = false;
    if (cubeIconRenderer != null)
        cubeIconRenderer.enabled = false;

    rb.velocity = Vector2.zero;
    rb.isKinematic = true;
    ClearTrails();

    if (respawnAudioSource != null)
        PlayRespawnMusicAtProgress();

    Invoke(nameof(RespawnAtLevelStart), 0.5f);
}

    private void TeleportToRespawn()
    {
        // Сбрасываем все состояния
        isDead = false;
        isHoldingWave = false;
        isJumping = false;
        isFlying = false;
        isBallBouncing = false;
        wasBallGrounded = false;
        
        // Останавливаем все корутины
        StopAllCoroutines();
        
        // Включаем основной спрайт
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalBallColor;
        }
        
        // Сбрасываем отдельный спрайт куба
        if (useSeparateCubeSprite && cubeModeSpriteRenderer != null)
        {
            cubeModeSpriteRenderer.enabled = false;
            cubeModeSpriteRenderer.transform.localRotation = Quaternion.identity;
            currentCubeSpriteRotation = Quaternion.identity;
            targetCubeSpriteRotation = Quaternion.identity;
            currentCubeRotationStep = 0;
            isCubeSpriteRotating = false;
            cubeSpriteRotationProgress = 0f;
        }
        
        // Включаем физику
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector2.zero;
        }
        
        // Телепортируем на указанную позицию
        transform.position = editorRespawnPosition;
        
        // Сбрасываем режим на изначальный, если включена опция
        if (resetModeOnRespawn)
        {
            currentMode = initialMode;
        }
        
        // Сбрасываем вращение и масштаб
        transform.rotation = Quaternion.identity;
        transform.localScale = originalScale;
        
        // Сбрасываем состояния режимов
        cubeJumpRotation = 0f;
        ballRotationAngle = 0f;
        waveHoldTimer = 0f;
        isUpsideDown = false;
        canFlipBallGravity = true;
        ballFlipTimer = 0f;
        isInGravityZone = false;
        
        // Сбрасываем активные порталы смены режима
        ResetModeChangers();
        
        // Обновляем физику и визуал
        UpdatePhysics();
        UpdateVisuals();
        
        // Очищаем трейлы
        ClearTrails();
        
        Debug.Log($"Игрок телепортирован в позицию: {editorRespawnPosition}", this);
    }
    
    // Метод для сброса всех ModeChanger'ов
    private void ResetModeChangers()
    {
        foreach (ModeChanger changer in activeModeChangers)
        {
            if (changer != null)
            {
                changer.ResetChanger();
            }
        }
        activeModeChangers.Clear();
    }

    private void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isGrounded = false;
    }

    private void UpdatePhysics()
    {
        // Активируем правильный коллайдер для текущего режима
        SetActiveCollider(currentMode);
        
        switch (currentMode)
        {
            case PlayerMode.Cube:
                rb.gravityScale = 0f; // гравитацией куба управляет cubeGravity в FixedUpdate
                rb.drag = 0f;
                isUpsideDown = false;
                isHoldingWave = false;
                isJumping = false;
                cubeJumpRotation = 0f;
                transform.rotation = Quaternion.identity;
                transform.localScale = originalScale;
                break;
                
            case PlayerMode.Ship:
                rb.gravityScale = isFlying ? 0f : shipGravityScale;
                rb.drag = 0.5f;
                isUpsideDown = false;
                isHoldingWave = false;
                transform.rotation = Quaternion.identity;
                transform.localScale = originalScale;
                break;
                
            case PlayerMode.Wave:
                HandleWavePhysics();      // немедленно применяем новую логику
                break;
                
            case PlayerMode.UFO:
                rb.gravityScale = ufoGravityScale;
                rb.drag = 0.3f;
                isUpsideDown = false;
                isHoldingWave = false;
                transform.rotation = Quaternion.identity;
                transform.localScale = originalScale;
                break;
                
            case PlayerMode.Spider:
                rb.gravityScale = isUpsideDown ? -spiderGravityScale : spiderGravityScale;
                rb.drag = 0.1f;
                isHoldingWave = false;
                transform.rotation = Quaternion.identity;
                transform.localScale = originalScale;
                break;
                
            case PlayerMode.Ball:
                rb.gravityScale = ballGravityScale;
                rb.drag = 0.2f;
                ballRotationAngle = 0f;
                wasBallGrounded = false;
                canFlipBallGravity = true;
                ballFlipTimer = 0f;
                transform.rotation = Quaternion.identity;
                transform.localScale = originalScale;
                break;
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        
        // Обновляем видимость основного спрайта и отдельного спрайта куба
        if (currentMode == PlayerMode.Cube && useSeparateCubeSprite)
        {
            // В режиме куба: основной спрайт невидим, отдельный спрайт куба видим
            spriteRenderer.enabled = false;
            
            if (cubeModeSpriteRenderer != null)
            {
                cubeModeSpriteRenderer.enabled = true;
                
                // Устанавливаем спрайт для отдельного спрайта куба
                if (cubeModeSprite != null)
                {
                    cubeModeSpriteRenderer.sprite = cubeModeSprite;
                }
                else if (SkinManager.Instance != null)
                {
                    cubeModeSpriteRenderer.sprite = SkinManager.Instance.GetCubeSkin();
                }
                else if (cubeSprite != null)
                {
                    cubeModeSpriteRenderer.sprite = cubeSprite;
                }
                
                // Наследуем цвет от основного спрайта
                if (inheritMainSpriteColor)
                {
                    cubeModeSpriteRenderer.color = spriteRenderer.color;
                }
                
                // Сбрасываем вращение при смене режима на куб
                cubeModeSpriteRenderer.transform.localRotation = Quaternion.identity;
                currentCubeSpriteRotation = Quaternion.identity;
                targetCubeSpriteRotation = Quaternion.identity;
                currentCubeRotationStep = 0;
                isCubeSpriteRotating = false;
                cubeSpriteRotationProgress = 0f;
            }
        }
        else
        {
            // В других режимах: основной спрайт видим, отдельный спрайт куба невидим
            spriteRenderer.enabled = true;
            
            if (cubeModeSpriteRenderer != null)
            {
                cubeModeSpriteRenderer.enabled = false;
            }
            
            // Обновляем основной спрайт в зависимости от режима
            if (SkinManager.Instance != null)
            {
                switch (currentMode)
                {
                    case PlayerMode.Cube:
                        spriteRenderer.sprite = SkinManager.Instance.GetCubeSkin();
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(false);
                        break;
                    case PlayerMode.Ship:
                        spriteRenderer.sprite = SkinManager.Instance.GetShipSkin();
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(showCubeIconInShipMode);
                        break;
                    case PlayerMode.Wave:
                        spriteRenderer.sprite = SkinManager.Instance.GetWaveSkin();
                        spriteRenderer.flipY = isUpsideDown;
                        UpdateCubeIconVisibility(false);
                        break;
                    case PlayerMode.UFO:
                        spriteRenderer.sprite = SkinManager.Instance.GetUFOSkin();
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(showCubeIconInUFOMode);
                        break;
                    case PlayerMode.Spider:
                        spriteRenderer.sprite = SkinManager.Instance.GetSpiderSkin();
                        spriteRenderer.flipY = isUpsideDown;
                        UpdateCubeIconVisibility(false);
                        break;
                    case PlayerMode.Ball:
                        spriteRenderer.sprite = SkinManager.Instance.GetBallSkin();
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(false);
                        
                        // Устанавливаем цвет в зависимости от нахождения в зоне гравитации
                        if (isInGravityZone)
                        {
                            spriteRenderer.color = gravityZoneColor;
                        }
                        else
                        {
                            spriteRenderer.color = originalBallColor;
                        }
                        break;
                }
            }
            else
            {
                // Используем стандартные спрайты, если SkinManager не доступен
                switch (currentMode)
                {
                    case PlayerMode.Cube:
                        spriteRenderer.sprite = cubeSprite;
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(false);
                        break;
                    case PlayerMode.Ship:
                        spriteRenderer.sprite = shipSprite;
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(showCubeIconInShipMode);
                        break;
                    case PlayerMode.Wave:
                        spriteRenderer.sprite = waveSprite;
                        spriteRenderer.flipY = isUpsideDown;
                        UpdateCubeIconVisibility(false);
                        break;
                    case PlayerMode.UFO:
                        spriteRenderer.sprite = ufoSprite;
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(showCubeIconInUFOMode);
                        break;
                    case PlayerMode.Spider:
                        spriteRenderer.sprite = spiderSprite;
                        spriteRenderer.flipY = isUpsideDown;
                        UpdateCubeIconVisibility(false);
                        break;
                    case PlayerMode.Ball:
                        spriteRenderer.sprite = ballSprite;
                        spriteRenderer.flipY = false;
                        UpdateCubeIconVisibility(false);
                        
                        // Устанавливаем цвет в зависимости от нахождения в зоне гравитации
                        if (isInGravityZone)
                        {
                            spriteRenderer.color = gravityZoneColor;
                        }
                        else
                        {
                            spriteRenderer.color = originalBallColor;
                        }
                        break;
                }
            }
        }
        
        // Обновляем спрайт иконки куба
        UpdateCubeIconSprite();
    }

    private void UpdateCubeIconVisibility(bool showIcon)
    {
        if (!hasCubeIconBeenInitialized || cubeIconRenderer == null) return;
        
        cubeIconRenderer.enabled = showIcon;
        
        if (showIcon)
        {
            // Настраиваем иконку куба
            cubeIconRenderer.transform.localPosition = cubeIconOffset;
            cubeIconRenderer.transform.localScale = Vector3.one * cubeIconScale;
            cubeIconRenderer.transform.rotation = Quaternion.identity;
        }
    }

    private void UpdateCubeIconSprite()
    {
        if (cubeIconRenderer == null || !cubeIconRenderer.enabled) return;
        
        // Получаем спрайт куба из SkinManager или используем стандартный
        Sprite cubeIconSprite = null;
        
        if (SkinManager.Instance != null)
        {
            cubeIconSprite = SkinManager.Instance.GetCubeSkin();
        }
        
        if (cubeIconSprite == null)
        {
            cubeIconSprite = cubeSprite;
        }
        
        if (cubeIconSprite != null)
        {
            cubeIconRenderer.sprite = cubeIconSprite;
        }
    }

    public void ChangeMode(PlayerMode newMode)
    {
        // Очищаем трейлы при смене режима
        if ((currentMode == PlayerMode.Wave || currentMode == PlayerMode.Ball || currentMode == PlayerMode.Spider) && 
            newMode != PlayerMode.Wave && newMode != PlayerMode.Ball && newMode != PlayerMode.Spider)
        {
            ClearTrails();
            transform.rotation = Quaternion.identity;
            transform.localScale = originalScale;
            isHoldingWave = false;
            
            // Возвращаем нормальный цвет, если был шар в зоне гравитации
            if (currentMode == PlayerMode.Ball && spriteRenderer != null)
            {
                spriteRenderer.color = originalBallColor;
            }
        }
        
        // Сбрасываем отдельный спрайт куба при смене с режима куба
        if (currentMode == PlayerMode.Cube && useSeparateCubeSprite && cubeModeSpriteRenderer != null)
        {
            cubeModeSpriteRenderer.transform.localRotation = Quaternion.identity;
            currentCubeSpriteRotation = Quaternion.identity;
            targetCubeSpriteRotation = Quaternion.identity;
            currentCubeRotationStep = 0;
            isCubeSpriteRotating = false;
            cubeSpriteRotationProgress = 0f;
        }
        
        // Если переключаемся на волну, сбрасываем вращение и масштаб
        if (newMode == PlayerMode.Wave)
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = originalScale;
        }
        
        if (currentMode == PlayerMode.Cube)
        {
            isJumping = false;
            cubeJumpRotation = 0f;
        }
        
        if (currentMode == PlayerMode.Ship)
        {
            isFlying = false;
        }
        
        if (currentMode == PlayerMode.Ball)
        {
            isBallBouncing = false;
            ballRotationAngle = 0f;
            wasBallGrounded = false;
            canFlipBallGravity = true;
            ballFlipTimer = 0f;
            isInGravityZone = false;
            
            // Возвращаем нормальный цвет
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalBallColor;
            }
        }
        
        currentMode = newMode;
        UpdatePhysics();
        UpdateVisuals();
    }

private void OnDestroy()
{
    ClearTrails();
    foreach (GameObject marker in practiceMarkers)
        if (marker != null) Destroy(marker);
    practiceMarkers.Clear();
}
    
    // Метод для визуализации точки респавна в редакторе
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (showRespawnPreview && IsEditor)
        {
            // Рисуем точку респавна
            Gizmos.color = respawnPreviewColor;
            Gizmos.DrawCube(editorRespawnPosition, Vector3.one * 0.5f);
            
            // Рисуем стрелку направления
            Gizmos.color = Color.green;
            Vector3 arrowEnd = editorRespawnPosition + Vector2.right * 1f;
            Gizmos.DrawLine(editorRespawnPosition, arrowEnd);
            
            // Рисуем надпись
            UnityEditor.Handles.Label(editorRespawnPosition + Vector2.up * 0.7f, "Respawn Point");
            
            // Если игрок существует, рисуем линию от игрока до точки респавна
            if (Application.isPlaying && transform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, editorRespawnPosition);
            }
        }
    }
    #endif
    
    // Метод для установки изначального режима извне (например, из GameManager)
    public void SetInitialMode(PlayerMode mode)
    {
        initialMode = mode;
        if (resetModeOnRespawn)
        {
            currentMode = initialMode;
            UpdatePhysics();
            UpdateVisuals();
        }
    }
    
    // Метод для получения текущего изначального режима
    public PlayerMode GetInitialMode()
    {
        return initialMode;
    }

    private void RespawnAtLevelStart()
{
    currentMode = initialMode;
    moveSpeed = initialMoveSpeed;
    UpdatePhysics();
    UpdateVisuals();
    transform.position = levelStartPosition;
    FinishRespawn();
}
private void RespawnAtPracticePoint()
{
    if (practicePoints.Count > 0)
    {
        PracticeStartPoint point = practicePoints[practicePoints.Count - 1];
        transform.position = point.position;
        currentMode = point.mode;
        moveSpeed = point.moveSpeed;
        UpdatePhysics();
        UpdateVisuals();
    }
    else
    {
        RespawnAtLevelStart();
        return;
    }
    FinishRespawn();
}

private void FinishRespawn()
{
    isDead = false;
    if (rb != null)
    {
        rb.isKinematic = false;
        rb.velocity = Vector2.zero;
    }
    if (spriteRenderer != null) spriteRenderer.enabled = true;
    UpdateVisuals();

    isHoldingWave = false;
    isJumping = false;
    isFlying = false;
    isBallBouncing = false;
    wasBallGrounded = false;
    isUpsideDown = false;
    canFlipBallGravity = true;
    ballFlipTimer = 0f;
    isInGravityZone = false;
    cubeJumpRotation = 0f;
    ballRotationAngle = 0f;
    waveHoldTimer = 0f;

    ResetModeChangers();
    ballGrounded = false;
    ballCoyoteTimer = 0f;

    transform.rotation = Quaternion.identity;
    transform.localScale = originalScale;

    if (useSeparateCubeSprite && cubeModeSpriteRenderer != null)
    {
        cubeModeSpriteRenderer.transform.localRotation = Quaternion.identity;
        currentCubeSpriteRotation = Quaternion.identity;
        currentCubeRotationStep = 0;
        isCubeSpriteRotating = false;
    }

    ClearTrails();
}

public void TogglePracticeMode()
{
    isPracticeMode = !isPracticeMode;
    if (practicePanel != null)
        practicePanel.SetActive(isPracticeMode);

    if (!isPracticeMode)
    {
        // Удаляем все визуальные маркеры
        foreach (GameObject marker in practiceMarkers)
        {
            if (marker != null) Destroy(marker);
        }
        practiceMarkers.Clear();
        practicePoints.Clear();
    }

    Debug.Log($"Practice Mode: {(isPracticeMode ? "ON" : "OFF")}");
}

public void SetPracticePoint()
{
    if (!isPracticeMode) return;
    
    PracticeStartPoint p = new PracticeStartPoint
    {
        position = transform.position,
        mode = currentMode,
        moveSpeed = moveSpeed
    };
    practicePoints.Add(p);

    // Создаём маркер
    if (practicePointMarkerPrefab != null)
    {
        GameObject marker = Instantiate(practicePointMarkerPrefab, p.position, Quaternion.identity);
        practiceMarkers.Add(marker);
    }

    Debug.Log($"Practice point set at {p.position}, mode={p.mode}");
}

public void RemoveLastPracticePoint()
{
    if (practicePoints.Count > 0)
    {
        practicePoints.RemoveAt(practicePoints.Count - 1);
        
        // Удаляем последний маркер
        if (practiceMarkers.Count > 0)
        {
            Destroy(practiceMarkers[practiceMarkers.Count - 1]);
            practiceMarkers.RemoveAt(practiceMarkers.Count - 1);
        }
        
        Debug.Log("Last practice point removed");
    }
}

//очистка залупы новая
/// <summary>
/// Полная очистка всех точек практики и маркеров
/// </summary>
public void ClearAllPracticePoints()
{
    foreach (GameObject marker in practiceMarkers)
    {
        if (marker != null) Destroy(marker);
    }
    practiceMarkers.Clear();
    practicePoints.Clear();
}

/// <summary>
/// Принудительная смерть с удалением всех чекпойнтов практики
/// </summary>
public void KillPlayerAndClearPractice()
{
    if (isDead) return;
    ClearAllPracticePoints();
    Die();
}

/// <summary>
/// Воспроизведение музыки респавна с позиции, соответствующей прогрессу уровня
/// </summary>
private void PlayRespawnMusicAtProgress()
{
    if (respawnAudioSource == null || respawnAudioSource.clip == null) return;

    float progress = 0f;
    if (levelFinish != null)
    {
        float finishX = levelFinish.position.x;
        if (finishX > levelStartPosition.x)
        {
            progress = Mathf.Clamp01((transform.position.x - levelStartPosition.x) / (finishX - levelStartPosition.x));
        }
    }

    respawnAudioSource.Stop();
    respawnAudioSource.time = respawnAudioSource.clip.length * progress;
    respawnAudioSource.Play();
}

}