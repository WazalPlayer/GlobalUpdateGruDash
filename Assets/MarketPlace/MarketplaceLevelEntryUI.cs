using UnityEngine;
using UnityEngine.UI;

public class MarketplaceLevelEntryUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text levelNameText;
    public Text authorText;
    public Text statsText;
    public Text difficultyText;
    public Text dateText;
    public Button downloadButton;
    public Button likeButton;
    public Button deleteButton;
    public Button detailButton;   // новая кнопка "Подробнее"
    public Image difficultyIndicator;
    
    [Header("Rating Icon")]
    public GameObject ratedIcon; // Иконка, которая показывается если уровень рейтнут
    
    private SupabaseLevelData levelData;
    private MarketplaceUI marketplace;
    
    public void Setup(SupabaseLevelData data, MarketplaceUI marketplaceUI)
    {
        levelData = data;
        marketplace = marketplaceUI;
        
        // Заполняем UI
        if (levelNameText != null) levelNameText.text = data.level_name;
        if (authorText != null) authorText.text = $"Автор: {data.author}";
        if (statsText != null) statsText.text = $"Загрузки: {data.downloads} Лайки: {data.likes}";
        if (difficultyText != null) difficultyText.text = GetDifficultyText(data.difficulty);
        if (dateText != null) dateText.text = GetFormattedDate(data.created_at);
        
    if (difficultyIndicator != null)
    {
        Sprite difficultySprite = marketplaceUI.GetDifficultySprite(data.difficulty);
        difficultyIndicator.sprite = difficultySprite;
        difficultyIndicator.color = Color.white;
    }
        
        // Показываем иконку рейтинга только если уровень рейтнут
        // Предполагаем, что в SupabaseLevelData есть поле IsRated
        if (ratedIcon != null)
        {
            ratedIcon.SetActive(data.is_rated); // или IsRated, смотря как поле называется
        }
        
        // Показываем удаление только для своих уровней
        string playerId = PlayerPrefs.GetString("PlayerID", "");
        bool isMyLevel = data.author_id == playerId;
        
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(isMyLevel);
        
        // Обработчики кнопок - ВАЖНО: удаляем старые обработчики перед добавлением новых
        if (downloadButton != null)
        {
            downloadButton.onClick.RemoveAllListeners();
            downloadButton.onClick.AddListener(OnDownloadClicked);
        }

        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(OnDetailClicked);
        }
        
        if (likeButton != null)
        {
            likeButton.onClick.RemoveAllListeners();
            likeButton.onClick.AddListener(OnLikeClicked);
            likeButton.interactable = !isMyLevel; // Нельзя лайкать свои уровни
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
        
        Debug.Log($"✅ Создан UI элемент для уровня: {data.level_name} (Рейтнут: {data.is_rated})");
    }
    
    private void OnDetailClicked()
{
    Debug.Log($"📋 Открытие детальной информации уровня: {levelData.level_name}");
    if (marketplace != null)
        marketplace.ShowLevelDetails(levelData);
    else
        Debug.LogError("❌ MarketplaceUI ссылка потеряна для детального просмотра");
}

    private void OnDownloadClicked()
    {
        Debug.Log($"🎮 Нажата кнопка загрузки для уровня: {levelData.level_name}");
        
        if (marketplace != null)
        {
            marketplace.LoadLevelIntoEditor(levelData);
        }
        else
        {
            Debug.LogError("❌ MarketplaceUI ссылка потеряна");
        }
    }
    
    private void OnLikeClicked()
    {
        Debug.Log($"⭐ Нажата кнопка лайка для уровня: {levelData.level_name}");
        
        if (marketplace != null && likeButton != null)
        {
            // Временно блокируем кнопку чтобы избежать множественных нажатий
            likeButton.interactable = false;
            
            marketplace.LikeLevel(levelData.level_id);
            
            // Визуально обновляем счетчик лайков
            levelData.likes++;
            if (statsText != null)
                statsText.text = $"Загрузки: {levelData.downloads} Лайки: {levelData.likes}";
        }
        else
        {
            Debug.LogError("❌ MarketplaceUI ссылка потеряна или кнопка лайка не найдена");
        }
    }
    
    private void OnDeleteClicked()
    {
        Debug.Log($"🗑️ Нажата кнопка удаления для уровня: {levelData.level_name}");
        
        // Подтверждение удаления
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.DeleteLevel(levelData.level_id, (success) => {
                if (success)
                {
                    Debug.Log("✅ Уровень удален из базы данных");
                    // Удаляем UI элемент
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogError("❌ Ошибка удаления уровня");
                }
            });
        }
    }
    
    private string GetDifficultyText(float difficulty)
    {
        if (difficulty <= 1.5f) return "Очень легко";
        if (difficulty <= 2.5f) return "Легко";
        if (difficulty <= 3.5f) return "Средне";
        if (difficulty <= 4.5f) return "Сложно";
        if (difficulty <= 5.5f) return "Эксперт";
        if (difficulty <= 6.5f) return "Изи Демон";
        if (difficulty <= 7.5f) return "Хард Демон";
        return "Экстрим Демон";
    }
    
    private Color GetDifficultyColor(float difficulty)
    {
        if (difficulty <= 1.5f) return Color.green;
        if (difficulty <= 2.5f) return Color.cyan;
        if (difficulty <= 3.5f) return Color.yellow;
        if (difficulty <= 4.5f) return new Color(1f, 0.5f, 0f); // Оранжевый
        return Color.red;
    }
    
    private string GetFormattedDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return "";
        
        try
        {
            // Пытаемся распарсить дату
            if (System.DateTime.TryParse(dateString, out System.DateTime date))
            {
                return date.ToString("dd.MM.yyyy");
            }
            else
            {
                // Если парсинг не удался, возвращаем первые 10 символов
                return dateString.Length > 10 ? dateString.Substring(0, 10) : dateString;
            }
        }
        catch
        {
            return dateString.Length > 10 ? dateString.Substring(0, 10) : dateString;
        }
    }
}