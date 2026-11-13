using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Страницы уровней в маркете: по pageSize уровней за раз.
// Кнопки «Назад» / «Дальше» меняют страницу, а при прокрутке до конца
// списка подгружается следующая страница и добавляется в конец.
public class MarketplacePager : MonoBehaviour
{
    public MarketplaceUI marketUI;
    public ScrollRect levelsScroll;
    public Button prevButton;
    public Button nextButton;
    public Text pageText;

    [Tooltip("Сколько уровней на одной странице")]
    public int pageSize = 50;

    [Tooltip("Порог подгрузки: когда список прокручен почти до конца (0 = низ, 1 = верх)")]
    [Range(0f, 0.3f)] public float autoLoadThreshold = 0.05f;

    private int page = 0;          // текущая страница (для кнопок)
    private int loadedPages = 0;   // сколько страниц уже загружено (для автоподгрузки)
    private bool hasMore = true;
    private bool loading = false;

    private void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        LoadReplace(0);
    }

    private void Update()
    {
        // Автоподгрузка: прокрутили почти до конца — добавляем следующую страницу
        if (levelsScroll == null || loading || !hasMore) return;
        if (levelsScroll.verticalNormalizedPosition <= autoLoadThreshold && levelsScroll.content.rect.height > 0f)
        {
            LoadAppend();
        }
    }

    public void NextPage()
    {
        if (loading || !hasMore) return;
        LoadReplace(page + 1);
    }

    public void PrevPage()
    {
        if (loading || page <= 0) return;
        LoadReplace(page - 1);
    }

    // Заменить список страницей номер p (кнопки)
    private void LoadReplace(int p)
    {
        loading = true;
        SetButtons();
        SupabaseManager.Instance?.GetLevelsPage(p * pageSize, pageSize + 1, levels =>
        {
            loading = false;
            page = p;
            loadedPages = p + 1;
            hasMore = levels.Count > pageSize;
            if (hasMore) levels.RemoveRange(pageSize, levels.Count - pageSize);

            marketUI?.ShowLevelPage(levels, false);
            if (levelsScroll != null) levelsScroll.verticalNormalizedPosition = 1f;
            SetButtons();
        });
    }

    // Добавить следующую страницу в конец (прокрутка)
    private void LoadAppend()
    {
        loading = true;
        int p = loadedPages;
        SupabaseManager.Instance?.GetLevelsPage(p * pageSize, pageSize + 1, levels =>
        {
            loading = false;
            loadedPages = p + 1;
            hasMore = levels.Count > pageSize;
            if (hasMore) levels.RemoveRange(pageSize, levels.Count - pageSize);

            marketUI?.ShowLevelPage(levels, true);
            SetButtons();
        });
    }

    private void SetButtons()
    {
        if (prevButton != null) prevButton.interactable = !loading && page > 0;
        if (nextButton != null) nextButton.interactable = !loading && hasMore;
        if (pageText != null) pageText.text = $"Страница {page + 1}";
    }
}
