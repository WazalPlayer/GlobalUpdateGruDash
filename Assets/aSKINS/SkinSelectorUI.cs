using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkinSelectorUI : MonoBehaviour
{
    [System.Serializable]
    public class SkinTab
    {
        public string tabName; // "Cube", "Ship", и т.д.
        public SkinManager.SkinCollection skinCollection; // Ссылка на коллекцию скинов.
        public Transform buttonsContainer; // Контейнер для кнопок.
        public GameObject buttonPrefab; // Префаб кнопки.
        public Image previewImage; // Превью для этого режима.
    }

    [Header("Вкладки скинов")]
    public SkinTab cubeTab;
    public SkinTab shipTab;
    public SkinTab waveTab;
    public SkinTab ufoTab;
    public SkinTab spiderTab;
    public SkinTab ballTab;

    private Dictionary<string, SkinTab> tabs;

    private void Start()
    {
        tabs = new Dictionary<string, SkinTab>
        {
            { "Cube", cubeTab },
            { "Ship", shipTab },
            { "Wave", waveTab },
            { "UFO", ufoTab },
            { "Spider", spiderTab },
            { "Ball", ballTab }
        };

        InitializeAllTabs();
    }

    private void InitializeAllTabs()
    {
        foreach (var tab in tabs)
        {
            InitializeTab(tab.Key, tab.Value);
        }
    }

    private void InitializeTab(string tabName, SkinTab tab)
    {
        if (tab.skinCollection == null || tab.buttonsContainer == null) return;

        // Очищаем контейнер
        foreach (Transform child in tab.buttonsContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем кнопки для каждого скина в коллекции
        for (int i = 0; i < tab.skinCollection.sprites.Count; i++)
        {
            int index = i;
            GameObject buttonObj = Instantiate(tab.buttonPrefab, tab.buttonsContainer);
            Button button = buttonObj.GetComponent<Button>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            if (buttonImage != null && tab.skinCollection.sprites[i] != null)
            {
                buttonImage.sprite = tab.skinCollection.sprites[i];
            }

            // При нажатии на кнопку выбираем этот скин и обновляем превью
            button.onClick.AddListener(() => 
            {
                SelectSkin(tabName, index);
                UpdatePreview(tabName, tab);
            });

            // Можно также добавить подсветку выбранной кнопки, но для простоты пока без этого.
        }

        // Загружаем выбранный скин и обновляем превью
        UpdatePreview(tabName, tab);
    }

    private void SelectSkin(string skinType, int skinIndex)
    {
        if (SkinManager.Instance != null)
        {
            SkinManager.Instance.SelectSkin(skinType, skinIndex);
        }
    }

    private void UpdatePreview(string skinType, SkinTab tab)
    {
        if (tab.previewImage == null || SkinManager.Instance == null) return;

        Sprite previewSprite = null;
        switch (skinType)
        {
            case "Cube":
                previewSprite = SkinManager.Instance.GetCubeSkin();
                break;
            case "Ship":
                previewSprite = SkinManager.Instance.GetShipSkin();
                break;
            case "Wave":
                previewSprite = SkinManager.Instance.GetWaveSkin();
                break;
            case "UFO":
                previewSprite = SkinManager.Instance.GetUFOSkin();
                break;
            case "Spider":
                previewSprite = SkinManager.Instance.GetSpiderSkin();
                break;
            case "Ball":
                previewSprite = SkinManager.Instance.GetBallSkin();
                break;
        }

        if (previewSprite != null)
        {
            tab.previewImage.sprite = previewSprite;
        }
    }
}