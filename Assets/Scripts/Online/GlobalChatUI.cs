using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Окно глобального чата. Создаётся автоматически в каждой сцене.
// Кнопка «Чат» копируется из существующей кнопки в ButtonsMassive, поэтому стиль совпадает.
public class GlobalChatUI : MonoBehaviour
{
    private static GlobalChatUI instance;

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

        Button close = CreateButton("Close", panel.transform, "Назад", new Color(0.3f, 0.75f, 0.2f));
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
        Text placeholder = CreateText("Placeholder", inputGo.transform, "Написать сообщение...", 30, TextAnchor.MiddleLeft);
        placeholder.color = new Color(0, 0, 0, 0.4f);
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = FirebaseSettings.ChatMaxLength;
        input.lineType = InputField.LineType.SingleLine;

        Button send = CreateButton("Send", panel.transform, "Отправить", new Color(0.3f, 0.75f, 0.2f));
        SetRect(send.GetComponent<RectTransform>(), 0.77f, 0.1f, 0.97f, 0.18f);
        send.onClick.AddListener(SendCurrent);

        statusText = CreateText("Status", panel.transform, "", 26, TextAnchor.MiddleCenter);
        SetRect(statusText.rectTransform, 0.03f, 0.02f, 0.97f, 0.09f);
        statusText.color = new Color(1f, 0.8f, 0.3f);

        panel.SetActive(false);
        canvasGo.SetActive(true);
    }

    // Добавляет кнопку «Чат» в ButtonsMassive текущей сцены (копия существующей кнопки)
    private void AddMenuButton()
    {
        GameObject container = GameObject.Find("ButtonsMassive");
        if (container == null) return;
        if (container.transform.Find("GlobalChatButton") != null) return;

        Button source = container.GetComponentInChildren<Button>(true);
        if (source == null) return;

        GameObject clone = Instantiate(source.gameObject, container.transform);
        clone.name = "GlobalChatButton";
        clone.SetActive(true);

        // Убираем старые скрипты и иконки исходной кнопки, оставляем фон и стиль
        foreach (MonoBehaviour mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is Button || mb is Graphic || mb is LayoutElement) continue;
            Destroy(mb);
        }
        foreach (Image img in clone.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject != clone) img.enabled = false;
        }

        Button button = clone.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OpenPanel);

        SetLabel(clone, "Чат");
    }

    private static void SetLabel(GameObject root, string text)
    {
        TMP_Text tmp = root.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = text; return; }

        Text legacy = root.GetComponentInChildren<Text>(true);
        if (legacy != null) { legacy.text = text; return; }
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

    // ---- помощники для создания UI ----

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
