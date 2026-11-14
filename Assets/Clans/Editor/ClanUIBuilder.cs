using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Меню клана на сцене: кнопка в ButtonsMassive + панель с вкладками
// «Клан», «Участники», «Чат», «Топ кланов». Всё — обычные объекты, их можно двигать в Unity.
// Запуск: Tools > Clans > Build Clan Menu in Open Scene
public static class ClanUIBuilder
{
    private const string IconPath = "Assets/Clans/Art/clans_icon.png";
    private static readonly Color Green = new Color(0.45f, 0.85f, 0.2f, 1f);
    private static readonly Color DarkGreen = new Color(0.25f, 0.6f, 0.1f, 1f);
    private static readonly Color PanelBg = new Color(0.04f, 0.1f, 0.2f, 0.96f);
    private static readonly Color TabOff = new Color(0.12f, 0.25f, 0.4f, 1f);

    [MenuItem("Tools/Clans/Build Clan Menu in Open Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Transform buttonsMassive = FindInScene(scene, "ButtonsMassive");
        if (buttonsMassive == null)
        {
            EditorUtility.DisplayDialog("Клан", "В сцене нет объекта ButtonsMassive. Откройте PartMenu.", "OK");
            return;
        }

        Sprite icon = PrepareIcon();

        // Удаляем старые версии, чтобы не дублировать
        Transform oldBtn = buttonsMassive.Find("ClanButton");
        if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);
        Transform oldCanvas = FindRoot(scene, "ClanCanvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

        // 1. Кнопка клана в ButtonsMassive
        Vector2 size = new Vector2(150, 150);
        Button sibling = buttonsMassive.GetComponentInChildren<Button>(true);
        if (sibling != null)
        {
            Vector2 s = sibling.GetComponent<RectTransform>().rect.size;
            if (s.x > 1f && s.y > 1f) size = s;
        }
        GameObject clanBtnGo = new GameObject("ClanButton", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(clanBtnGo, "Build Clan Button");
        clanBtnGo.transform.SetParent(buttonsMassive, false);
        clanBtnGo.GetComponent<RectTransform>().sizeDelta = size;
        Image clanImg = clanBtnGo.GetComponent<Image>();
        clanImg.sprite = icon;
        clanImg.preserveAspect = true;
        clanImg.color = Color.white;
        Button clanButton = clanBtnGo.GetComponent<Button>();
        clanButton.targetGraphic = clanImg;

        // 2. Canvas и панель
        GameObject canvasGo = new GameObject("ClanCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Clan Canvas");
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000, 1000);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = Img("ClanPanel", canvasGo.transform, PanelBg);
        SetStretch(panel.GetComponent<RectTransform>(), 0.03f, 0.03f, 0.97f, 0.97f);
        panel.SetActive(false);

        Text title = Txt("ClanTitle", panel.transform, "Кланы", 46, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, 0.3f, 0.92f, 0.7f, 1f);

        Button close = Btn("ClanCloseButton", panel.transform, "Назад", Green);
        SetRect(close.GetComponent<RectTransform>(), 0.02f, 0.93f, 0.25f, 0.99f);

        Text status = Txt("ClanStatusText", panel.transform, "", 26, TextAnchor.MiddleCenter);
        status.color = new Color(1f, 0.8f, 0.3f);
        SetRect(status.rectTransform, 0.02f, 0.01f, 0.98f, 0.05f);

        // 3. Вкладки
        string[] tabNames = { "Клан", "Участники", "Чат", "Топ кланов" };
        GameObject tabBar = Img("TabBar", panel.transform, new Color(0, 0, 0, 0));
        SetRect(tabBar.GetComponent<RectTransform>(), 0.02f, 0.85f, 0.98f, 0.92f);
        Button[] tabs = new Button[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            tabs[i] = Btn("Tab_" + tabNames[i], tabBar.transform, tabNames[i], TabOff);
            float x0 = i / (float)tabNames.Length + 0.005f;
            float x1 = (i + 1) / (float)tabNames.Length - 0.005f;
            SetRect(tabs[i].GetComponent<RectTransform>(), x0, 0f, x1, 1f);
        }

        // Строка-шаблон (выключена): текст + кнопка
        GameObject rowTemplate = new GameObject("RowTemplate", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowTemplate.transform.SetParent(panel.transform, false);
        rowTemplate.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
        rowTemplate.GetComponent<LayoutElement>().preferredHeight = 80;
        HorizontalLayoutGroup hl = rowTemplate.GetComponent<HorizontalLayoutGroup>();
        hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = false;
        hl.padding = new RectOffset(10, 10, 6, 6); hl.spacing = 10;
        Text rowText = Txt("RowText", rowTemplate.transform, "Строка", 28, TextAnchor.MiddleLeft);
        rowText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        Button rowBtn = Btn("RowButton", rowTemplate.transform, "Действие", Green);
        rowBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
        rowTemplate.SetActive(false);

        const float pageX0 = 0.03f, pageY0 = 0.04f, pageX1 = 0.97f, pageY1 = 0.83f;

        // 4. Страница «Клан»: два состояния — есть клан / нет клана
        GameObject pageClan = Img("PageClan", panel.transform, new Color(0, 0, 0, 0));
        SetRect(pageClan.GetComponent<RectTransform>(), pageX0, pageY0, pageX1, pageY1);

        GameObject myArea = Img("MyClanArea", pageClan.transform, new Color(0, 0, 0, 0.2f));
        SetStretch(myArea.GetComponent<RectTransform>(), 0, 0, 1, 1);
        Text info = Txt("ClanInfoText", myArea.transform, "", 34, TextAnchor.UpperLeft);
        SetRect(info.rectTransform, 0.03f, 0.6f, 0.97f, 0.98f);
        Button joinType = Btn("JoinTypeButton", myArea.transform, "открыто", Green);
        SetRect(joinType.GetComponent<RectTransform>(), 0.03f, 0.42f, 0.5f, 0.56f);
        Button leave = Btn("LeaveButton", myArea.transform, "Выйти из клана", new Color(0.8f, 0.3f, 0.2f));
        SetRect(leave.GetComponent<RectTransform>(), 0.52f, 0.42f, 0.97f, 0.56f);
        Text joinHint = Txt("JoinHintText", myArea.transform, "Тип вступления меняет только лидер. Лимит клана: 50 участников.", 26, TextAnchor.UpperLeft);
        joinHint.color = new Color(0.8f, 0.9f, 1f);
        SetRect(joinHint.rectTransform, 0.03f, 0.25f, 0.97f, 0.4f);

        GameObject noArea = Img("NoClanArea", pageClan.transform, new Color(0, 0, 0, 0.2f));
        SetStretch(noArea.GetComponent<RectTransform>(), 0, 0, 1, 1);
        Text createHint = Txt("CreateHintText", noArea.transform, "Вы не в клане. Создание клана бесплатно.", 30, TextAnchor.UpperLeft);
        SetRect(createHint.rectTransform, 0.03f, 0.82f, 0.97f, 0.98f);
        InputField nameIn = Input("ClanNameInput", noArea.transform, "Название клана", 0.03f, 0.66f, 0.6f, 0.8f);
        InputField tagIn = Input("ClanTagInput", noArea.transform, "Тег (2-5)", 0.63f, 0.66f, 0.97f, 0.8f);
        Button createType = Btn("CreateJoinTypeButton", noArea.transform, "открыто", Green);
        SetRect(createType.GetComponent<RectTransform>(), 0.03f, 0.5f, 0.5f, 0.62f);
        Button create = Btn("CreateClanButton", noArea.transform, "Создать клан", Green);
        SetRect(create.GetComponent<RectTransform>(), 0.52f, 0.5f, 0.97f, 0.62f);

        // 5. Страница «Участники»
        GameObject pageMembers = Img("PageMembers", panel.transform, new Color(0, 0, 0, 0));
        SetRect(pageMembers.GetComponent<RectTransform>(), pageX0, pageY0, pageX1, pageY1);
        Transform membersContent = Scroll("MembersScroll", pageMembers.transform, 0f, 0f, 1f, 1f, "MembersContent");

        // 6. Страница «Чат»
        GameObject pageChat = Img("PageChat", panel.transform, new Color(0, 0, 0, 0));
        SetRect(pageChat.GetComponent<RectTransform>(), pageX0, pageY0, pageX1, pageY1);
        ScrollRect chatScroll = Scroll2("ClanChatScroll", pageChat.transform, 0f, 0.12f, 1f, 1f, out Transform chatContent);
        chatContent.name = "ClanChatContent";
        Text chatText = Txt("ClanChatText", chatContent, "", 28, TextAnchor.UpperLeft);
        chatText.horizontalOverflow = HorizontalWrapMode.Wrap;
        chatText.verticalOverflow = VerticalWrapMode.Overflow;
        InputField chatIn = Input("ClanChatInput", pageChat.transform, "Сообщение клану", 0f, 0f, 0.75f, 0.1f);
        Button chatSend = Btn("ClanChatSend", pageChat.transform, "Отправить", Green);
        SetRect(chatSend.GetComponent<RectTransform>(), 0.78f, 0f, 1f, 0.1f);

        // 7. Страница «Топ кланов»
        GameObject pageTop = Img("PageTop", panel.transform, new Color(0, 0, 0, 0));
        SetRect(pageTop.GetComponent<RectTransform>(), pageX0, pageY0, pageX1, pageY1);
        Transform topContent = Scroll("TopScroll", pageTop.transform, 0f, 0f, 1f, 1f, "TopContent");

        // 8. Профиль игрока (всплывающий поверх)
        GameObject profile = Img("ClanProfileArea", panel.transform, new Color(0.02f, 0.05f, 0.12f, 0.98f));
        SetRect(profile.GetComponent<RectTransform>(), 0.15f, 0.3f, 0.85f, 0.7f);
        Text profileText = Txt("ClanProfileText", profile.transform, "", 32, TextAnchor.UpperCenter);
        SetRect(profileText.rectTransform, 0.05f, 0.25f, 0.95f, 0.95f);
        Button profileClose = Btn("ClanProfileClose", profile.transform, "Закрыть", Green);
        SetRect(profileClose.GetComponent<RectTransform>(), 0.3f, 0.05f, 0.7f, 0.2f);
        profile.SetActive(false);

        // 9. Логика: ссылки
        ClanUI ui = canvasGo.AddComponent<ClanUI>();
        ui.clanButton = clanButton;
        ui.panel = panel;
        ui.closeButton = close;
        ui.statusText = status;
        ui.tabButtons = tabs;
        ui.tabPages = new GameObject[] { pageClan, pageMembers, pageChat, pageTop };
        ui.myClanArea = myArea;
        ui.infoText = info;
        ui.leaveButton = leave;
        ui.joinTypeButton = joinType;
        ui.joinTypeLabel = joinType.GetComponentInChildren<Text>();
        ui.membersContent = membersContent;
        ui.noClanArea = noArea;
        ui.nameInput = nameIn;
        ui.tagInput = tagIn;
        ui.createButton = create;
        ui.createJoinTypeButton = createType;
        ui.createJoinTypeLabel = createType.GetComponentInChildren<Text>();
        ui.topContent = topContent;
        ui.chatScroll = chatScroll;
        ui.chatText = chatText;
        ui.chatInput = chatIn;
        ui.chatSendButton = chatSend;
        ui.profileArea = profile;
        ui.profileText = profileText;
        ui.profileCloseButton = profileClose;
        ui.rowTemplate = rowTemplate;

        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ClanUIBuilder] Меню клана создано на сцене " + scene.name + " и сохранено.");
    }

    private static Sprite PrepareIcon()
    {
        AssetDatabase.ImportAsset(IconPath);
        TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
    }

    // ---- помощники ----

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform t = FindRecursive(root.transform, name);
            if (t != null) return t;
        }
        return null;
    }

    private static Transform FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root.transform;
        return null;
    }

    private static Transform FindRecursive(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            Transform f = FindRecursive(c, name);
            if (f != null) return f;
        }
        return null;
    }

    private static GameObject Img(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static Text Txt(string name, Transform parent, string text, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        return t;
    }

    private static Button Btn(string name, Transform parent, string label, Color color)
    {
        GameObject go = Img(name, parent, color);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        Text t = Txt(name + "Label", go.transform, label, 28, TextAnchor.MiddleCenter);
        SetStretch(t.rectTransform, 0, 0, 1, 1);
        return b;
    }

    private static InputField Input(string name, Transform parent, string placeholder, float x0, float y0, float x1, float y1)
    {
        GameObject go = Img(name, parent, new Color(1, 1, 1, 0.9f));
        SetRect(go.GetComponent<RectTransform>(), x0, y0, x1, y1);
        InputField input = go.AddComponent<InputField>();
        Text text = Txt(name + "Text", go.transform, "", 28, TextAnchor.MiddleLeft);
        text.color = Color.black;
        SetStretch(text.rectTransform, 0.02f, 0, 0.98f, 1);
        Text ph = Txt(name + "Placeholder", go.transform, placeholder, 28, TextAnchor.MiddleLeft);
        ph.color = new Color(0, 0, 0, 0.4f);
        SetStretch(ph.rectTransform, 0.02f, 0, 0.98f, 1);
        input.textComponent = text;
        input.placeholder = ph;
        return input;
    }

    // Прокручиваемый список; возвращает Content
    private static Transform Scroll(string name, Transform parent, float x0, float y0, float x1, float y1, string contentName)
    {
        ScrollRect sr = Scroll2(name, parent, x0, y0, x1, y1, out Transform content);
        content.name = contentName;
        return content;
    }

    private static ScrollRect Scroll2(string name, Transform parent, float x0, float y0, float x1, float y1, out Transform content)
    {
        GameObject sv = Img(name, parent, new Color(0, 0, 0, 0.35f));
        SetRect(sv.GetComponent<RectTransform>(), x0, y0, x1, y1);
        sv.AddComponent<Mask>().showMaskGraphic = true;
        ScrollRect sr = sv.AddComponent<ScrollRect>();
        sr.viewport = sv.GetComponent<RectTransform>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.elasticity = 0f;

        GameObject c = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        c.transform.SetParent(sv.transform, false);
        RectTransform cr = c.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1);
        cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(0.5f, 1);
        cr.sizeDelta = Vector2.zero;
        VerticalLayoutGroup vl = c.GetComponent<VerticalLayoutGroup>();
        vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
        vl.padding = new RectOffset(8, 8, 8, 8); vl.spacing = 6;
        c.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = cr;
        content = c.transform;
        return sr;
    }

    private static void SetStretch(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        SetRect(rt, x0, y0, x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
