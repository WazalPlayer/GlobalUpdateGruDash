using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Кланы через Supabase (таблицы и функции — в Docs/supabase_clans.sql).
// Использует URL и ключ из SupabaseManager. Игрок — SupabaseManager.PlayerId.
public class ClanService : MonoBehaviour
{
    public static ClanService Instance { get; private set; }

    public const int MaxMembers = 50;

    [Serializable]
    public class ClanData
    {
        public double id;
        public string name;
        public string tag;
        public string leader_id;
        public string join_type;
        public int member_limit;
        public double stars;
    }

    [Serializable]
    public class ClanMemberData
    {
        public double clan_id;
        public string player_id;
        public string role;
    }

    [Serializable]
    public class ClanMessageData
    {
        public double id;
        public double clan_id;
        public string player_id;
        public string player_name;
        public string text;
        public string created_at;
    }

    [Serializable] private class ClanListWrap { public List<ClanData> items; }
    [Serializable] private class MemberListWrap { public List<ClanMemberData> items; }
    [Serializable] private class MessageListWrap { public List<ClanMessageData> items; }
    [Serializable] private class ErrorBody { public string message; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private static string Url => SupabaseManager.Instance != null ? SupabaseManager.Instance.supabaseUrl : "";
    private static string Key => SupabaseManager.Instance != null ? SupabaseManager.Instance.supabaseKey : "";
    private static string PlayerId => SupabaseManager.Instance != null ? SupabaseManager.Instance.PlayerId : "";

    // ---------- Клан игрока и список ----------

    // Клан, в котором состоит игрок (или null)
    public void GetMyClan(Action<ClanData, string> done)
    {
        StartCoroutine(GetMyClanRoutine(done));
    }

    private IEnumerator GetMyClanRoutine(Action<ClanData, string> done)
    {
        string url = $"{Url}/rest/v1/clan_members?select=clan_id,player_id,role&player_id=eq.{PlayerId}";
        yield return Get(url, (ok, body, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            MemberListWrap m = JsonUtility.FromJson<MemberListWrap>("{\"items\":" + body + "}");
            if (m == null || m.items == null || m.items.Count == 0) { done?.Invoke(null, null); return; }
            long clanId = (long)m.items[0].clan_id;
            StartCoroutine(LoadClan(clanId, done));
        });
    }

    private IEnumerator LoadClan(long clanId, Action<ClanData, string> done)
    {
        string url = $"{Url}/rest/v1/clans?select=*&id=eq.{clanId}";
        yield return Get(url, (ok, body, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            ClanListWrap c = JsonUtility.FromJson<ClanListWrap>("{\"items\":" + body + "}");
            done?.Invoke(c != null && c.items.Count > 0 ? c.items[0] : null, null);
        });
    }

    // Топ кланов по звёздам
    public void GetTopClans(int limit, Action<List<ClanData>, string> done)
    {
        string url = $"{Url}/rest/v1/clans?select=*&order=stars.desc,created_at.asc&limit={limit}";
        StartCoroutine(Get(url, (ok, body, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            ClanListWrap c = JsonUtility.FromJson<ClanListWrap>("{\"items\":" + body + "}");
            done?.Invoke(c != null ? c.items : new List<ClanData>(), null);
        }));
    }

    // Участники клана (до 50)
    public void GetMembers(long clanId, Action<List<ClanMemberData>, string> done)
    {
        string url = $"{Url}/rest/v1/clan_members?select=clan_id,player_id,role&clan_id=eq.{clanId}&order=joined_at.asc";
        StartCoroutine(Get(url, (ok, body, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            MemberListWrap m = JsonUtility.FromJson<MemberListWrap>("{\"items\":" + body + "}");
            done?.Invoke(m != null ? m.items : new List<ClanMemberData>(), null);
        }));
    }

    // ---------- Создание, вступление, выход ----------

    // joinType: "open" или "request". Ответ: null при успехе, иначе текст ошибки
    public void CreateClan(string name, string tag, string joinType, Action<long, string> done)
    {
        string body = "{\"p_name\":\"" + Esc(name) + "\",\"p_tag\":\"" + Esc(tag) +
                      "\",\"p_join_type\":\"" + Esc(joinType) + "\",\"p_player\":\"" + Esc(PlayerId) + "\"}";
        StartCoroutine(Rpc("create_clan", body, (ok, text, err) =>
        {
            if (!ok) { done?.Invoke(0, err); return; }
            long.TryParse(text.Trim(), out long id);
            done?.Invoke(id, null);
        }));
    }

    // Результат: joined / requested / full / already_in_clan / not_found
    public void JoinClan(long clanId, Action<string, string> done)
    {
        string body = "{\"p_clan\":" + clanId + ",\"p_player\":\"" + Esc(PlayerId) + "\"}";
        StartCoroutine(Rpc("join_clan", body, (ok, text, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            done?.Invoke(text.Trim().Trim('"'), null);
        }));
    }

    public void LeaveClan(Action<string> done)
    {
        string body = "{\"p_player\":\"" + Esc(PlayerId) + "\"}";
        StartCoroutine(Rpc("leave_clan", body, (ok, text, err) => done?.Invoke(ok ? null : err)));
    }

    // Лидер распускает клан (удаляются участники, заявки и чат)
    public void DisbandClan(Action<string> done)
    {
        string body = "{\"p_player\":\"" + Esc(PlayerId) + "\"}";
        StartCoroutine(Rpc("disband_clan", body, (ok, text, err) => done?.Invoke(ok ? null : err)));
    }

    // Лидер: принять заявку игрока
    public void ApproveRequest(long clanId, string playerId, Action<string, string> done)
    {
        string body = "{\"p_clan\":" + clanId + ",\"p_leader\":\"" + Esc(PlayerId) + "\",\"p_player\":\"" + Esc(playerId) + "\"}";
        StartCoroutine(Rpc("approve_request", body, (ok, text, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            done?.Invoke(text.Trim().Trim('"'), null);
        }));
    }

    // Лидер: сменить тип вступления (open / request)
    public void SetJoinType(long clanId, string joinType, Action<string> done)
    {
        string url = $"{Url}/rest/v1/clans?id=eq.{clanId}&leader_id=eq.{PlayerId}";
        string body = "{\"join_type\":\"" + Esc(joinType) + "\"}";
        StartCoroutine(Send("PATCH", url, body, (ok, text, err) => done?.Invoke(ok ? null : err)));
    }

    // ---------- Внутриклановый чат ----------

    public void GetClanMessages(long clanId, int limit, Action<List<ClanMessageData>, string> done)
    {
        string url = $"{Url}/rest/v1/clan_messages?select=*&clan_id=eq.{clanId}&order=created_at.desc&limit={limit}";
        StartCoroutine(Get(url, (ok, body, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            MessageListWrap m = JsonUtility.FromJson<MessageListWrap>("{\"items\":" + body + "}");
            List<ClanMessageData> list = m != null ? m.items : new List<ClanMessageData>();
            list.Reverse(); // сервер отдаёт новые сверху, показываем старые сверху
            done?.Invoke(list, null);
        }));
    }

    public void SendClanMessage(long clanId, string text, Action<string> done)
    {
        string clean = (text ?? "").Trim();
        if (clean.Length == 0) { done?.Invoke("Пустое сообщение"); return; }
        if (clean.Length > 200) clean = clean.Substring(0, 200);

        string name = PlayerPrefs.GetString("PlayerName", "Игрок");
        string body = "{\"clan_id\":" + clanId + ",\"player_id\":\"" + Esc(PlayerId) +
                      "\",\"player_name\":\"" + Esc(name) + "\",\"text\":\"" + Esc(clean) + "\"}";
        string url = $"{Url}/rest/v1/clan_messages";
        StartCoroutine(Send("POST", url, body, (ok, t, err) => done?.Invoke(ok ? null : err)));
    }

    // Ники и статистика игроков по их id (таблица accounts)
    [Serializable]
    public class AccountRow
    {
        public string player_id;
        public string player_name;
        public double stars;
        public double demons;
        public string created_at;
    }

    [Serializable] private class AccountListWrap { public List<AccountRow> items; }

    public void GetAccounts(List<string> playerIds, Action<List<AccountRow>, string> done)
    {
        if (playerIds == null || playerIds.Count == 0) { done?.Invoke(new List<AccountRow>(), null); return; }
        StringBuilder sb = new StringBuilder();
        foreach (string id in playerIds)
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append('"').Append(id).Append('"');
        }
        string url = $"{Url}/rest/v1/accounts?select=player_id,player_name,stars,demons,created_at&player_id=in.({UnityWebRequest.EscapeURL(sb.ToString()).Replace("+", "%20")})";
        StartCoroutine(Get(url, (ok, body, err) =>
        {
            if (!ok) { done?.Invoke(null, err); return; }
            AccountListWrap a = JsonUtility.FromJson<AccountListWrap>("{\"items\":" + body + "}");
            done?.Invoke(a != null ? a.items : new List<AccountRow>(), null);
        }));
    }

    // ---------- Низкоуровневые запросы ----------

    private IEnumerator Get(string url, Action<bool, string, string> done)
    {
        using (UnityWebRequest r = UnityWebRequest.Get(url))
        {
            Headers(r);
            yield return r.SendWebRequest();
            done?.Invoke(r.result == UnityWebRequest.Result.Success, r.downloadHandler.text, ErrorText(r));
        }
    }

    private IEnumerator Rpc(string fn, string body, Action<bool, string, string> done)
    {
        yield return Send("POST", $"{Url}/rest/v1/rpc/{fn}", body, done);
    }

    private IEnumerator Send(string method, string url, string body, Action<bool, string, string> done)
    {
        using (UnityWebRequest r = new UnityWebRequest(url, method))
        {
            r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            r.downloadHandler = new DownloadHandlerBuffer();
            Headers(r);
            r.SetRequestHeader("Content-Type", "application/json");
            r.SetRequestHeader("Prefer", "return=minimal");
            yield return r.SendWebRequest();
            done?.Invoke(r.result == UnityWebRequest.Result.Success, r.downloadHandler.text, ErrorText(r));
        }
    }

    private static void Headers(UnityWebRequest r)
    {
        r.SetRequestHeader("apikey", Key);
        r.SetRequestHeader("Authorization", "Bearer " + Key);
    }

    // Текст ошибки из Postgres (raise exception 'xxx') или сетевой ошибки
    private static string ErrorText(UnityWebRequest r)
    {
        if (r.result == UnityWebRequest.Result.Success) return null;
        try
        {
            ErrorBody e = JsonUtility.FromJson<ErrorBody>(r.downloadHandler.text);
            if (e != null && !string.IsNullOrEmpty(e.message)) return e.message;
        }
        catch (Exception) { }
        return r.error;
    }

    private static string Esc(string s)
    {
        return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }
}
