using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Маркет: приоритетные уровни (рейтинговые и «Изи демон» и выше) грузятся сразу полностью.
// Остальные — страницами по pageSize. До autoLoadLimit уровней страницы догружаются в фоне
// по одной (с паузой, чтобы не лагало). Дальше — только при прокрутке до конца списка.
public class MarketplacePager : MonoBehaviour
{
    public MarketplaceUI marketUI;
    public ScrollRect levelsScroll;
    public Button prevButton;
    public Button nextButton;
    public Text pageText;

    [Tooltip("Уровней на одной странице")]
    public int pageSize = 50;

    [Tooltip("Сколько уровней догружать в фоне автоматически, дальше — только при прокрутке")]
    public int autoLoadLimit = 10000;

    [Tooltip("Пауза между фоновыми страницами, секунд")]
    public float prefetchDelay = 0.25f;

    [Tooltip("Порог подгрузки при прокрутке: 0 = низ списка")]
    [Range(0f, 0.3f)] public float scrollThreshold = 0.05f;

    private int page = 0;          // текущая страница для кнопок
    private int loadedPages = 0;   // сколько страниц основного списка уже загружено
    private bool hasMore = true;
    private bool loading = false;

    private void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        StartCoroutine(StartupRoutine());
    }

    private void Update()
    {
        if (levelsScroll == null || loading || !hasMore || loadedPages == 0) return;
        if (levelsScroll.verticalNormalizedPosition <= scrollThreshold)
            StartCoroutine(AppendRoutine());
    }

    private IEnumerator StartupRoutine()
    {
        SetButtons();

        // 1. Приоритетные уровни — все сразу
        List<SupabaseLevelData> priority = null;
        if (SupabaseManager.Instance != null)
            SupabaseManager.Instance.GetPriorityLevels(list => priority = list);
        else
            priority = new List<SupabaseLevelData>();
        while (priority == null) yield return null;
        marketUI?.ShowPriorityLevels(priority);

        // 2. Первая страница основного списка
        yield return ReplaceRoutine(0);

        // 3. Фоновая догрузка до autoLoadLimit
        while (hasMore && loadedPages * pageSize < autoLoadLimit)
        {
            yield return new WaitForSeconds(prefetchDelay);
            if (loading) continue;
            yield return AppendRoutine();
        }
    }

    public void NextPage()
    {
        if (loading || !hasMore) return;
        StartCoroutine(ReplaceRoutine(page + 1));
    }

    public void PrevPage()
    {
        if (loading || page <= 0) return;
        StartCoroutine(ReplaceRoutine(page - 1));
    }

    // Заменить список страницей p (кнопки)
    private IEnumerator ReplaceRoutine(int p)
    {
        loading = true;
        SetButtons();

        List<SupabaseLevelData> levels = null;
        if (SupabaseManager.Instance != null)
            SupabaseManager.Instance.GetLevelsPage(p * pageSize, pageSize + 1,
                list => levels = list, SupabaseManager.RegularFilter);
        else
            levels = new List<SupabaseLevelData>();
        while (levels == null) yield return null;

        hasMore = levels.Count > pageSize;
        if (hasMore) levels.RemoveRange(pageSize, levels.Count - pageSize);

        page = p;
        loadedPages = p + 1;
        marketUI?.ShowLevelPage(levels, false);
        if (levelsScroll != null) levelsScroll.verticalNormalizedPosition = 1f;

        loading = false;
        SetButtons();
    }

    // Добавить следующую страницу в конец
    private IEnumerator AppendRoutine()
    {
        if (loading || !hasMore) yield break;
        loading = true;
        SetButtons();

        int p = loadedPages;
        List<SupabaseLevelData> levels = null;
        if (SupabaseManager.Instance != null)
            SupabaseManager.Instance.GetLevelsPage(p * pageSize, pageSize + 1,
                list => levels = list, SupabaseManager.RegularFilter);
        else
            levels = new List<SupabaseLevelData>();
        while (levels == null) yield return null;

        hasMore = levels.Count > pageSize;
        if (hasMore) levels.RemoveRange(pageSize, levels.Count - pageSize);

        loadedPages = p + 1;
        marketUI?.ShowLevelPage(levels, true);

        loading = false;
        SetButtons();
    }

    private void SetButtons()
    {
        if (prevButton != null) prevButton.interactable = !loading && page > 0;
        if (nextButton != null) nextButton.interactable = !loading && hasMore;
        if (pageText != null) pageText.text = $"Страница {page + 1}";
    }
}
