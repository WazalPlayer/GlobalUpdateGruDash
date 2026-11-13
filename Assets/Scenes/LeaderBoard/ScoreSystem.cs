using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreSystem : MonoBehaviour
{
    public static ScoreSystem Instance;

    [System.Serializable]
    public class ScoreSettings
    {
        public int finishPoints = 50;
        public int collisionPoints = 10;
    }

    [Header("Настройки очков")]
    public ScoreSettings scoreSettings = new ScoreSettings();

    [Header("Сохранение")]
    [SerializeField] private string scorePrefKey = "PlayerScore";

    [Header("Интерфейс")]
    [SerializeField] private Text scoreText;
    [SerializeField] private string scoreFormat = "Очки: {0}";

    private int _currentScore;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            LoadScore();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindScoreText();
        UpdateScoreDisplay();
    }

    public void AddCollisionPoints()
    {
        _currentScore += scoreSettings.collisionPoints;
        SaveScore();
        UpdateScoreDisplay();
        Debug.Log($"Добавлено {scoreSettings.collisionPoints} очков. Всего: {_currentScore}");
    }

    public void AddFinishPoints()
    {
        _currentScore += scoreSettings.finishPoints;
        SaveScore();
        UpdateScoreDisplay();
        Debug.Log($"Финиш! Добавлено {scoreSettings.finishPoints} очков. Всего: {_currentScore}");
    }

    private void FindScoreText()
    {
        if (scoreText == null)
        {
            scoreText = GameObject.FindGameObjectWithTag("ScoreText")?.GetComponent<Text>();
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, _currentScore);
        }
    }

    private void SaveScore()
    {
        PlayerPrefs.SetInt(scorePrefKey, _currentScore);
        PlayerPrefs.Save();
    }

    private void LoadScore()
    {
        _currentScore = PlayerPrefs.GetInt(scorePrefKey, 0);
    }

    public int GetCurrentScore() => _currentScore;
}