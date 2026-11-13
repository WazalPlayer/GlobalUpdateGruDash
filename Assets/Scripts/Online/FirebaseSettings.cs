// Настройки онлайн-сервисов. Заполните своими значениями из консоли Firebase и GitHub.
// Пока значения содержат PASTE_..., онлайн-функции сами отключаются и игра не падает.
public static class FirebaseSettings
{
    // Firebase Console -> Project settings -> Web API Key
    public const string ApiKey = "AIzaSyBIsPBGfOgoE0o0-JHtV6aQk7xOKL56vyA";

    // Realtime Database URL, без слеша в конце. База должна быть создана в консоли Firebase
    // (проект gru-dash-231740, регион us-central1): ссылка появится после создания.
    public const string DatabaseUrl = "https://gru-dash-231740-default-rtdb.firebaseio.com";

    // Домен для служебных гостевых e-mail (Firebase Auth принимает адрес без подтверждения)
    public const string GuestEmailDomain = "guest.gru-dash.app";

    // Манифест версий, лежит в репозитории GitHub, например
    // https://raw.githubusercontent.com/OWNER/REPO/main/version.json
    // Формат: {"version":"1.0.1","url":"https://...","notes":"..."}
    public const string UpdateManifestUrl = "PASTE_GITHUB_RAW_VERSION_JSON_URL";

    public const int ChatMaxLength = 200;
    public const int ChatHistoryCount = 50;
    public const float ChatPollSeconds = 3f;

    public static bool IsConfigured =>
        !ApiKey.StartsWith("PASTE_") && !DatabaseUrl.StartsWith("PASTE_");

    public static bool IsUpdateConfigured => !UpdateManifestUrl.StartsWith("PASTE_");
}
