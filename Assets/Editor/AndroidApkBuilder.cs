using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Сборка APK на рабочий стол одним кликом.
// Tools > Build > Android APK to Desktop
// Подпись: отладочная (debug keystore Unity). Свой keystore не подставляется.
public static class AndroidApkBuilder
{
    [MenuItem("Tools/Build/Android APK to Desktop")]
    public static void BuildApk()
    {
        string version = PlayerSettings.bundleVersion;
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string apkPath = Path.Combine(desktop, "GruDash_" + version + ".apk");

        if (!EditorUtility.DisplayDialog("Сборка APK",
            "Собрать APK версии " + version + " и положить на рабочий стол:\n" + apkPath + "\n\nЭто займёт несколько минут.",
            "Собрать", "Отмена"))
            return;

        // Переключаем платформу на Android, если она ещё не активна
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Сборка APK", "Не удалось переключить платформу на Android. Проверьте, установлен ли Android Build Support.", "OK");
                return;
            }
        }

        EditorUserBuildSettings.buildAppBundle = false;      // именно APK, не AAB
        PlayerSettings.Android.useCustomKeystore = false;    // отладочная подпись

        string[] scenes = Array.ConvertAll(
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(EditorBuildSettings.scenes, s => s.enabled)),
            s => s.path);
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Сборка APK", "В Build Settings нет включённых сцен.", "OK");
            return;
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AndroidApkBuilder] APK готов: {apkPath} ({summary.totalSize / (1024 * 1024)} МБ)");
            EditorUtility.RevealInFinder(apkPath);
        }
        else
        {
            Debug.LogError($"[AndroidApkBuilder] Сборка не удалась: {summary.result}. Смотрите ошибки выше в Console.");
        }
    }
}
