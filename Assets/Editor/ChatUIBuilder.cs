using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Создаёт интерфейс чата как обычные объекты в открытой сцене (PartMenu),
// чтобы их можно было настраивать в инспекторе. Запуск: Tools > Online > Build Chat UI
public static class ChatUIBuilder
{
    private const string IconPath = "Assets/UI/Chat/chat_icon.png";

    [MenuItem("Tools/Online/Build Chat UI in Open Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Чат", "Откройте сцену PartMenu.", "OK");
            return;
        }

        Sprite icon = PrepareIcon();

        // 1. Кнопка в ButtonsMassive
        Transform buttonsMassive = FindInScene(scene, "ButtonsMassive");
        if (buttonsMassive == null)
        {
            EditorUtility.DisplayDialog("Чат", "В сцене нет объекта ButtonsMassive.", "OK");
            return;
        }

        Vector2 size = new Vector2(150, 150);
        Button sibling = buttonsMassive.GetComponentInChildren<Button>(true);
        if (sibling != null)
        {
            Vector2 s = sibling.GetComponent<RectTransform>().rect.size;
            if (s.x > 1f && s.y > 1f) size = s;
        }

        Transform existingBtn = buttonsMassive.Find("ChatButton");
        if (existingBtn != null) Object.DestroyImmediate(existingBtn.gameObject);

        GameObject chatButtonGo = new GameObject("ChatButton", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(chatButtonGo, "Build Chat Button");
        chatButtonGo.transform.SetParent(buttonsMassive, false);
        chatButtonGo.GetComponent<RectTransform>().sizeDelta = size;
        Image chatImage = chatButtonGo.GetComponent<Image>();
        chatImage.sprite = icon;
        chatImage.preserveAspect = true;
        chatImage.color = Color.white;
        Button chatButton = chatButtonGo.GetComponent<Button>();
        chatButton.targetGraphic = chatImage;

        // 2. Окно чата
        Transform oldCanvas = FindRoot(scene, "ChatCanvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

        GameObject canvasGo = new GameObject("ChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Chat Canvas");
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000, 1000);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateImage("ChatPanel", canvasGo.transform, new Color(0.04f, 0.1f, 0.2f, 0.96f));
        Stretch(panel.GetComponent<RectTransform>(), 0.05f, 0.05f, 0.95f, 0.95f);

        Text title = CreateText("ChatTitle", panel.transform, "Глобальный чат", 46, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, 0f, 0.9f, 1f, 1f);

        Button close = CreateButton("ChatCloseButton", panel.transform, "Назад", new Color(0.45f, 0.85f, 0.2f));
        SetRect(close.GetComponent<RectTransform>(), 0.03f, 0.92f, 0.3f, 0.99f);

        GameObject scrollGo = new GameObject("ChatMessagesScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scrollGo.transform.SetParent(panel.transform, false);
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);
        SetRect(scrollGo.GetComponent<RectTransform>(), 0.03f, 0.2f, 0.97f, 0.9f);

        GameObject content = new GameObject("ChatContent", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        content.transform.SetParent(scrollGo.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        Text messages = CreateText("ChatMessagesText", content.transform, "", 30, TextAnchor.UpperLeft);
        messages.horizontalOverflow = HorizontalWrapMode.Wrap;
        messages.verticalOverflow = VerticalWrapMode.Overflow;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = scrollGo.GetComponent<RectTransform>();
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.elasticity = 0f;
        scroll.vertical = true;
        scroll.horizontal = false;

        GameObject inputGo = CreateImage("ChatInput", panel.transform, new Color(1, 1, 1, 0.9f));
        SetRect(inputGo.GetComponent<RectTransform>(), 0.03f, 0.1f, 0.75f, 0.18f);
        InputField input = inputGo.AddComponent<InputField>();
        Text inputText = CreateText("ChatInputText", inputGo.transform, "", 30, TextAnchor.MiddleLeft);
        inputText.color = Color.black;
        Stretch(inputText.rectTransform, 0.02f, 0f, 0.98f, 1f);
        Text placeholder = CreateText("ChatInputPlaceholder", inputGo.transform, "Написать сообщение...", 30, TextAnchor.MiddleLeft);
        placeholder.color = new Color(0, 0, 0, 0.4f);
        Stretch(placeholder.rectTransform, 0.02f, 0f, 0.98f, 1f);
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = FirebaseSettings.ChatMaxLength;
        input.lineType = InputField.LineType.SingleLine;

        Button send = CreateButton("ChatSendButton", panel.transform, "Отправить", new Color(0.45f, 0.85f, 0.2f));
        SetRect(send.GetComponent<RectTransform>(), 0.77f, 0.1f, 0.97f, 0.18f);

        Text status = CreateText("ChatStatusText", panel.transform, "", 26, TextAnchor.MiddleCenter);
        SetRect(status.rectTransform, 0.03f, 0.02f, 0.97f, 0.09f);
        status.color = new Color(1f, 0.8f, 0.3f);

        panel.SetActive(false);

        // Логика: компонент на корне ChatCanvas, ссылки заполняем сразу
        GlobalChatUI ui = canvasGo.AddComponent<GlobalChatUI>();
        ui.chatButton = chatButton;
        ui.panel = panel;
        ui.messagesText = messages;
        ui.scroll = scroll;
        ui.input = input;
        ui.sendButton = send;
        ui.closeButton = close;
        ui.statusText = status;

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ChatUIBuilder] Кнопка чата и окно созданы в сцене " + scene.name + " и сохранены.");
    }

    // Иконка: Sprite с правильными настройками импорта
    private static Sprite PrepareIcon()
    {
        AssetDatabase.ImportAsset(IconPath);
        TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer != null)
        {
            if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 100)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
    }

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
        {
            if (root.name == name) return root.transform;
        }
        return null;
    }

    private static Transform FindRecursive(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform child in t)
        {
            Transform found = FindRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ---- помощники ----

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static Text CreateText(string name, Transform parent, string text, int size, TextAnchor anchor)
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

    private static Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject go = CreateImage(name, parent, color);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        Text t = CreateText(name + "Label", go.transform, label, 30, TextAnchor.MiddleCenter);
        Stretch(t.rectTransform, 0, 0, 1, 1);
        return b;
    }

    private static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
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
