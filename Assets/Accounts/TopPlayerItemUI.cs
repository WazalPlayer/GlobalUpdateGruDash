using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TopPlayerItemUI : MonoBehaviour
{
    [Header("Основная информация")]
    public Text rankText;
    public Text playerNameText;
    public Text starsText;
    public Text levelText;
    public Text demonsText;
    public Text creatorsText;

    [Header("Иконки скинов")]
    public Image cubeSkinIcon;
    public Image shipSkinIcon;
    public Image waveSkinIcon;
    public Image ufoSkinIcon;
    public Image spiderSkinIcon;
    public Image backgroundImage;
    public Image ballSkinIcon;
    
    [Header("Статус администратора")]
    public Image adminStatusImage;
    
    [Header("Кнопки")]
    public Button loadSkinsButton;
    public Button detailButton;
    
    [Header("Элементы оформления")]
    public Image background;
    public Color firstPlaceColor = new Color(1f, 0.92f, 0.016f, 0.9f);
    public Color secondPlaceColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
    public Color thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f, 0.9f);
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Header("Медали")]
    public Image[] medalIcons; // размер 3

    private AccountData accountData;
    private AccountUI accountUI;
    private int currentRank;

    public void Initialize(AccountData data, int rank, AccountUI ui)
    {
        accountData = data;
        currentRank = rank;
        accountUI = ui;

        UpdateUI();

        if (backgroundImage != null)
        {
            Sprite bgSprite = BackgroundPicker.Instance != null ? 
                BackgroundPicker.Instance.GetBackgroundSprite(accountData.background) : null;
            backgroundImage.sprite = bgSprite;
            backgroundImage.enabled = bgSprite != null;
        }

        if (loadSkinsButton != null)
            loadSkinsButton.onClick.AddListener(OnLoadSkinsClick);
        
        if (detailButton != null)
            detailButton.onClick.AddListener(OnDetailClick);
    }

    private void UpdateUI()
    {
        if (accountData == null) return;

        if (rankText != null) rankText.text = $"#{currentRank}";
        if (playerNameText != null) playerNameText.text = accountData.player_name;
        if (starsText != null) starsText.text = $"⭐ {accountData.stars}";
        if (demonsText != null) demonsText.text = $"{accountData.demons}";
        if (creatorsText != null) creatorsText.text = $" {accountData.creators}";
        if (levelText != null) levelText.text = $"Ур. {accountData.player_level}";
        
        UpdateSkinIcons();
        UpdateAdminStatus();
        UpdateBackgroundColor();
        UpdateMedals(accountData.medals);
    }

    private void UpdateBackgroundColor()
    {
        if (background != null)
        {
            switch (currentRank)
            {
                case 1: background.color = firstPlaceColor; break;
                case 2: background.color = secondPlaceColor; break;
                case 3: background.color = thirdPlaceColor; break;
                default: background.color = normalColor; break;
            }
        }
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

    private void OnLoadSkinsClick()
    {
        if (accountUI != null && accountData != null)
            accountUI.LoadSkinsFromAccount(accountData);
    }

    private void OnDetailClick()
    {
        if (accountUI != null && accountData != null)
            accountUI.ShowDetailPanel(accountData);
    }

    public AccountData GetAccountData() => accountData;
}