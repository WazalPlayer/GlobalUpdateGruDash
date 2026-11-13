using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AccountItemUI : MonoBehaviour
{
    [Header("Основная информация")]
    public Text playerNameText;
    public Text levelText;
    public Text statsText;
    public Text experienceText;
    public Text starsText;
    public Text demonsText;
    public Text creatorsText;
    
    [Header("Иконки скинов")]
    public Image cubeSkinIcon;
    public Image shipSkinIcon;
    public Image waveSkinIcon;
    public Image ufoSkinIcon;
    public Image backgroundImage;
    public Image spiderSkinIcon;
    public Image ballSkinIcon;
    
    [Header("Статус администратора")]
    public Image adminStatusImage;
    
    [Header("Кнопки")]
    public Button loadSkinsButton;
    public Button viewDetailsButton;
    public Button copyNameButton;
    public Button detailButton;
    
    [Header("Элементы оформления")]
    public Image background;
    public GameObject selectedIndicator;
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
    public Color highlightColor = new Color(0.4f, 0.6f, 1f, 0.9f);
    
    [Header("Медали")]
    public Image[] medalIcons; // размер 3

    private AccountData accountData;
    private AccountUI accountUI;
    private bool isSelected = false;

    public void Initialize(AccountData data, AccountUI ui, bool selected = false)
    {
        accountData = data;
        accountUI = ui;
        isSelected = selected;

        UpdateUI();
        
        if (loadSkinsButton != null)
            loadSkinsButton.onClick.AddListener(OnLoadSkinsClick);
        if (viewDetailsButton != null)
            viewDetailsButton.onClick.AddListener(OnViewDetailsClick);
        if (copyNameButton != null)
            copyNameButton.onClick.AddListener(OnCopyNameClick);
        if (detailButton != null)
            detailButton.onClick.AddListener(OnDetailClick);
        
        UpdateSelectionState();
    }

    private void UpdateUI()
    {
        if (accountData == null) return;

        if (playerNameText != null) playerNameText.text = accountData.player_name;
        if (backgroundImage != null)
        {
            Sprite bgSprite = BackgroundPicker.Instance != null ? 
                BackgroundPicker.Instance.GetBackgroundSprite(accountData.background) : null;
            backgroundImage.sprite = bgSprite;
            backgroundImage.enabled = bgSprite != null;
        }
        if (levelText != null) levelText.text = $"Ур. {accountData.player_level}";
        if (experienceText != null) experienceText.text = $"{accountData.experience} XP";
        if (starsText != null) starsText.text = $"⭐ {accountData.stars}";
        if (demonsText != null) demonsText.text = $"{accountData.demons}";
        if (creatorsText != null) creatorsText.text = $" {accountData.creators}";
        if (statsText != null)
        {
            statsText.text = $@"Уровней: {accountData.total_levels_created}
Лайков: {accountData.total_likes_received}
Демонов: {accountData.demons}
Создан: {FormatDate(accountData.created_at)}";
        }

        UpdateSkinIcons();
        UpdateAdminStatus();
        UpdateMedals(accountData.medals);
    }

    private void UpdateAdminStatus()
    {
        if (adminStatusImage != null)
        {
            adminStatusImage.gameObject.SetActive(accountData.is_admin);
            if (accountData.is_admin)
                adminStatusImage.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    private void UpdateSkinIcons()
    {
        if (accountData == null) return;
        SetSkinIcon(cubeSkinIcon, "Cube", accountData.cube_skin);
        SetSkinIcon(shipSkinIcon, "Ship", accountData.ship_skin);
        SetSkinIcon(waveSkinIcon, "Wave", accountData.wave_skin);
        SetSkinIcon(ufoSkinIcon, "UFO", accountData.ufo_skin);
        SetSkinIcon(spiderSkinIcon, "Spider", accountData.spider_skin);
        SetSkinIcon(ballSkinIcon, "Ball", accountData.ball_skin);
    }

    private void SetSkinIcon(Image img, string skinType, int skinIndex)
    {
        if (img == null) return;
        Sprite sprite = GetSkinSprite(skinType, skinIndex);
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : Color.gray;
        img.gameObject.SetActive(sprite != null);
    }

    private Sprite GetSkinSprite(string skinType, int skinIndex)
    {
        if (SkinManager.Instance == null) return null;
        switch (skinType)
        {
            case "Cube":   return (SkinManager.Instance.cubeSkins.sprites.Count > skinIndex && skinIndex >= 0) ? SkinManager.Instance.cubeSkins.sprites[skinIndex] : null;
            case "Ship":   return (SkinManager.Instance.shipSkins.sprites.Count > skinIndex && skinIndex >= 0) ? SkinManager.Instance.shipSkins.sprites[skinIndex] : null;
            case "Wave":   return (SkinManager.Instance.waveSkins.sprites.Count > skinIndex && skinIndex >= 0) ? SkinManager.Instance.waveSkins.sprites[skinIndex] : null;
            case "UFO":    return (SkinManager.Instance.ufoSkins.sprites.Count > skinIndex && skinIndex >= 0) ? SkinManager.Instance.ufoSkins.sprites[skinIndex] : null;
            case "Spider": return (SkinManager.Instance.spiderSkins.sprites.Count > skinIndex && skinIndex >= 0) ? SkinManager.Instance.spiderSkins.sprites[skinIndex] : null;
            case "Ball":   return (SkinManager.Instance.ballSkins.sprites.Count > skinIndex && skinIndex >= 0) ? SkinManager.Instance.ballSkins.sprites[skinIndex] : null;
        }
        return null;
    }

    private void UpdateMedals(int medalsMask)
    {
        if (medalIcons == null) return;
        List<int> medals = new List<int>();
        for (int i = 0; i < 32 && medals.Count < medalIcons.Length; i++)
        {
            if ((medalsMask & (1 << i)) != 0)
                medals.Add(i);
        }
        for (int i = 0; i < medalIcons.Length; i++)
        {
            if (i < medals.Count && MedalsManager.Instance != null)
            {
                medalIcons[i].sprite = MedalsManager.Instance.GetMedalSprite(medals[i]);
                medalIcons[i].gameObject.SetActive(true);
            }
            else
            {
                medalIcons[i].gameObject.SetActive(false);
            }
        }
    }

    private string FormatDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return "неизвестно";
        try
        {
            System.DateTime date = System.DateTime.Parse(dateString);
            return date.ToString("dd.MM.yyyy");
        }
        catch
        {
            return dateString.Length > 10 ? dateString.Substring(0, 10) : dateString;
        }
    }

    private void UpdateSelectionState()
    {
        if (background != null) background.color = isSelected ? selectedColor : normalColor;
        if (selectedIndicator != null) selectedIndicator.SetActive(isSelected);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionState();
    }

    private void OnLoadSkinsClick()
    {
        if (accountUI != null && accountData != null)
        {
            accountUI.LoadSkinsFromAccount(accountData);
            SetSelected(true);
        }
    }

    private void OnViewDetailsClick()
    {
        if (accountUI != null && accountData != null)
            accountUI.UpdateAccountItemUI(accountData, true);
    }

    private void OnCopyNameClick()
    {
        if (accountData != null && !string.IsNullOrEmpty(accountData.player_name))
        {
            GUIUtility.systemCopyBuffer = accountData.player_name;
            accountUI?.ShowNotification($"Ник скопирован: {accountData.player_name}", Color.green);
            Debug.Log($"📋 Ник скопирован: {accountData.player_name}");
        }
    }

    private void OnDetailClick()
    {
        if (accountUI != null && accountData != null)
            accountUI.ShowDetailPanel(accountData);
    }

    public void OnPointerEnter()
    {
        if (background != null && !isSelected)
            background.color = highlightColor;
    }

    public void OnPointerExit()
    {
        if (background != null && !isSelected)
            background.color = normalColor;
    }

    public AccountData GetAccountData() => accountData;
}