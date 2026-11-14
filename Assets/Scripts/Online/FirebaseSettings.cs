// Настройки онлайн-сервисов. Заполните своими значениями из консоли Firebase и GitHub.
// Пока значения содержат PASTE_..., онлайн-функции сами отключаются и игра не падает.
public static class FirebaseSettings
{
    // Firebase Console -> Project settings -> Web API Key
    public const string ApiKey = "AIzaSyCQZgDBJLGWXHHj-3FJ5eIP2LhScqLaDK4";

    // Realtime Database URL, без слеша в конце. База создана в регионе europe-west1
    // (старый адрес *.firebaseio.com для неё не работает, отдаёт 404).
    public const string DatabaseUrl = "https://gru-dash-game-7291-default-rtdb.europe-west1.firebasedatabase.app";

    // Домен для служебных гостевых e-mail (Firebase Auth принимает адрес без подтверждения)
    public const string GuestEmailDomain = "guest.gru-dash.app";

    // Манифест версий, лежит в репозитории GitHub, например
    // https://raw.githubusercontent.com/OWNER/REPO/main/version.json
    // Формат: {"version":"1.0.1","url":"https://...","notes":"..."}
    public const string UpdateManifestUrl = "https://raw.githubusercontent.com/WazalPlayer/GlobalUpdateGruDash/main/version.json";

    public const int ChatMaxLength = 200;
    public const int ChatHistoryCount = 50;
    public const float ChatPollSeconds = 3f;

    public static bool IsConfigured =>
        !ApiKey.StartsWith("PASTE_") && !DatabaseUrl.StartsWith("PASTE_");

    public static bool IsUpdateConfigured => !UpdateManifestUrl.StartsWith("PASTE_");
}
