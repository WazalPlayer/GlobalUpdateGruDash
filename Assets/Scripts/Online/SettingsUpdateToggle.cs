using UnityEngine;
using UnityEngine.UI;

// Строка в настройках: переключатель «Автообновление» и номер версии.
// Объекты создаёт редакторный инструмент Tools > Online > Add Update Settings (на выбранную панель).
public class SettingsUpdateToggle : MonoBehaviour
{
    public Toggle autoUpdateToggle;
    public Text versionText;
    public Text updateInfoText;

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

        if (UpdateChecker.Instance != null)
        {
            UpdateChecker.Instance.OnUpdateAvailable += ShowUpdate;
            UpdateChecker.Instance.OnError += ShowError;
            if (UpdateChecker.Instance.Latest != null)
                ShowUpdate(UpdateChecker.Instance.Latest);
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

    private void OnToggleChanged(bool value)
    {
        UpdateChecker.IsEnabled = value;
        if (value && UpdateChecker.Instance != null) UpdateChecker.Instance.Check();
    }

    private void ShowUpdate(UpdateChecker.UpdateManifest manifest)
    {
        SetInfo("Доступна версия " + manifest.version);
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
