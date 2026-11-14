using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Только отчёт, ничего не меняет в проекте.
// Tools > UI > Text to TMP: Dry Run Report
// Показывает: сколько компонентов Text в каждой сцене и префабе, и какие поля скриптов объявлены как Text.
// Отчёт сохраняется в Library/TextToTmpReport.txt
public static class TextToTmpReport
{
    private static readonly Regex FieldRegex = new Regex(
        @"^\s*(public|private|protected|\[SerializeField\][^\n]*?)\s*(?:private|public|protected)?\s*(?:static\s+)?(Text|UnityEngine\.UI\.Text)(\[\])?\s+(\w+)",
        RegexOptions.Multiline);

    [MenuItem("Tools/UI/Text to TMP: Dry Run Report")]
    public static void Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Text -> TMP dry run (ничего не изменено) ===");

        sb.AppendLine("\n-- Сцены --");
        int sceneTotal = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = CountLegacyText(scene.GetRootGameObjects());
            if (n > 0) sb.AppendLine($"{path}: {n}");
            sceneTotal += n;
        }

        sb.AppendLine("\n-- Префабы --");
        int prefabTotal = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int n = CountLegacyText(new GameObject[] { root });
                if (n > 0) sb.AppendLine($"{path}: {n}");
                prefabTotal += n;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        sb.AppendLine("\n-- Поля скриптов типа Text --");
        int fieldTotal = 0;
        foreach (string file in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            MatchCollection matches = FieldRegex.Matches(text);
            if (matches.Count == 0) continue;
            sb.AppendLine(file.Replace('\\', '/') + ":");
            foreach (Match m in matches)
                sb.AppendLine("   " + m.Groups[4].Value + " : Text" + m.Groups[3].Value);
            fieldTotal += matches.Count;
        }

        sb.AppendLine($"\nИтого: компонентов Text в сценах {sceneTotal}, в префабах {prefabTotal}, полей скриптов {fieldTotal}.");

        string reportPath = Path.Combine("Library", "TextToTmpReport.txt");
        File.WriteAllText(reportPath, sb.ToString(), new UTF8Encoding(false));
        Debug.Log(sb.ToString());
        Debug.Log("[TextToTmpReport] Отчёт сохранён: " + Path.GetFullPath(reportPath));
    }

    private static int CountLegacyText(GameObject[] roots)
    {
        int count = 0;
        foreach (GameObject r in roots)
            count += r.GetComponentsInChildren<Text>(true).Length;
        return count;
    }
}
