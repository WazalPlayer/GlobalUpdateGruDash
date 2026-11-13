using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ставит стиль Normal всем текстам (Text и TMP_Text) на открытой сцене
// или во всех сценах и префабах проекта.
// Tools > Fonts > Set Normal Style in Open Scene
// Tools > Fonts > Set Normal Style in All Scenes and Prefabs
public static class FontStyleNormalizer
{
    [MenuItem("Tools/Fonts/Set Normal Style in Open Scene")]
    public static void OpenScene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        int n = NormalizeHierarchy(null);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[FontStyleNormalizer] Стиль Normal: {n} текстов в сцене {scene.name}.");
    }

    [MenuItem("Tools/Fonts/Set Normal Style in All Scenes and Prefabs")]
    public static void AllScenesAndPrefabs()
    {
        if (!EditorUtility.DisplayDialog("Normal style",
            "Поставить стиль Normal всем текстам во всех сценах и префабах? Сцены будут сохранены.",
            "Да", "Отмена"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int total = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = NormalizeHierarchy(null);
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
                int n = NormalizeHierarchy(root);
                if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                total += n;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FontStyleNormalizer] Стиль Normal применён к {total} текстам во всём проекте.");
    }

    // root == null — обрабатываем всю открытую сцену
    private static int NormalizeHierarchy(GameObject root)
    {
        int count = 0;

        Text[] legacy = root == null
            ? Object.FindObjectsOfType<Text>(true)
            : root.GetComponentsInChildren<Text>(true);
        foreach (Text t in legacy)
        {
            if (t.fontStyle == FontStyle.Normal) continue;
            Undo.RecordObject(t, "Normal style");
            t.fontStyle = FontStyle.Normal;
            EditorUtility.SetDirty(t);
            count++;
        }

        TMP_Text[] tmp = root == null
            ? Object.FindObjectsOfType<TMP_Text>(true)
            : root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in tmp)
        {
            // Снимаем все стилистические флаги: Bold, Italic, Underline, Strikethrough, Highlight, Superscript, Subscript, Uppercase, Lowercase, SmallCaps
            if (t.fontStyle == FontStyles.Normal) continue;
            Undo.RecordObject(t, "Normal style");
            t.fontStyle = FontStyles.Normal;
            EditorUtility.SetDirty(t);
            count++;
        }

        return count;
    }
}
