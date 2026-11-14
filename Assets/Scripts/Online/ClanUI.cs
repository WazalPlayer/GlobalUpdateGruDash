using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Экран клана. Все объекты лежат на сцене (их создаёт Tools > Online > Build Clan UI), здесь только логика.
public class ClanUI : MonoBehaviour
{
    [Header("Открытие")]
    public Button clanButton;
    public GameObject panel;
    public Button closeButton;
    public Text statusText;

    [Header("Мой клан")]
    public GameObject myClanArea;
    public Text infoText;
    public Button leaveButton;
    public Button joinTypeButton;       // для лидера: открыто / по заявке
    public Text joinTypeLabel;
    public Transform membersContent;

    [Header("Нет клана")]
    public GameObject noClanArea;
    public InputField nameInput;
    public InputField tagInput;
    public Button createButton;
    public Button createJoinTypeButton;
    public Text createJoinTypeLabel;
    public Transform topContent;

    [Header("Чат клана")]
    public ScrollRect chatScroll;
    public Text chatText;
    public InputField chatInput;
    public Button chatSendButton;

    [Header("Профиль игрока")]
    public GameObject profileArea;
    public Text profileText;
    public Button profileCloseButton;

    [Header("Шаблон строки (выключен)")]
    public GameObject rowTemplate;      // текст и кнопка внутри; выключен на сцене

    private ClanService.ClanData myClan;
    private bool isLeader;
    private string createType = "open";
    private float chatTimer;
    private const float ChatPollSeconds = 3f;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (profileArea != null) profileArea.SetActive(false);
        if (rowTemplate != null) rowTemplate.SetActive(false);

        if (clanButton != null) clanButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (leaveButton != null) leaveButton.onClick.AddListener(Leave);
        if (joinTypeButton != null) joinTypeButton.onClick.AddListener(ToggleJoinTypeMine);
        if (createJoinTypeButton != null) createJoinTypeButton.onClick.AddListener(ToggleCreateType);
        if (createButton != null) createButton.onClick.AddListener(CreateClan);
        if (chatSendButton != null) chatSendButton.onClick.AddListener(SendChat);
        if (profileCloseButton != null) profileCloseButton.onClick.AddListener(() => profileArea.SetActive(false));
        RefreshCreateType();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || myClan == null) return;
        chatTimer -= Time.deltaTime;
        if (chatTimer <= 0f)
        {
            chatTimer = ChatPollSeconds;
            LoadChat();
        }
    }

    public void Open()
    {
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        panel.SetActive(false);
        if (profileArea != null) profileArea.SetActive(false);
    }

    // ---------- Загрузка ----------

    private void Refresh()
    {
        SetStatus("Загрузка...");
        if (ClanService.Instance == null) { SetStatus("Сервис кланов не запущен"); return; }

        ClanService.Instance.GetMyClan((clan, err) =>
        {
            if (err != null) { SetStatus("Нет связи: " + err); return; }
            myClan = clan;
            if (clan == null) ShowNoClan();
            else ShowMyClan(clan);
        });
    }

    private void ShowNoClan()
    {
        isLeader = false;
        myClan = null;
        if (myClanArea != null) myClanArea.SetActive(false);
        if (noClanArea != null) noClanArea.SetActive(true);
        LoadTop();
        SetStatus("Вы не состоите в клане");
    }

    private void ShowMyClan(ClanService.ClanData clan)
    {
        isLeader = clan.leader_id == SupabaseManager.Instance.PlayerId;
        if (noClanArea != null) noClanArea.SetActive(false);
        if (myClanArea != null) myClanArea.SetActive(true);

        if (infoText != null)
            infoText.text = $"[{clan.tag}] {clan.name}\nЗвёзды: {clan.stars}\nТип вступления: {TypeName(clan.join_type)}";
        if (joinTypeButton != null) joinTypeButton.gameObject.SetActive(isLeader);
        if (joinTypeLabel != null) joinTypeLabel.text = TypeName(clan.join_type);

        LoadMembers(clan);
        LoadChat();
        chatTimer = ChatPollSeconds;
        SetStatus("");
    }

    private void LoadMembers(ClanService.ClanData clan)
    {
        ClanService.Instance.GetMembers((long)clan.id, (members, err) =>
        {
            if (err != null) { SetStatus("Участники: " + err); return; }
            List<string> ids = new List<string>();
            foreach (var m in members) ids.Add(m.player_id);

            ClanService.Instance.GetAccounts(ids, (accounts, err2) =>
            {
                Dictionary<string, ClanService.AccountRow> byId = new Dictionary<string, ClanService.AccountRow>();
                if (accounts != null) foreach (var a in accounts) byId[a.player_id] = a;

                ClearContent(membersContent);
                foreach (var m in members)
                {
                    string name = byId.ContainsKey(m.player_id) ? byId[m.player_id].player_name : ShortId(m.player_id);
                    string roleText = m.role == "leader" ? " (лидер)" : "";
                    // Тег клана выводится рядом с ником
                    string line = $"[{clan.tag}] {name}{roleText}";
                    string playerId = m.player_id;
                    AddRow(membersContent, line, "Профиль", () => ShowProfile(playerId, byId));
                }
                SetStatus($"Участников: {members.Count}/{ClanService.MaxMembers}");
            });
        });
    }

    private void LoadTop()
    {
        ClanService.Instance.GetTopClans(50, (clans, err) =>
        {
            if (err != null) { SetStatus("Топ кланов: " + err); return; }
            ClearContent(topContent);
            foreach (var c in clans)
            {
                ClanService.ClanData clan = c;
                AddRow(topContent, $"{clan.stars} ★  [{clan.tag}] {clan.name}  ({TypeName(clan.join_type)})",
                    "Вступить", () => JoinClan(clan));
            }
        });
    }

    private void LoadChat()
    {
        if (myClan == null || ClanService.Instance == null) return;
        ClanService.Instance.GetClanMessages((long)myClan.id, 50, (messages, err) =>
        {
            if (err != null || messages == null) return;
            StringBuilder sb = new StringBuilder();
            foreach (var m in messages)
                sb.Append("<b>").Append(m.player_name).Append(":</b> ").Append(m.text).Append('\n');
            if (chatText != null) chatText.text = sb.ToString();
            if (chatScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                chatScroll.verticalNormalizedPosition = 0f;
            }
        });
    }

    // ---------- Действия ----------

    private void JoinClan(ClanService.ClanData clan)
    {
        ClanService.Instance.JoinClan((long)clan.id, (result, err) =>
        {
            if (err != null) { SetStatus("Ошибка: " + err); return; }
            switch (result)
            {
                case "joined": SetStatus("Вы вступили в клан"); Refresh(); break;
                case "requested": SetStatus("Заявка отправлена, ждите решения лидера"); break;
                case "full": SetStatus("В клане нет мест (50/50)"); break;
                case "already_in_clan": SetStatus("Вы уже состоите в клане"); break;
                default: SetStatus("Клан не найден"); break;
            }
        });
    }

    private void Leave()
    {
        ClanService.Instance.LeaveClan(err =>
        {
            if (err != null)
            {
                SetStatus(err == "leader_cannot_leave" ? "Лидер не может выйти из клана" : "Ошибка: " + err);
                return;
            }
            Refresh();
        });
    }

    private void ToggleJoinTypeMine()
    {
        if (myClan == null || !isLeader) return;
        string next = myClan.join_type == "open" ? "request" : "open";
        ClanService.Instance.SetJoinType((long)myClan.id, next, err =>
        {
            if (err != null) { SetStatus("Ошибка: " + err); return; }
            Refresh();
        });
    }

    private void ToggleCreateType()
    {
        createType = createType == "open" ? "request" : "open";
        RefreshCreateType();
    }

    private void RefreshCreateType()
    {
        if (createJoinTypeLabel != null) createJoinTypeLabel.text = TypeName(createType);
    }

    private void CreateClan()
    {
        string name = nameInput != null ? nameInput.text.Trim() : "";
        string tag = tagInput != null ? tagInput.text.Trim() : "";
        if (name.Length < 2 || tag.Length < 2 || tag.Length > 5)
        {
            SetStatus("Название от 2 символов, тег от 2 до 5");
            return;
        }
        ClanService.Instance.CreateClan(name, tag, createType, (id, err) =>
        {
            if (err != null)
            {
                SetStatus(err.Contains("duplicate") ? "Такое название или тег уже занято" : "Ошибка: " + err);
                return;
            }
            SetStatus("Клан создан");
            Refresh();
        });
    }

    private void SendChat()
    {
        if (myClan == null || chatInput == null) return;
        string text = chatInput.text;
        if (string.IsNullOrWhiteSpace(text)) return;
        ClanService.Instance.SendClanMessage((long)myClan.id, text, err =>
        {
            if (err != null) { SetStatus("Чат: " + err); return; }
            chatInput.text = "";
            LoadChat();
        });
    }

    private void ShowProfile(string playerId, Dictionary<string, ClanService.AccountRow> byId)
    {
        if (profileArea == null) return;
        profileArea.SetActive(true);
        if (!byId.ContainsKey(playerId))
        {
            if (profileText != null) profileText.text = "Профиль не найден";
            return;
        }
        var a = byId[playerId];
        if (profileText != null)
            profileText.text = $"{a.player_name}\nЗвёзды: {a.stars}\nДемоны: {a.demons}\nС нами с: {a.created_at}";
    }

    // ---------- Помощники ----------

    private void AddRow(Transform content, string label, string buttonLabel, UnityEngine.Events.UnityAction onClick)
    {
        if (content == null || rowTemplate == null) return;
        GameObject row = Instantiate(rowTemplate, content);
        row.name = "Row";
        row.SetActive(true);
        Text t = row.GetComponentInChildren<Text>(true);
        if (t != null) t.text = label;
        Button b = row.GetComponentInChildren<Button>(true);
        if (b != null)
        {
            Text bt = b.GetComponentInChildren<Text>(true);
            if (bt != null) bt.text = buttonLabel;
            b.onClick.AddListener(onClick);
        }
    }

    private static void ClearContent(Transform content)
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject child = content.GetChild(i).gameObject;
            if (child.activeSelf && child.name.StartsWith("Row")) Destroy(child);
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private static string TypeName(string joinType) => joinType == "request" ? "по заявке" : "открыто";

    private static string ShortId(string id) => string.IsNullOrEmpty(id) ? "?" : id.Substring(0, System.Math.Min(6, id.Length));
}
