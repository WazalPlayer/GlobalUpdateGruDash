using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Меню клана. Все объекты лежат на сцене (их создаёт Tools > Clans > Build Clan Menu in Open Scene).
// Вкладки: 0 Клан, 1 Участники, 2 Заявки, 3 Чат, 4 Топ кланов.
public class ClanUI : MonoBehaviour
{
    [Header("Открытие")]
    public Button clanButton;
    public GameObject panel;
    public Button closeButton;
    public Text statusText;

    [Header("Вкладки")]
    public Button[] tabButtons;
    public GameObject[] tabPages;

    [Header("Клан (вкладка 0)")]
    public GameObject myClanArea;
    public Text infoText;
    public Button leaveButton;
    public Button joinTypeButton;
    public Text joinTypeLabel;
    public GameObject noClanArea;
    public InputField nameInput;
    public InputField tagInput;
    public Button createButton;
    public Button createJoinTypeButton;
    public Text createJoinTypeLabel;

    [Header("Участники (вкладка 1)")]
    public Transform membersContent;

    [Header("Заявки (вкладка 2)")]
    public Transform requestsContent;
    public Text requestsHint;

    [Header("Чат клана (вкладка 3)")]
    public ScrollRect chatScroll;
    public Text chatText;
    public InputField chatInput;
    public Button chatSendButton;

    [Header("Топ кланов (вкладка 4)")]
    public Transform topContent;

    [Header("Предпросмотр клана")]
    public GameObject previewArea;
    public Text previewText;
    public Transform previewMembersContent;
    public Button previewJoinButton;
    public Button previewCloseButton;

    [Header("Профиль игрока")]
    public GameObject profileArea;
    public Text profileText;
    public Button profileCloseButton;

    [Header("Шаблон строки (выключен, 3 кнопки)")]
    public GameObject rowTemplate;

    private ClanService.ClanData myClan;
    private bool isLeader;
    private string createType = "open";
    private float chatTimer;
    private float disbandArmedUntil = -1f;
    private ClanService.ClanStatRow previewClanStat;
    private const float ChatPollSeconds = 3f;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (previewArea != null) previewArea.SetActive(false);
        if (profileArea != null) profileArea.SetActive(false);
        if (rowTemplate != null) rowTemplate.SetActive(false);

        if (clanButton != null) clanButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (leaveButton != null) leaveButton.onClick.AddListener(Leave);
        if (joinTypeButton != null) joinTypeButton.onClick.AddListener(ToggleJoinTypeMine);
        if (createJoinTypeButton != null) createJoinTypeButton.onClick.AddListener(ToggleCreateType);
        if (createButton != null) createButton.onClick.AddListener(CreateClan);
        if (chatSendButton != null) chatSendButton.onClick.AddListener(SendChat);
        if (previewCloseButton != null) previewCloseButton.onClick.AddListener(() => previewArea.SetActive(false));
        if (previewJoinButton != null) previewJoinButton.onClick.AddListener(PreviewJoin);
        if (profileCloseButton != null) profileCloseButton.onClick.AddListener(() => profileArea.SetActive(false));
        RefreshCreateType();

        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => SelectTab(index));
            }
        }
        SelectTab(0);
    }

    private void Update()
    {
        if (panel == null || !panel.activeInHierarchy || myClan == null) return;
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
        if (previewArea != null) previewArea.SetActive(false);
        if (profileArea != null) profileArea.SetActive(false);
    }

    // Показать одну вкладку и загрузить её данные
    public void SelectTab(int index)
    {
        if (tabPages != null)
            for (int i = 0; i < tabPages.Length; i++)
                if (tabPages[i] != null) tabPages[i].SetActive(i == index);

        if (panel == null || !panel.activeInHierarchy) return;
        if (index == 2) LoadRequests();
        if (index == 4) LoadTop();
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
        ClearContent(membersContent);
        ClearContent(requestsContent);
        if (requestsHint != null) requestsHint.text = "Вы не в клане";
        if (chatText != null) chatText.text = "Вступите в клан, чтобы писать в чат.";
        SetStatus("Вы не состоите в клане");
        SelectTab(GetActiveTab());
    }

    private void ShowMyClan(ClanService.ClanData clan)
    {
        isLeader = clan.leader_id == SupabaseManager.Instance.PlayerId;
        if (noClanArea != null) noClanArea.SetActive(false);
        if (myClanArea != null) myClanArea.SetActive(true);

        if (joinTypeButton != null) joinTypeButton.gameObject.SetActive(isLeader);
        if (joinTypeLabel != null) joinTypeLabel.text = TypeName(clan.join_type);
        if (leaveButton != null)
        {
            Text lbl = leaveButton.GetComponentInChildren<Text>(true);
            if (lbl != null) lbl.text = isLeader ? "Распустить клан" : "Выйти из клана";
        }

        LoadMembers(clan);
        LoadRequests();
        LoadChat();
        chatTimer = ChatPollSeconds;
        SetStatus("");
    }

    // Участники + ники + общая статистика клана (сумма звёзд участников)
    private void LoadMembers(ClanService.ClanData clan)
    {
        ClanService.Instance.GetMembers((long)clan.id, (members, err) =>
        {
            if (err != null) { SetStatus("Участники: " + err); return; }
            List<string> ids = new List<string>();
            foreach (var m in members) ids.Add(m.player_id);

            ClanService.Instance.GetAccounts(ids, (accounts, err2) =>
            {
                Dictionary<string, ClanService.AccountRow> byId = BuildIndex(accounts);
                double total = 0;
                foreach (var a in byId.Values) total += a.stars;

                if (infoText != null)
                    infoText.text = $"[{clan.tag}] {clan.name}\n" +
                                    $"Звёзды клана: {total}\n" +
                                    $"Участников: {members.Count}/{ClanService.MaxMembers}\n" +
                                    $"Вступление: {TypeName(clan.join_type)}";

                ClearContent(membersContent);
                foreach (var m in members)
                {
                    string name = byId.ContainsKey(m.player_id) ? byId[m.player_id].player_name : ShortId(m.player_id);
                    string role = m.role == "leader" ? " (лидер)" : "";
                    string playerId = m.player_id;
                    AddRow(membersContent, $"[{clan.tag}] {name}{role}",
                        new[] { ("Профиль", (UnityAction)(() => ShowProfile(playerId, byId))) });
                }
                SetStatus($"Участников: {members.Count}/{ClanService.MaxMembers}");
            });
        });
    }

    // Заявки: видит только лидер
    private void LoadRequests()
    {
        if (myClan == null) return;
        if (requestsHint != null) requestsHint.text = isLeader ? "Заявки на вступление" : "Заявки видит только лидер";
        ClearContent(requestsContent);
        if (!isLeader) return;

        ClanService.Instance.GetRequests((long)myClan.id, (requests, err) =>
        {
            if (err != null) { SetStatus("Заявки: " + err); return; }
            List<string> ids = new List<string>();
            foreach (var r in requests) ids.Add(r.player_id);

            ClanService.Instance.GetAccounts(ids, (accounts, err2) =>
            {
                Dictionary<string, ClanService.AccountRow> byId = BuildIndex(accounts);
                ClearContent(requestsContent);
                if (requestsHint != null) requestsHint.text = $"Заявок: {requests.Count}";
                foreach (var r in requests)
                {
                    string playerId = r.player_id;
                    string name = byId.ContainsKey(playerId) ? byId[playerId].player_name : ShortId(playerId);
                    string stars = byId.ContainsKey(playerId) ? $" · {byId[playerId].stars} ★" : "";
                    AddRow(requestsContent, name + stars, new[]
                    {
                        ("Профиль", (UnityAction)(() => ShowProfile(playerId, byId))),
                        ("Принять", (UnityAction)(() => Approve(playerId))),
                        ("Отклонить", (UnityAction)(() => Reject(playerId))),
                    });
                }
            });
        });
    }

    private void Approve(string playerId)
    {
        ClanService.Instance.ApproveRequest((long)myClan.id, playerId, (result, err) =>
        {
            if (err != null) { SetStatus("Ошибка: " + err); return; }
            SetStatus(result == "approved" ? "Заявка принята" : result == "full" ? "В клане нет мест" : "Игрок уже в клане");
            LoadRequests();
            LoadMembers(myClan);
        });
    }

    private void Reject(string playerId)
    {
        ClanService.Instance.RejectRequest((long)myClan.id, playerId, err =>
        {
            if (err != null) { SetStatus("Ошибка: " + err); return; }
            SetStatus("Заявка отклонена");
            LoadRequests();
        });
    }

    // Топ по звёздам (сумма участников) — из view clan_stats
    private void LoadTop()
    {
        if (ClanService.Instance == null) return;
        ClanService.Instance.GetTopClanStats(50, (clans, err) =>
        {
            if (err != null) { SetStatus("Топ кланов: " + err); return; }
            ClearContent(topContent);
            if (clans.Count == 0) SetStatus("Кланов пока нет");
            foreach (var c in clans)
            {
                ClanService.ClanStatRow stat = c;
                AddRow(topContent,
                    $"{stat.total_stars} ★  [{stat.tag}] {stat.name}  · {stat.members}/50 · {TypeName(stat.join_type)}",
                    new[]
                    {
                        ("Смотреть", (UnityAction)(() => ShowPreview(stat))),
                        ("Вступить", (UnityAction)(() => JoinById((long)stat.clan_id))),
                    });
            }
        });
    }

    // Предпросмотр клана: статистика и список участников
    private void ShowPreview(ClanService.ClanStatRow stat)
    {
        previewClanStat = stat;
        if (previewArea == null) return;
        previewArea.SetActive(true);
        if (previewText != null)
            previewText.text = $"[{stat.tag}] {stat.name}\nЗвёзды клана: {stat.total_stars}\n" +
                               $"Участников: {stat.members}/50\nВступление: {TypeName(stat.join_type)}";
        if (previewJoinButton != null) previewJoinButton.gameObject.SetActive(myClan == null);

        ClanService.Instance.GetMembers((long)stat.clan_id, (members, err) =>
        {
            if (err != null) return;
            List<string> ids = new List<string>();
            foreach (var m in members) ids.Add(m.player_id);
            ClanService.Instance.GetAccounts(ids, (accounts, err2) =>
            {
                Dictionary<string, ClanService.AccountRow> byId = BuildIndex(accounts);
                ClearContent(previewMembersContent);
                foreach (var m in members)
                {
                    string playerId = m.player_id;
                    string name = byId.ContainsKey(playerId) ? byId[playerId].player_name : ShortId(playerId);
                    AddRow(previewMembersContent, $"[{stat.tag}] {name}",
                        new[] { ("Профиль", (UnityAction)(() => ShowProfile(playerId, byId))) });
                }
            });
        });
    }

    private void PreviewJoin()
    {
        if (previewClanStat == null) return;
        JoinById((long)previewClanStat.clan_id);
        previewArea.SetActive(false);
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

    private void JoinById(long clanId)
    {
        ClanService.Instance.JoinClan(clanId, (result, err) =>
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

    // Участник выходит сразу. Лидер: первое нажатие — подтверждение, второе — роспуск
    private void Leave()
    {
        if (isLeader)
        {
            if (Time.unscaledTime > disbandArmedUntil)
            {
                disbandArmedUntil = Time.unscaledTime + 4f;
                SetStatus("Нажмите ещё раз, чтобы распустить клан");
                return;
            }
            disbandArmedUntil = -1f;
            ClanService.Instance.DisbandClan(err =>
            {
                if (err != null) { SetStatus("Ошибка роспуска: " + err); return; }
                SetStatus("Клан распущен");
                Refresh();
            });
            return;
        }

        ClanService.Instance.LeaveClan(err =>
        {
            if (err != null) { SetStatus("Ошибка выхода: " + err); return; }
            SetStatus("Вы вышли из клана");
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

    // Строка из шаблона: текст и до трёх кнопок (неиспользуемые скрываются)
    private void AddRow(Transform content, string label, (string, UnityAction)[] actions)
    {
        if (content == null || rowTemplate == null) return;
        GameObject row = Instantiate(rowTemplate, content);
        row.name = "Row";
        row.SetActive(true);

        Text t = row.GetComponentInChildren<Text>(true);
        if (t != null) t.text = label;

        Button[] buttons = row.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (actions != null && i < actions.Length)
            {
                buttons[i].gameObject.SetActive(true);
                Text bt = buttons[i].GetComponentInChildren<Text>(true);
                if (bt != null) bt.text = actions[i].Item1;
                buttons[i].onClick.AddListener(actions[i].Item2);
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    private static Dictionary<string, ClanService.AccountRow> BuildIndex(List<ClanService.AccountRow> accounts)
    {
        var byId = new Dictionary<string, ClanService.AccountRow>();
        if (accounts != null) foreach (var a in accounts) byId[a.player_id] = a;
        return byId;
    }

    private int GetActiveTab()
    {
        if (tabPages == null) return 0;
        for (int i = 0; i < tabPages.Length; i++)
            if (tabPages[i] != null && tabPages[i].activeSelf) return i;
        return 0;
    }

    private static void ClearContent(Transform content)
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject child = content.GetChild(i).gameObject;
            if (child.name == "Row") Destroy(child);
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private static string TypeName(string joinType) => joinType == "request" ? "по заявке" : "открыто";

    private static string ShortId(string id) => string.IsNullOrEmpty(id) ? "?" : id.Substring(0, System.Math.Min(6, id.Length));
}
