using UnityEngine;
using UnityEngine.UI;

public class LevelGenerator : MonoBehaviour
{
    [Header("Префабы")]
    public GameObject[] obstaclePrefabs;
    public GameObject[] platformPrefabs;
    public GameObject coinPrefab;
    public GameObject finishPrefab;
    
    [Header("Настройки уровня")]
    public float levelLength = 100f;
    public float minObstacleDistance = 3f;
    public float maxObstacleDistance = 8f;
    public float coinSpawnChance = 0.3f;
    public int maxPlatformSequence = 3;
    
    [Header("Финиш")]
    public bool useExistingFinish = false;
    public Transform existingFinish;

    [Header("Триггер очистки практики")]
    public GameObject practiceClearPrefab;   // префаб с PracticeClearZone

    public float practiceClearTriggerDistance = 3f; // расстояние до финиша

    [Header("Триггер очистки практики")]
    public float practiceClearTriggerOffset = 3f; // расстояние до финиша, где будет триггер
    // (префаб больше не нужен, создаём на лету)
    
    [Header("UI Элементы")]
    public Text progressText;
    public Text bestScoreText;
    public Transform playerTransform;
    
    [Header("Прогресс бар")]
    public Slider progressBar;
    public Image progressFill;
    
    private float finishPosition;
    private string levelKey;
    private int currentLevel = 1;

    // Ссылки на игрока (найдётся автоматически)
    private PlayerController playerController;

    private void Start()
    {
        InitializeLevel();
        GenerateStructuredLevel();
        UpdateUI();
        InitializeProgressBar();

        // Находим PlayerController для музыки и очистки
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            // Передаём финиш в PlayerController для прогресс-музыки
            if (useExistingFinish && existingFinish != null)
            {
                playerController.levelFinish = existingFinish;
            }
            else
            {
                GameObject finishObj = GameObject.FindWithTag("Finish");
                if (finishObj != null)
                    playerController.levelFinish = finishObj.transform;
            }
        }
    }

    private void Update()
    {
        if (playerTransform != null && finishPosition > 0)
        {
            UpdateProgress();
            UpdateProgressBar();
            // За координатную проверку финиша больше не отвечаем – очистку делает триггер
        }
    }

    private void InitializeLevel()
    {
        levelKey = "BestLevelScore_" + currentLevel;
        finishPosition = 0;
    }

    private void GenerateStructuredLevel()
    {
        if (platformPrefabs == null || platformPrefabs.Length == 0)
        {
            Debug.LogError("Не назначены префабы платформ!");
            return;
        }

        float currentPos = 10f;
        int platformSequence = 0;
        
        while (currentPos < levelLength)
        {
            if (platformSequence < maxPlatformSequence && Random.value > 0.4f)
            {
                SpawnPlatform(currentPos);
                platformSequence++;
                currentPos += 4f;
            }
            else
            {
                if (obstaclePrefabs != null && obstaclePrefabs.Length > 0 && Random.value > 0.2f)
                {
                    SpawnObstacle(currentPos);
                }
                
                if (coinPrefab != null && Random.value < coinSpawnChance)
                {
                    SpawnCoin(currentPos + Random.Range(-2f, 2f));
                }
                
                platformSequence = 0;
                currentPos += Random.Range(minObstacleDistance, maxObstacleDistance);
            }
        }
        
        // ---------- Финиш ----------
        if (useExistingFinish && existingFinish != null)
        {
            finishPosition = existingFinish.position.x;
            Debug.Log("Используется существующий финиш на позиции: " + finishPosition);
        }
        else if (!useExistingFinish && finishPrefab != null)
        {
            finishPosition = currentPos + 5f;
            Instantiate(finishPrefab, new Vector3(finishPosition, 0, 0), Quaternion.identity);
            Debug.Log("Создан новый финиш на позиции: " + finishPosition);
        }
        else if (useExistingFinish && existingFinish == null)
        {
            Debug.LogError("Включен useExistingFinish, но не назначен existingFinish! Автоматически спавнится новый финиш.");
            finishPosition = currentPos + 5f;
            if (finishPrefab != null)
                Instantiate(finishPrefab, new Vector3(finishPosition, 0, 0), Quaternion.identity);
        }
        else
        {
            Debug.LogError("Не назначен префаб финиша!");
        }

        // Создаём триггер очистки практики перед финишем
        if (finishPosition > 0)
        {
            SpawnPracticeClearTrigger(finishPosition - practiceClearTriggerOffset);
        }
    }

    /// <summary>
    /// Создаёт невидимый триггер, который при входе игрока очищает все чекпоинты практики.
    /// </summary>
    private void SpawnPracticeClearTrigger(float xPosition)
    {
        // Уже существует? На всякий случай удалим старый (если вызывается повторно)
        Transform old = transform.Find("PracticeClearTrigger");
        if (old != null) Destroy(old.gameObject);

        GameObject triggerObj = new GameObject("PracticeClearTrigger");
        triggerObj.transform.parent = this.transform; // будет удаляться при перегенерации
        triggerObj.transform.position = new Vector3(xPosition, 0f, 0f);

        // Коллайдер-триггер
        BoxCollider2D col = triggerObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 20f); // широкий, чтобы поймать игрока на любой высоте

        // Прикрепляем внутренний скрипт-обработчик
        PracticeClearTriggerHandler handler = triggerObj.AddComponent<PracticeClearTriggerHandler>();
        handler.playerController = playerController;

        Debug.Log($"Practice clear trigger spawned at X = {xPosition}");
    }

    // ----- Внутренний класс-обработчик триггера -----
    private class PracticeClearTriggerHandler : MonoBehaviour
    {
        public PlayerController playerController;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && playerController != null)
            {
                playerController.ClearAllPracticePoints();
                Debug.Log("Practice points cleared by trigger");
                // Можно уничтожить триггер после срабатывания, чтобы не срабатывал повторно
                Destroy(gameObject);
            }
        }
    }

    // ----- Остальные методы без изменений (SpawnPlatform, SpawnObstacle, SpawnCoin, UpdateProgress, UpdateProgressBar, UI) -----
    private void SpawnPlatform(float xPos)
    {
        if (platformPrefabs.Length == 0) return;
        int platformIndex = Random.Range(0, platformPrefabs.Length);
        if (platformPrefabs[platformIndex] != null)
        {
            Instantiate(platformPrefabs[platformIndex], new Vector3(xPos, -4f, 0), Quaternion.identity, this.transform);
        }
    }

    private void SpawnObstacle(float xPos)
    {
        if (obstaclePrefabs.Length == 0) return;
        int obstacleIndex = Random.Range(0, obstaclePrefabs.Length);
        if (obstaclePrefabs[obstacleIndex] != null)
        {
            float yPos = Random.Range(-3f, 3f);
            GameObject obstacle = Instantiate(obstaclePrefabs[obstacleIndex], new Vector3(xPos, yPos, 0), Quaternion.identity, this.transform);
            obstacle.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
        }
    }

    private void SpawnCoin(float xPos)
    {
        float yPos = Random.Range(-3f, 3f);
        Instantiate(coinPrefab, new Vector3(xPos, yPos, 0), Quaternion.identity, this.transform);
    }

    private void UpdateProgress()
    {
        float playerX = playerTransform.position.x;
        float progress = Mathf.Clamp01(playerX / finishPosition) * 100f;
        progressText.text = $"Прогресс: {progress:F1}%";

        float bestProgress = PlayerPrefs.GetFloat(levelKey, 0);
        if (progress > bestProgress)
        {
            bestProgress = progress;
            PlayerPrefs.SetFloat(levelKey, bestProgress);
            PlayerPrefs.Save();
        }
        bestScoreText.text = $"Рекорд: {bestProgress:F1}%";
    }

    private void InitializeProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 100f;
            progressBar.value = 0f;
        }
    }

    private void UpdateProgressBar()
    {
        if (progressBar != null)
        {
            float playerX = playerTransform.position.x;
            float progress = Mathf.Clamp01(playerX / finishPosition) * 100f;
            progressBar.value = progress;
        }
    }

    public void SetLevel(int level)
    {
        currentLevel = level;
        InitializeLevel();
        InitializeProgressBar();
    }

    [ContextMenu("Сбросить рекорды")]
    public void ResetBestScores()
    {
        PlayerPrefs.DeleteKey(levelKey);
        UpdateUI();
        if (progressBar != null) progressBar.value = 0f;
    }

    private void UpdateUI()
    {
        if (progressText != null) progressText.text = "Прогресс: 0%";
        if (bestScoreText != null) bestScoreText.text = $"Рекорд: {PlayerPrefs.GetFloat(levelKey, 0):F1}%";
        if (progressBar != null) progressBar.value = 0f;
    }

    [ContextMenu("Обновить генерацию уровня")]
    public void RegenerateLevel()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        GenerateStructuredLevel();
        UpdateUI();
        InitializeProgressBar();
    }
}