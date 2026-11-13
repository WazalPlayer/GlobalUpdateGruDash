using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    
    [Header("UI Elements")]
    public Text scoreText;
    
    private int currentScore = 0;
    private const string SCORE_KEY = "PlayerScore";
    
    void Awake()
    {
        // Создаем синглтон
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Загружаем сохраненные очки
        LoadScore();
        UpdateScoreUI();
    }
    
    // Добавление очков
    public void AddScore(int points)
    {
        currentScore += points;
        SaveScore();
        UpdateScoreUI();
        
        Debug.Log($"Добавлено {points} очков. Всего: {currentScore}");
    }
    
    // Установка очков
    public void SetScore(int points)
    {
        currentScore = points;
        SaveScore();
        UpdateScoreUI();
    }
    
    // Получение текущих очков
    public int GetScore()
    {
        return currentScore;
    }
    
    // Сброс очков
    public void ResetScore()
    {
        currentScore = 0;
        SaveScore();
        UpdateScoreUI();
    }
    
    // Обновление UI
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Очки: {currentScore}";
        }
    }
    
    // Сохранение очков
    private void SaveScore()
    {
        PlayerPrefs.SetInt(SCORE_KEY, currentScore);
        PlayerPrefs.Save();
    }
    
    // Загрузка очков
    private void LoadScore()
    {
        currentScore = PlayerPrefs.GetInt(SCORE_KEY, 0);
    }
    
    // Очистка сохраненных данных (для отладки)
    [ContextMenu("Clear Saved Data")]
    public void ClearSavedData()
    {
        PlayerPrefs.DeleteKey(SCORE_KEY);
        PlayerPrefs.Save();
        ResetScore();
        Debug.Log("Сохраненные данные очищены!");
    }
}