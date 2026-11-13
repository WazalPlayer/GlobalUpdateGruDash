using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DetailAccountPanel : MonoBehaviour
{
    [Header("Информация об игроке")]
    public Text playerNameText;
    public Text starsText;
    public Text demonsText;
    public Text creatorsText;
    public Text levelText;          // опционально
    public Text experienceText;     // опционально

    [Header("Скины")]
    public Image cubeSkinIcon;
    public Image shipSkinIcon;
    public Image waveSkinIcon;
    public Image ufoSkinIcon;
    public Image spiderSkinIcon;
    public Image ballSkinIcon;

    [Header("Медали")]
    public Transform medalsContainer;   // контейнер для всех медалей
    public GameObject medalPrefab;      // префаб с Image (можно просто Image)

    [Header("Кнопка закрытия")]
    public Button closeButton;

    private AccountData currentAccount;

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        
        // Панель изначально выключена
        gameObject.SetActive(false);
    }

    public void Show(AccountData account)
    {
        if (account == null)
        {
            Debug.LogWarning("DetailAccountPanel: попытка показать null-аккаунт");
            return;
        }

        currentAccount = account;
        UpdateUI();
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (currentAccount == null) return;

        // --- Основная информация ---
        if (playerNameText != null)
            playerNameText.text = currentAccount.player_name;

        if (starsText != null)
            starsText.text = $"Звёзды: {currentAccount.stars}";

        if (demonsText != null)
            demonsText.text = $"Демоны: {currentAccount.demons}";

        if (creatorsText != null)
            creatorsText.text = $"Креатор-поинты: {currentAccount.creators}";

        if (levelText != null)
            levelText.text = $"Уровень: {currentAccount.player_level}";

        if (experienceText != null)
            experienceText.text = $"Опыт: {currentAccount.experience}";

        // --- Скины ---
        SetSkinIcon(cubeSkinIcon, "Cube", currentAccount.cube_skin);
        SetSkinIcon(shipSkinIcon, "Ship", currentAccount.ship_skin);
        SetSkinIcon(waveSkinIcon, "Wave", currentAccount.wave_skin);
        SetSkinIcon(ufoSkinIcon, "UFO", currentAccount.ufo_skin);
        SetSkinIcon(spiderSkinIcon, "Spider", currentAccount.spider_skin);
        SetSkinIcon(ballSkinIcon, "Ball", currentAccount.ball_skin);

        // --- Медали (все, не только 3) ---
        UpdateMedals(currentAccount.medals);
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
        if (medalsContainer == null || medalPrefab == null) return;

        // Очищаем контейнер
        foreach (Transform child in medalsContainer)
            Destroy(child.gameObject);

        // Собираем все ID медалей (битовая маска)
        List<int> medalIds = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            if ((medalsMask & (1 << i)) != 0)
                medalIds.Add(i);
        }

        // Создаём иконки
        foreach (int medalId in medalIds)
        {
            GameObject medalObj = Instantiate(medalPrefab, medalsContainer);
            Image medalImage = medalObj.GetComponent<Image>();
            if (medalImage != null && MedalsManager.Instance != null)
            {
                medalImage.sprite = MedalsManager.Instance.GetMedalSprite(medalId);
                // можно добавить название в тултип, но без отдельного компонента — просто игнорируем
            }
        }
    }
}