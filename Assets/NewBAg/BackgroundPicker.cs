using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BackgroundPicker : MonoBehaviour
{
    public static BackgroundPicker Instance { get; private set; }

    [Header("UI References")]
    public GameObject pickerPanel;
    public Transform backgroundsContainer;
    public GameObject backgroundButtonPrefab;
    public Image previewBackgroundImage;

    [Header("Backgrounds")]
    public List<Sprite> backgroundSprites;

    private int selectedBackgroundIndex = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Не вызываем DontDestroyOnLoad, объект будет уничтожен при загрузке новой сцены
            LoadSelectedBackground();
            CreateBackgroundButtons();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdatePreview();
        if (pickerPanel != null) pickerPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LoadSelectedBackground()
    {
        selectedBackgroundIndex = AccountsManager.Instance != null ?
            AccountsManager.Instance.GetPlayerBackground() :
            PlayerPrefs.GetInt("PlayerBackground", 0);
    }

    private void CreateBackgroundButtons()
    {
        if (backgroundsContainer == null || backgroundButtonPrefab == null) return;

        foreach (Transform child in backgroundsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < backgroundSprites.Count; i++)
        {
            int index = i;
            GameObject buttonObj = Instantiate(backgroundButtonPrefab, backgroundsContainer);
            Image btnImage = buttonObj.GetComponent<Image>();
            if (btnImage != null && backgroundSprites[i] != null)
                btnImage.sprite = backgroundSprites[i];

            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => SelectBackground(index));
        }
    }

    public void SelectBackground(int index)
    {
        if (index < 0 || index >= backgroundSprites.Count) return;
        selectedBackgroundIndex = index;
        SaveSelectedBackground();
        UpdatePreview();

        if (AccountsManager.Instance != null)
            AccountsManager.Instance.SetPlayerBackground(selectedBackgroundIndex);

        if (pickerPanel != null) pickerPanel.SetActive(false);
    }

    private void SaveSelectedBackground()
    {
        PlayerPrefs.SetInt("PlayerBackground", selectedBackgroundIndex);
        PlayerPrefs.Save();
    }

    private void UpdatePreview()
    {
        if (previewBackgroundImage != null)
            previewBackgroundImage.sprite = GetSelectedBackgroundSprite();
    }

    public void ShowPanel()
    {
        if (pickerPanel != null)
        {
            UpdatePreview();
            pickerPanel.SetActive(true);
        }
    }

    public Sprite GetSelectedBackgroundSprite()
    {
        return GetBackgroundSprite(selectedBackgroundIndex);
    }

    public Sprite GetBackgroundSprite(int index)
    {
        if (index >= 0 && index < backgroundSprites.Count)
            return backgroundSprites[index];
        return null;
    }

    public int GetSelectedBackgroundIndex()
    {
        return selectedBackgroundIndex;
    }
}