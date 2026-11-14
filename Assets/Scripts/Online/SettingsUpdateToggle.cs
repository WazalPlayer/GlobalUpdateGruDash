using UnityEngine;
using UnityEngine.UI;

// Блок «Обновления» в настройках: переключатель автопроверки, номер версии,
// кнопка «Проверить» и кнопка «Скачать», когда есть новая версия.
// Объекты создаёт Tools > Online > Add Update Settings to Selected Panel.
public class SettingsUpdateToggle : MonoBehaviour
{
    public Toggle autoUpdateToggle;
    public Text versionText;
    public Text updateInfoText;
    public Button checkButton;
    public Button downloadButton;

    private string downloadUrl;

    private void OnEnable()
    {
        if (autoUpdateToggle != null)
        {
            autoUpdateToggle.onValueChanged.RemoveListener(OnToggleChanged);
            autoUpdateToggle.isOn = UpdateChecker.IsEnabled;
            autoUpdateToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (versionText != null)
            versionText.text = "Версия: " + UpdateChecker.CurrentVersion;

        if (checkButton != null)
        {
            checkButton.onClick.RemoveListener(OnCheckClicked);
            checkButton.onClick.AddListener(OnCheckClicked);
        }

        if (downloadButton != null)
        {
            downloadButton.onClick.RemoveListener(OnDownloadClicked);
            downloadButton.onClick.AddListener(OnDownloadClicked);
            downloadButton.gameObject.SetActive(false);
        }

        if (UpdateChecker.Instance != null)
        {
            UpdateChecker.Instance.OnUpdateAvailable += ShowUpdate;
            UpdateChecker.Instance.OnError += ShowError;
            if (UpdateChecker.Instance.Latest != null)
                ShowUpdate(UpdateChecker.Instance.Latest);
            else
                SetInfo(UpdateChecker.IsEnabled ? "Проверяем обновления..." : "Автопроверка выключена");
        }
    }

    private void OnDisable()
    {
        if (UpdateChecker.Instance != null)
        {
            UpdateChecker.Instance.OnUpdateAvailable -= ShowUpdate;
            UpdateChecker.Instance.OnError -= ShowError;
        }
    }

    // Переключатель: включает или выключает автопроверку при запуске
    private void OnToggleChanged(bool value)
    {
        UpdateChecker.IsEnabled = value;
        SetInfo(value ? "Автопроверка включена" : "Автопроверка выключена");
        if (value && UpdateChecker.Instance != null) UpdateChecker.Instance.Check();
    }

    private void OnCheckClicked()
    {
        if (UpdateChecker.Instance == null) { SetInfo("Сервис обновлений не запущен"); return; }
        SetInfo("Проверяем...");
        UpdateChecker.Instance.Check();
    }

    // Открывает страницу с новой версией (скачивание/установка делает пользователь)
    private void OnDownloadClicked()
    {
        if (!string.IsNullOrEmpty(downloadUrl)) Application.OpenURL(downloadUrl);
    }

    private void ShowUpdate(UpdateChecker.UpdateManifest manifest)
    {
        downloadUrl = manifest.url;
        bool hasUrl = !string.IsNullOrEmpty(manifest.url);
        if (downloadButton != null) downloadButton.gameObject.SetActive(hasUrl);
        string notes = string.IsNullOrEmpty(manifest.notes) ? "" : "\n" + manifest.notes;
        SetInfo("Доступна версия " + manifest.version + notes);
    }

    private void ShowError(string message)
    {
        SetInfo(message);
    }

    private void SetInfo(string message)
    {
        if (updateInfoText != null) updateInfoText.text = message;
    }
}
