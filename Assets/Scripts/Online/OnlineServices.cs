using UnityEngine;

// Создаёт сервисы онлайна автоматически в каждой сцене, без правок сцен.
public static class OnlineServices
{
    private static GameObject root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (root != null) return;

        root = new GameObject("OnlineServices");
        Object.DontDestroyOnLoad(root);

        // Supabase: используем уже существующий SupabaseManager
        if (SupabaseManager.Instance == null)
            root.AddComponent<SupabaseManager>();

        root.AddComponent<GuestAccountService>();
        root.AddComponent<FirebaseChatService>();
        root.AddComponent<UpdateChecker>();
        root.AddComponent<ClanService>();
    }
}
