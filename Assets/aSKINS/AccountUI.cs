using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AccountUI : MonoBehaviour
{
    [Header("Основные элементы")]
    public GameObject accountPanel;
    public InputField playerNameInput;
    public Text currentPlayerNameText;
    public Text playerLevelText;
    public Text experienceText;
    public Text starsText;
    public Text statsText;
    public Button saveAccountButton;
    public Button loadAccountButton;
    public Button syncAccountButton;
    public Button closeButton;

    [Header("Скины игрока")]
    public Text cubeSkinText;
    public Text shipSkinText;
    public Text waveSkinText;
    public Text ufoSkinText;
    public Text spiderSkinText;
    public Text ballSkinText;

    [Header("Поиск и топ")]
    public InputField searchInput;
    public Button searchButton;
    public Button topPlayersButton;
    public Button randomAccountButton;
    public Transform accountsListContainer;
    public GameObject accountItemPrefab;
    
    [Header("Топ игроков")]
    public Transform topPlayersContainer;
    public GameObject topPlayerItemPrefab;

    [Header("Background")]
    public Button selectBackgroundButton;
    public Image backgroundPreviewImage;

    [Header("Уведомления")]
    public Text notificationText;
    public Color successColor = Color.green;
    public Color errorColor = Color.red;
    public Color infoColor = Color.yellow;

    [Header("Детальный просмотр")]
    public GameObject detailAccountPanel; // сюда привязать панель DetailAccountPanel (префаб)

    private List<GameObject> accountItems = new List<GameObject>();
    private List<GameObject> topPlayerItems = new List<GameObject>();
    private bool isInitialized = false;

    void Start()
    {
        InitializeUI();
        
        if (SkinManager.Instance != null)
            SkinManager.Instance.OnSkinsUpdated += UpdateSkinsDisplay;

        if (AccountsManager.Instance != null)
        {
            AccountsManager.Instance.OnAccountOperationComplete += OnAccountOperationComplete;
            AccountsManager.Instance.OnAccountsLoaded += OnAccountsLoaded;
            AccountsManager.Instance.OnTopPlayersLoaded += OnTopPlayersLoaded;
        }

        LoadCurrentAccountData();
        isInitialized = true;
    }

    void OnEnable()
    {
        if (isInitialized) LoadCurrentAccountData();
    }

    private void InitializeUI()
    {
        if (saveAccountButton != null) saveAccountButton.onClick.AddListener(SaveAccount);
        if (loadAccountButton != null) loadAccountButton.onClick.AddListener(LoadAccount);
        if (syncAccountButton != null) syncAccountButton.onClick.AddListener(SyncAccount);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (searchButton != null) searchButton.onClick.AddListener(SearchAccounts);
        if (topPlayersButton != null) topPlayersButton.onClick.AddListener(ShowTopPlayers);
        if (randomAccountButton != null) randomAccountButton.onClick.AddListener(LoadRandomAccount);
        if (selectBackgroundButton != null) selectBackgroundButton.onClick.AddListener(OpenBackgroundPicker);
        
        if (playerNameInput != null)
            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        
        if (searchInput != null)
            searchInput.onEndEdit.AddListener((text) => {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    SearchAccounts();
            });
    }

    private void OpenBackgroundPicker()
    {
        BackgroundPicker.Instance?.ShowPanel();
    }

    private void LoadCurrentAccountData()
    {
        if (SkinManager.Instance == null) return;
        string playerName = SkinManager.Instance.GetPlayerName();
        if (currentPlayerNameText != null) currentPlayerNameText.text = $"Игрок: {playerName}";
        if (playerNameInput != null) playerNameInput.text = playerName;
        UpdateSkinsDisplay();
        UpdateBackgroundDisplay();
        UpdateStatsDisplay();
    }

    private void UpdateBackgroundDisplay()
    {
        if (backgroundPreviewImage == null) return;
        int bgIndex = AccountsManager.Instance.GetPlayerBackground();
        Sprite bgSprite = BackgroundPicker.Instance != null ? 
            BackgroundPicker.Instance.GetBackgroundSprite(bgIndex) : null;
        backgroundPreviewImage.sprite = bgSprite;
        backgroundPreviewImage.enabled = bgSprite != null;
    }

    private void UpdateSkinsDisplay()
    {
        if (SkinManager.Instance == null) return;
        if (cubeSkinText != null) cubeSkinText.text = $"Куб: {SkinManager.Instance.GetCubeSkinName()}";
        if (shipSkinText != null) shipSkinText.text = $"Корабль: {SkinManager.Instance.GetShipSkinName()}";
        if (waveSkinText != null) waveSkinText.text = $"Волна: {SkinManager.Instance.GetWaveSkinName()}";
        if (ufoSkinText != null) ufoSkinText.text = $"НЛО: {SkinManager.Instance.GetUFOSkinName()}";
        if (spiderSkinText != null) spiderSkinText.text = $"Паук: {SkinManager.Instance.GetSpiderSkinName()}";
        if (ballSkinText != null) ballSkinText.text = $"Шар: {SkinManager.Instance.GetBallSkinName()}";
    }

    private void UpdateStatsDisplay()
    {
        if (AccountsManager.Instance == null) return;
        int stars = AccountsManager.Instance.GetPlayerStars();
        if (playerLevelText != null) playerLevelText.text = "Уровень: 1";
        if (experienceText != null) experienceText.text = "Опыт: 0";
        if (starsText != null) starsText.text = $"⭐ Звёзды: {stars}";
        if (statsText != null) statsText.text = "Уровней создано: 0\nЛайков получено: 0";
    }

    public void SaveAccount()
    {
        if (AccountsManager.Instance == null)
        {
            ShowNotification("AccountsManager не инициализирован", errorColor);
            return;
        }
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            SkinManager.Instance.SetPlayerName(playerNameInput.text);
        AccountsManager.Instance.SaveAccountData();
        ShowNotification("Сохранение аккаунта...", infoColor);
    }

    public void LoadAccount()
    {
        if (AccountsManager.Instance == null)
        {
            ShowNotification("AccountsManager не инициализирован", errorColor);
            return;
        }
        AccountsManager.Instance.LoadAccountData();
        ShowNotification("Загрузка аккаунта...", infoColor);
    }

    public void SyncAccount() => SaveAccount();

    public void SearchAccounts()
    {
        if (AccountsManager.Instance == null)
        {
            ShowNotification("AccountsManager не инициализирован", errorColor);
            return;
        }
        string searchQuery = searchInput != null ? searchInput.text : "";
        if (string.IsNullOrEmpty(searchQuery))
        {
            ShowNotification("Введите имя для поиска", infoColor);
            return;
        }
        AccountsManager.Instance.SearchAccountsByName(searchQuery);
        ShowNotification($"Поиск аккаунтов: {searchQuery}", infoColor);
    }

    public void ShowTopPlayers()
    {
        if (AccountsManager.Instance == null)
        {
            ShowNotification("AccountsManager не инициализирован", errorColor);
            return;
        }
        AccountsManager.Instance.GetTopPlayers(100);
        ShowNotification("Загрузка топ игроков...", infoColor);
    }

    public void LoadRandomAccount()
    {
        if (AccountsManager.Instance == null)
        {
            ShowNotification("AccountsManager не инициализирован", errorColor);
            return;
        }
        AccountsManager.Instance.GetRandomAccount((account) => {
            if (account != null)
            {
                UpdateAccountItemUI(account, true);
                ShowNotification($"Загружен аккаунт: {account.player_name}", successColor);
            }
            else
            {
                ShowNotification("Не удалось загрузить случайный аккаунт", errorColor);
            }
        });
    }

    private void OnAccountOperationComplete(bool success, string message)
    {
        if (success)
        {
            ShowNotification(message, successColor);
            LoadCurrentAccountData();
        }
        else
        {
            ShowNotification(message, errorColor);
        }
    }

    private void OnTopPlayersLoaded(List<AccountData> players)
    {
        ClearTopPlayersList();
        if (players == null || players.Count == 0)
        {
            ShowNotification("Топ игроков не найден", infoColor);
            return;
        }
        int rank = 1;
        foreach (var player in players)
        {
            CreateTopPlayerItem(player, rank);
            rank++;
        }
        ShowNotification($"Загружено топ игроков: {players.Count}", successColor);
    }

    private void OnAccountsLoaded(List<AccountData> accounts)
    {
        ClearAccountsList();
        if (accounts == null || accounts.Count == 0)
        {
            ShowNotification("Аккаунты не найдены", infoColor);
            return;
        }
        foreach (var account in accounts)
            CreateAccountItem(account);
        ShowNotification($"Найдено аккаунтов: {accounts.Count}", successColor);
    }

    private void CreateAccountItem(AccountData account)
    {
        if (accountsListContainer == null || accountItemPrefab == null) return;
        GameObject itemObj = Instantiate(accountItemPrefab, accountsListContainer);
        AccountItemUI itemUI = itemObj.GetComponent<AccountItemUI>();
        if (itemUI != null) itemUI.Initialize(account, this);
        accountItems.Add(itemObj);
    }

    private void CreateTopPlayerItem(AccountData account, int rank)
    {
        if (topPlayersContainer == null || topPlayerItemPrefab == null) return;
        GameObject itemObj = Instantiate(topPlayerItemPrefab, topPlayersContainer);
        TopPlayerItemUI itemUI = itemObj.GetComponent<TopPlayerItemUI>();
        if (itemUI != null) itemUI.Initialize(account, rank, this);
        topPlayerItems.Add(itemObj);
    }

    private void ClearAccountsList()
    {
        foreach (var item in accountItems) if (item != null) Destroy(item);
        accountItems.Clear();
    }

    private void ClearTopPlayersList()
    {
        foreach (var item in topPlayerItems) if (item != null) Destroy(item);
        topPlayerItems.Clear();
    }

    public void UpdateAccountItemUI(AccountData account, bool updateCurrent = false)
    {
        if (updateCurrent)
        {
            if (currentPlayerNameText != null) currentPlayerNameText.text = $"Игрок: {account.player_name}";
            if (playerLevelText != null) playerLevelText.text = $"Уровень: {account.player_level}";
            if (experienceText != null) experienceText.text = $"Опыт: {account.experience}";
            if (starsText != null) starsText.text = $"⭐ Звёзды: {account.stars}";
            if (statsText != null) statsText.text = $"Уровней создано: {account.total_levels_created}\nЛайков получено: {account.total_likes_received}";
        }
    }

    public void LoadSkinsFromAccount(AccountData account)
    {
        if (SkinManager.Instance == null) return;
        SkinManager.Instance.LoadSkinsFromAccount(account);
        ShowNotification($"Скины загружены от {account.player_name}", successColor);
    }

public void ShowDetailPanel(AccountData account)
{
    if (detailAccountPanel != null)
    {
        // Пытаемся получить компонент DetailAccountPanel
        DetailAccountPanel detailScript = detailAccountPanel.GetComponent<DetailAccountPanel>();
        if (detailScript != null)
        {
            detailScript.Show(account);
        }
        else
        {
            Debug.LogError("На панели detailAccountPanel отсутствует компонент DetailAccountPanel!");
        }
    }
    else
    {
        Debug.LogWarning("detailAccountPanel не назначен в AccountUI");
    }
}

    private void OnPlayerNameChanged(string newName)
    {
        if (!string.IsNullOrEmpty(newName) && SkinManager.Instance != null)
        {
            SkinManager.Instance.SetPlayerName(newName);
            if (currentPlayerNameText != null) currentPlayerNameText.text = $"Игрок: {newName}";
        }
    }

    public void TogglePanel()
    {
        if (accountPanel != null)
        {
            accountPanel.SetActive(!accountPanel.activeSelf);
            if (accountPanel.activeSelf) LoadCurrentAccountData();
        }
    }

    public void ShowPanel()
    {
        if (accountPanel != null)
        {
            accountPanel.SetActive(true);
            LoadCurrentAccountData();
        }
    }

    public void ClosePanel()
    {
        if (accountPanel != null) accountPanel.SetActive(false);
    }

    public void ShowNotification(string message, Color color)
    {
        Debug.Log($"🔔 {message}");
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.color = color;
            CancelInvoke(nameof(ClearNotification));
            Invoke(nameof(ClearNotification), 3f);
        }
    }

    private void ClearNotification()
    {
        if (notificationText != null) notificationText.text = "";
    }

    void OnDestroy()
    {
        if (SkinManager.Instance != null)
            SkinManager.Instance.OnSkinsUpdated -= UpdateSkinsDisplay;
        if (AccountsManager.Instance != null)
        {
            AccountsManager.Instance.OnAccountOperationComplete -= OnAccountOperationComplete;
            AccountsManager.Instance.OnAccountsLoaded -= OnAccountsLoaded;
            AccountsManager.Instance.OnTopPlayersLoaded -= OnTopPlayersLoaded;
        }
    }
}