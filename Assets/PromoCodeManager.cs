using UnityEngine;
using UnityEngine.UI;

public class PromoCodeManager : MonoBehaviour
{
    [Header("UI References")]
    public InputField codeInputField;
    public Text resultText;
    public Button redeemButton;
    public GameObject loadingIndicator;

    void Start()
    {
        if (redeemButton != null)
            redeemButton.onClick.AddListener(OnRedeemButtonClick);
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    public void OnRedeemButtonClick()
    {
        if (AccountsManager.Instance == null)
        {
            ShowMessage("AccountsManager не найден", Color.red);
            return;
        }

        string code = codeInputField.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowMessage("Введите промокод", Color.yellow);
            return;
        }

        SetUIInteractable(false);
        ShowMessage("Проверка...", Color.white);

        AccountsManager.Instance.RedeemCode(code, (success, message, stars) =>
        {
            SetUIInteractable(true);
            if (success)
            {
                ShowMessage($"Успех! Получено {stars} звёзд!", Color.green);
                codeInputField.text = "";
            }
            else
            {
                ShowMessage($"Ошибка: {message}", Color.red);
            }
        });
    }

    private void SetUIInteractable(bool interactable)
    {
        if (codeInputField != null) codeInputField.interactable = interactable;
        if (redeemButton != null) redeemButton.interactable = interactable;
        if (loadingIndicator != null) loadingIndicator.SetActive(!interactable);
    }

    private void ShowMessage(string text, Color color)
    {
        if (resultText != null)
        {
            resultText.text = text;
            resultText.color = color;
        }
    }
}