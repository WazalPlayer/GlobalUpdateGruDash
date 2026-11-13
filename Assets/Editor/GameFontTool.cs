using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Шрифт игры (Russo One, поддерживает кириллицу).
// Tools > Fonts > Create TMP Font Asset — создаёт TMP-шрифт для кириллицы.
// Tools > Fonts > Apply to Open Scene — ставит шрифт всем Text и TMP_Text в открытой сцене.
public static class GameFontTool
{
    private const string FontPath = "Assets/Fonts/RussoOne-Regular.ttf";
    private const string TmpAssetPath = "Assets/Fonts/RussoOne SDF.asset";

    [MenuItem("Tools/Fonts/Create TMP Font Asset")]
    public static void CreateTmpAsset()
    {
        TMP_FontAsset tmpAsset = LoadOrCreateTmpAsset();
        if (tmpAsset != null)
        {
            Selection.activeObject = tmpAsset;
            Debug.Log("[GameFontTool] TMP-шрифт готов: " + TmpAssetPath);
        }
    }

    [MenuItem("Tools/Fonts/Apply to Open Scene")]
    public static void ApplyToOpenScene()
    {
        Font legacyFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        TMP_FontAsset tmpAsset = LoadOrCreateTmpAsset();
        if (legacyFont == null || tmpAsset == null)
        {
            EditorUtility.DisplayDialog("Шрифт", "Не найден файл шрифта " + FontPath, "OK");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        int legacyCount = 0, tmpCount = 0;

        foreach (Text t in Object.FindObjectsOfType<Text>(true))
        {
            Undo.RecordObject(t, "Apply font");
            t.font = legacyFont;
            legacyCount++;
        }
        foreach (TMP_Text t in Object.FindObjectsOfType<TMP_Text>(true))
        {
            Undo.RecordObject(t, "Apply font");
            t.font = tmpAsset;
            tmpCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GameFontTool] Шрифт применён: Text — {legacyCount}, TMP — {tmpCount}. Сцена: {scene.name}");
    }

    private static TMP_FontAsset LoadOrCreateTmpAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpAssetPath);
        if (existing != null) return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            Debug.LogError("[GameFontTool] Нет файла " + FontPath);
            return null;
        }

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
        if (asset == null) return null;
        AssetDatabase.CreateAsset(asset, TmpAssetPath);
        AssetDatabase.SaveAssets();
        return asset;
    }
}
