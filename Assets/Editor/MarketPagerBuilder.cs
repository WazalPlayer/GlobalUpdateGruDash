using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Добавляет строку страниц («Назад» / «Дальше» + подпись) в маркет.
// Два варианта:
//  - Tools > Market > Add Page Buttons to MarketPrefab — меняет сам префаб маркета,
//    поэтому кнопки появятся во всех сценах, где стоит маркет. Рекомендуется.
//  - Tools > Market > Add Page Buttons to Open Scene — только открытая сцена.
public static class MarketPagerBuilder
{
    private const string ArrowPath = "Assets/UI/Market/arrow.png";
    private const string MarketPrefabPath = "Assets/Scripts/MarketPrefab.prefab";
    private const int PageSize = 25;

    [MenuItem("Tools/Market/Add Page Buttons to MarketPrefab")]
    public static void BuildOnPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(MarketPrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Маркет", "Не найден префаб " + MarketPrefabPath, "OK");
            return;
        }

        try
        {
            if (!AddPager(root, "префаб " + MarketPrefabPath)) return;
            PrefabUtility.SaveAsPrefabAsset(root, MarketPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[MarketPagerBuilder] Кнопки страниц добавлены в префаб маркета. Они появятся во всех сценах с маркетом.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Market/Add Page Buttons to Open Scene")]
    public static void BuildOnScene()
    {
        MarketplaceUI marketUI = Object.FindObjectOfType<MarketplaceUI>(true);
        if (marketUI == null)
        {
            EditorUtility.DisplayDialog("Маркет", "В открытой сцене не найден MarketplaceUI. Откройте сцену с маркетом.", "OK");
            return;
        }

        if (!AddPager(marketUI.gameObject, "сцена " + marketUI.gameObject.scene.name)) return;
        EditorSceneManager.MarkSceneDirty(marketUI.gameObject.scene);
        EditorSceneManager.SaveScene(marketUI.gameObject.scene);
        Debug.Log("[MarketPagerBuilder] Кнопки страниц добавлены в сцену и сохранены.");
    }

    // Добавляет строку страниц внутрь root (префаб или объект сцены с MarketplaceUI)
    private static bool AddPager(GameObject root, string where)
    {
        MarketplaceUI marketUI = root.GetComponentInChildren<MarketplaceUI>(true);
        if (marketUI == null)
        {
            EditorUtility.DisplayDialog("Маркет", "Не найден MarketplaceUI: " + where, "OK");
            return false;
        }

        GameObject panel = marketUI.gameObject;
        Sprite arrow = PrepareArrow();

        Transform old = panel.transform.Find("PagerBar");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject bar = new GameObject("PagerBar", typeof(RectTransform));
        bar.transform.SetParent(panel.transform, false);
        SetRect(bar.GetComponent<RectTransform>(), 0.05f, 0.02f, 0.95f, 0.11f);

        // «Назад» — стрелка, отзеркаленная по горизонтали
        Button prev = CreateArrowButton("PrevPageButton", bar.transform, arrow, mirrored: true);
        SetRect(prev.GetComponent<RectTransform>(), 0f, 0f, 0.2f, 1f);

        Text pageText = CreateText("PageText", bar.transform, "Страница 1", 36);
        SetRect(pageText.rectTransform, 0.3f, 0f, 0.7f, 1f);

        Button next = CreateArrowButton("NextPageButton", bar.transform, arrow, mirrored: false);
        SetRect(next.GetComponent<RectTransform>(), 0.8f, 0f, 1f, 1f);

        MarketplacePager pager = panel.GetComponent<MarketplacePager>();
        if (pager == null) pager = panel.AddComponent<MarketplacePager>();
        pager.marketUI = marketUI;
        pager.prevButton = prev;
        pager.nextButton = next;
        pager.pageText = pageText;
        pager.pageSize = PageSize;
        pager.autoLoadLimit = 10000;
        pager.prefetchDelay = 0f;
        pager.levelsScroll = marketUI.levelsContainer != null
            ? marketUI.levelsContainer.GetComponentInParent<ScrollRect>(true)
            : null;

        EditorUtility.SetDirty(pager);
        return true;
    }

    private static Sprite PrepareArrow()
    {
        AssetDatabase.ImportAsset(ArrowPath);
        TextureImporter importer = AssetImporter.GetAtPath(ArrowPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
    }

    private static Button CreateArrowButton(string name, Transform parent, Sprite sprite, bool mirrored)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = Color.white;
        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;
        if (mirrored) go.GetComponent<RectTransform>().localScale = new Vector3(-1f, 1f, 1f);
        return b;
    }

    private static Text CreateText(string name, Transform parent, string text, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }

    private static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
