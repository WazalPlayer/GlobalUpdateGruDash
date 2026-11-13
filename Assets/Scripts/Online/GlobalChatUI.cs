using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Логика окна глобального чата. Сам интерфейс — обычные объекты на сцене (PartMenu),
// их создаёт редакторный инструмент Tools > Online > Build Chat UI. Всё можно двигать и менять в Unity.
public class GlobalChatUI : MonoBehaviour
{
    [Header("Ссылки на объекты сцены (заполняет Build Chat UI)")]
    public Button chatButton;
    public GameObject panel;
    public Text messagesText;
    public ScrollRect scroll;
    public InputField input;
    public Button sendButton;
    public Button closeButton;
    public Text statusText;

    private bool subscribed;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (chatButton != null) chatButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (sendButton != null) sendButton.onClick.AddListener(SendCurrent);
    }

    private void OnDestroy()
    {
        if (FirebaseChatService.Instance != null && subscribed)
        {
            FirebaseChatService.Instance.OnMessagesUpdated -= RenderMessages;
            FirebaseChatService.Instance.OnError -= ShowError;
        }
    }

    public void OpenPanel()
    {
        if (panel != null) panel.SetActive(true);
        SetStatus(FirebaseSettings.IsConfigured ? "" : "Чат не настроен: укажите ключи Firebase");

        if (FirebaseChatService.Instance == null) return;

        if (!subscribed)
        {
            FirebaseChatService.Instance.OnMessagesUpdated += RenderMessages;
            FirebaseChatService.Instance.OnError += ShowError;
            subscribed = true;
        }
        FirebaseChatService.Instance.StartPolling();
    }

    public void ClosePanel()
    {
        if (panel != null) panel.SetActive(false);
        if (FirebaseChatService.Instance != null)
            FirebaseChatService.Instance.StopPolling();
    }

    private void SendCurrent()
    {
        if (FirebaseChatService.Instance == null || input == null) return;
        string text = input.text;
        if (string.IsNullOrWhiteSpace(text)) return;
        FirebaseChatService.Instance.SendMessage(text);
        input.text = "";
        input.ActivateInputField();
    }

    private void RenderMessages(List<FirebaseChatService.ChatMessage> messages)
    {
        if (messagesText == null) return;

        StringBuilder sb = new StringBuilder();
        foreach (FirebaseChatService.ChatMessage m in messages)
        {
            sb.Append("<b>").Append(m.name).Append(":</b> ").Append(m.text).Append('\n');
        }
        messagesText.text = sb.ToString();
        SetStatus("");

        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    private void ShowError(string message)
    {
        SetStatus(message);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
