using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Окно глобального чата и кнопка «Чат». Всё создаётся кодом в каждой сцене,
// сцены и префабы не меняются. Кнопка добавляется в ButtonsMassive, если он есть.
public class GlobalChatUI : MonoBehaviour
{
    private static GlobalChatUI instance;
    private static Sprite bubbleSprite;

    private static readonly Color ButtonGreen = new Color(0.45f, 0.85f, 0.2f, 1f);
    private static readonly Color ButtonDarkGreen = new Color(0.25f, 0.6f, 0.1f, 1f);

    private GameObject panel;
    private Text messagesText;
    private ScrollRect scroll;
    private InputField input;
    private Text statusText;
    private bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("GlobalChatUI");
            Object.DontDestroyOnLoad(go);
            instance = go.AddComponent<GlobalChatUI>();
            instance.BuildPanel();
        }

        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        instance.AddMenuButton();
    }

    // ---------- Окно чата ----------

    private void BuildPanel()
    {
        GameObject canvasGo = new GameObject("GlobalChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000, 1000);
        scaler.matchWidthOrHeight = 0.5f;

        panel = CreateImage("Panel", canvasGo.transform, new Color(0.04f, 0.1f, 0.2f, 0.96f));
        Stretch(panel.GetComponent<RectTransform>(), 0.05f, 0.05f, 0.95f, 0.95f);

        Text title = CreateText("Title", panel.transform, "Глобальный чат", 46, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, 0f, 0.9f, 1f, 1f);

        Button close = CreateButton("Close", panel.transform, "Назад", ButtonGreen);
        SetRect(close.GetComponent<RectTransform>(), 0.03f, 0.92f, 0.3f, 0.99f);
        close.onClick.AddListener(ClosePanel);

        GameObject scrollGo = new GameObject("MessagesScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scrollGo.transform.SetParent(panel.transform, false);
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);
        SetRect(scrollGo.GetComponent<RectTransform>(), 0.03f, 0.2f, 0.97f, 0.9f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        content.transform.SetParent(scrollGo.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        messagesText = CreateText("Messages", content.transform, "", 30, TextAnchor.UpperLeft);
        messagesText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messagesText.verticalOverflow = VerticalWrapMode.Overflow;

        scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = scrollGo.GetComponent<RectTransform>();
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.elasticity = 0f;
        scroll.vertical = true;
        scroll.horizontal = false;

        GameObject inputGo = CreateImage("InputField", panel.transform, new Color(1, 1, 1, 0.9f));
        SetRect(inputGo.GetComponent<RectTransform>(), 0.03f, 0.1f, 0.75f, 0.18f);
        input = inputGo.AddComponent<InputField>();
        Text inputText = CreateText("Text", inputGo.transform, "", 30, TextAnchor.MiddleLeft);
        inputText.color = Color.black;
        Stretch(inputText.rectTransform, 0.02f, 0f, 0.98f, 1f);
        Text placeholder = CreateText("Placeholder", inputGo.transform, "Написать сообщение...", 30, TextAnchor.MiddleLeft);
        placeholder.color = new Color(0, 0, 0, 0.4f);
        Stretch(placeholder.rectTransform, 0.02f, 0f, 0.98f, 1f);
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = FirebaseSettings.ChatMaxLength;
        input.lineType = InputField.LineType.SingleLine;

        Button send = CreateButton("Send", panel.transform, "Отправить", ButtonGreen);
        SetRect(send.GetComponent<RectTransform>(), 0.77f, 0.1f, 0.97f, 0.18f);
        send.onClick.AddListener(SendCurrent);

        statusText = CreateText("Status", panel.transform, "", 26, TextAnchor.MiddleCenter);
        SetRect(statusText.rectTransform, 0.03f, 0.02f, 0.97f, 0.09f);
        statusText.color = new Color(1f, 0.8f, 0.3f);

        panel.SetActive(false);
    }

    public void OpenPanel()
    {
        panel.SetActive(true);
        statusText.text = FirebaseSettings.IsConfigured ? "" : "Чат не настроен: укажите ключи Firebase";

        if (!subscribed && FirebaseChatService.Instance != null)
        {
            FirebaseChatService.Instance.OnMessagesUpdated += RenderMessages;
            FirebaseChatService.Instance.OnError += ShowError;
            subscribed = true;
        }
        if (FirebaseChatService.Instance != null)
            FirebaseChatService.Instance.StartPolling();
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
        if (FirebaseChatService.Instance != null)
            FirebaseChatService.Instance.StopPolling();
    }

    private void SendCurrent()
    {
        if (FirebaseChatService.Instance == null) return;
        string text = input.text;
        if (string.IsNullOrWhiteSpace(text)) return;
        FirebaseChatService.Instance.SendMessage(text);
        input.text = "";
        input.ActivateInputField();
    }

    private void RenderMessages(List<FirebaseChatService.ChatMessage> messages)
    {
        StringBuilder sb = new StringBuilder();
        foreach (FirebaseChatService.ChatMessage m in messages)
        {
            sb.Append("<b>").Append(m.name).Append(":</b> ").Append(m.text).Append('\n');
        }
        messagesText.text = sb.ToString();
        statusText.text = "";
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0f;
    }

    private void ShowError(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // ---------- Кнопка «Чат» в меню ----------

    private void AddMenuButton()
    {
        GameObject container = GameObject.Find("ButtonsMassive");
        if (container == null) return;
        if (container.transform.Find("GlobalChatButton") != null) return;

        // Размер берём у соседней кнопки, чтобы сетка выглядела одинаково
        Vector2 size = new Vector2(150, 150);
        Button sibling = container.GetComponentInChildren<Button>(true);
        if (sibling != null)
        {
            Vector2 s = sibling.GetComponent<RectTransform>().rect.size;
            if (s.x > 1f && s.y > 1f) size = s;
        }

        GameObject btn = new GameObject("GlobalChatButton", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        btn.transform.SetParent(container.transform, false);
        btn.GetComponent<RectTransform>().sizeDelta = size;

        Image bg = btn.GetComponent<Image>();
        bg.color = ButtonGreen;

        Outline outline = btn.GetComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(4, -4);

        // Тёмная полоска снизу, как у остальных кнопок
        GameObject strip = CreateImage("BottomStrip", btn.transform, ButtonDarkGreen);
        SetRect(strip.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.2f);

        // Иконка — пузырь чата, создаётся кодом
        GameObject icon = CreateImage("Icon", btn.transform, Color.white);
        icon.GetComponent<Image>().sprite = GetBubbleSprite();
        icon.GetComponent<Image>().preserveAspect = true;
        SetRect(icon.GetComponent<RectTransform>(), 0.22f, 0.3f, 0.78f, 0.82f);

        Text label = CreateText("Label", btn.transform, "Чат", 36, TextAnchor.MiddleCenter);
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
        SetRect(label.rectTransform, 0f, 0f, 1f, 0.25f);

        Button button = btn.GetComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(OpenPanel);
    }

    // Пузырь чата: белая заливка, тёмный контур, хвостик слева внизу. Рисуется в текстуре при первом вызове.
    private static Sprite GetBubbleSprite()
    {
        if (bubbleSprite != null) return bubbleSprite;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(64, 74);
        Vector2 radius = new Vector2(50, 36);
        Vector2 t0 = new Vector2(38, 100), t1 = new Vector2(70, 100), t2 = new Vector2(40, 122);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                bool inside = InEllipse(p, center, radius) || InTriangle(p, t0, t1, t2);
                bool border = inside && (!InEllipse(p, center, radius - new Vector2(5, 5)) && !InTriangle(p, t0 + new Vector2(4, -4), t1 + new Vector2(-4, -4), t2 + new Vector2(0, 4)));
                Color c = !inside ? new Color(0, 0, 0, 0) : (border ? new Color(0.05f, 0.2f, 0.4f, 1f) : new Color(1f, 1f, 1f, 1f));
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        bubbleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return bubbleSprite;
    }

    private static bool InEllipse(Vector2 p, Vector2 c, Vector2 r)
    {
        float dx = (p.x - c.x) / r.x, dy = (p.y - c.y) / r.y;
        return dx * dx + dy * dy <= 1f;
    }

    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
        bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    // ---------- Помощники для UI ----------

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
        Text t = CreateText("Label", go.transform, label, 30, TextAnchor.MiddleCenter);
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
