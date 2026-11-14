using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Меняет ТОЛЬКО шрифт у всех текстов: Text (legacy) и TMP_Text.
// Стиль (Bold/Italic), цвет, размер, обводка и остальное не трогаются.
// Tools > UI > Set Font Only (Open Scene)
// Tools > UI > Set Font Only (All Scenes and Prefabs)
public static class FontOnlyTool
{
    private const string FontPath = "Assets/Fonts/RussoOne-Regular.ttf";
    private const string TmpAssetPath = "Assets/Fonts/RussoOne SDF.asset";

    [MenuItem("Tools/UI/Set Font Only (Open Scene)")]
    public static void OpenScene()
    {
        Font legacy = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        TMP_FontAsset tmp = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpAssetPath);
        if (legacy == null || tmp == null)
        {
            EditorUtility.DisplayDialog("Шрифт", "Не найдены RussoOne-Regular.ttf или RussoOne SDF.asset в Assets/Fonts.", "OK");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        int n = SetFonts(null, legacy, tmp);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[FontOnlyTool] Шрифт заменён у {n} текстов в сцене {scene.name}.");
    }

    [MenuItem("Tools/UI/Set Font Only (All Scenes and Prefabs)")]
    public static void AllProject()
    {
        if (!EditorUtility.DisplayDialog("Шрифт",
            "Заменить шрифт у всех Text и TMP текстов во всех сценах и префабах? Стиль, цвет и размер не меняются. Сцены будут сохранены.",
            "Да", "Отмена"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Font legacy = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        TMP_FontAsset tmp = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpAssetPath);
        if (legacy == null || tmp == null)
        {
            EditorUtility.DisplayDialog("Шрифт", "Не найдены RussoOne-Regular.ttf или RussoOne SDF.asset в Assets/Fonts.", "OK");
            return;
        }

        int total = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = SetFonts(null, legacy, tmp);
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            total += n;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int n = SetFonts(root, legacy, tmp);
                if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                total += n;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FontOnlyTool] Шрифт заменён у {total} текстов во всём проекте.");
    }

    // root == null — вся открытая сцена
    private static int SetFonts(GameObject root, Font legacy, TMP_FontAsset tmp)
    {
        int count = 0;

        Text[] legacyTexts = root == null
            ? Object.FindObjectsOfType<Text>(true)
            : root.GetComponentsInChildren<Text>(true);
        foreach (Text t in legacyTexts)
        {
            if (t.font == legacy) continue;
            Undo.RecordObject(t, "Font only");
            t.font = legacy;
            EditorUtility.SetDirty(t);
            count++;
        }

        TMP_Text[] tmpTexts = root == null
            ? Object.FindObjectsOfType<TMP_Text>(true)
            : root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in tmpTexts)
        {
            if (t.font == tmp) continue;
            Undo.RecordObject(t, "Font only");
            t.font = tmp;
            EditorUtility.SetDirty(t);
            count++;
        }

        return count;
    }
}
