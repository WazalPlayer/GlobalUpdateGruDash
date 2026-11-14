using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

// Единый стиль TextMeshPro-текстов: шрифт Russo One, обычное начертание, белый цвет,
// чёрная обводка 0.15. Перед применением сохраняется резервная копия, её можно откатить.
// Tools > UI > Apply Russo One Style to TMP (Open Scene)
// Tools > UI > Apply Russo One Style to TMP (All Scenes and Prefabs)
// Tools > UI > Revert TMP Style (restore backup)
public static class TmpStyleTool
{
    private const string FontPath = "Assets/Fonts/RussoOne-Regular.ttf";
    private const string TmpAssetPath = "Assets/Fonts/RussoOne SDF.asset";
    private const float OutlineWidth = 0.15f;
    private static readonly string BackupPath = Path.Combine("Library", "TmpStyleBackup.json");

    [System.Serializable]
    private class Entry
    {
        public string assetPath;     // сцена или префаб
        public string objectPath;    // путь объекта в иерархии
        public string fontGuid;      // исходный шрифт (guid), может быть пустым
        public int fontStyle;
        public float r, g, b, a;
    }

    [System.Serializable]
    private class Backup
    {
        public List<Entry> entries = new List<Entry>();
        public bool hasMaterial;
        public float faceR, faceG, faceB, faceA;
        public float outR, outG, outB, outA;
        public float outlineWidth;
        public string materialFontGuid;
    }

    [MenuItem("Tools/UI/Apply Russo One Style to TMP (Open Scene)")]
    public static void ApplyOpenScene()
    {
        Apply(false);
    }

    [MenuItem("Tools/UI/Apply Russo One Style to TMP (All Scenes and Prefabs)")]
    public static void ApplyAll()
    {
        Apply(true);
    }

    [MenuItem("Tools/UI/Revert TMP Style (restore backup)")]
    public static void Revert()
    {
        if (!File.Exists(BackupPath))
        {
            EditorUtility.DisplayDialog("Откат стиля", "Резервной копии нет: сначала примените стиль.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Откат стиля", "Вернуть исходные шрифты, стиль и цвета текстов из резервной копии?", "Да", "Отмена"))
            return;

        Backup backup = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
        RestoreMaterial(backup);

        // Группируем по файлам, чтобы не открывать одну сцену несколько раз
        var byAsset = new Dictionary<string, List<Entry>>();
        foreach (Entry e in backup.entries)
        {
            if (!byAsset.ContainsKey(e.assetPath)) byAsset[e.assetPath] = new List<Entry>();
            byAsset[e.assetPath].Add(e);
        }

        int restored = 0;
        foreach (var kv in byAsset)
        {
            if (kv.Key.EndsWith(".prefab"))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(kv.Key);
                try
                {
                    restored += RestoreEntries(root, kv.Value);
                    PrefabUtility.SaveAsPrefabAsset(root, kv.Key);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            else
            {
                var scene = EditorSceneManager.OpenScene(kv.Key, OpenSceneMode.Single);
                restored += RestoreEntries(null, kv.Value);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TmpStyleTool] Откат выполнен: восстановлено {restored} текстов.");
    }

    // ---------- применение ----------

    private static void Apply(bool allProject)
    {
        if (allProject && !EditorUtility.DisplayDialog("Стиль TMP",
            "Применить стиль ко всем TMP-текстам во всех сценах и префабах? Сцены будут сохранены. Резервная копия сохранится для отката.",
            "Да", "Отмена"))
            return;
        if (allProject && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        TMP_FontAsset font = PrepareFont();
        if (font == null) return;

        Backup backup = LoadOrNewBackup();
        if (!backup.hasMaterial) SaveMaterialBackup(backup, font);

        int total = 0;
        if (!allProject)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            total += ApplyToHierarchy(null, font, scene.path, backup);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int n = ApplyToHierarchy(null, font, path, backup);
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
                    int n = ApplyToHierarchy(root, font, path, backup);
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    total += n;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        File.WriteAllText(BackupPath, JsonUtility.ToJson(backup, true));
        AssetDatabase.SaveAssets();
        Debug.Log($"[TmpStyleTool] Стиль применён к {total} TMP-текстам. Откат: Tools → UI → Revert TMP Style.");
    }

    // Готовит TMP-шрифт и его материал. Создаёт ассет, если его ещё нет.
    private static TMP_FontAsset PrepareFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpAssetPath);
        if (font == null)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (ttf == null)
            {
                EditorUtility.DisplayDialog("Стиль TMP", "Нет файла шрифта " + FontPath, "OK");
                return null;
            }
            font = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
            AssetDatabase.CreateAsset(font, TmpAssetPath);
        }

        Material mat = font.material;
        mat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, OutlineWidth);
        EditorUtility.SetDirty(mat);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return font;
    }

    // Сохраняет исходные параметры материала шрифта (чтобы откатить обводку и цвет)
    private static void SaveMaterialBackup(Backup backup, TMP_FontAsset font)
    {
        Material mat = font.material;
        Color face = mat.GetColor(ShaderUtilities.ID_FaceColor);
        Color outline = mat.GetColor(ShaderUtilities.ID_OutlineColor);
        backup.hasMaterial = true;
        backup.faceR = face.r; backup.faceG = face.g; backup.faceB = face.b; backup.faceA = face.a;
        backup.outR = outline.r; backup.outG = outline.g; backup.outB = outline.b; backup.outA = outline.a;
        backup.outlineWidth = mat.GetFloat(ShaderUtilities.ID_OutlineWidth);
        backup.materialFontGuid = AssetDatabase.AssetPathToGUID(TmpAssetPath);
    }

    private static void RestoreMaterial(Backup backup)
    {
        if (!backup.hasMaterial) return;
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpAssetPath);
        if (font == null) return;
        Material mat = font.material;
        mat.SetColor(ShaderUtilities.ID_FaceColor, new Color(backup.faceR, backup.faceG, backup.faceB, backup.faceA));
        mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(backup.outR, backup.outG, backup.outB, backup.outA));
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, backup.outlineWidth);
        EditorUtility.SetDirty(mat);
    }

    // Загружает существующую копию, чтобы при повторном применении не потерять исходные значения
    private static Backup LoadOrNewBackup()
    {
        if (File.Exists(BackupPath))
        {
            Backup old = JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
            if (old != null) return old;
        }
        return new Backup();
    }

    private static int ApplyToHierarchy(GameObject root, TMP_FontAsset font, string assetPath, Backup backup)
    {
        TMP_Text[] texts = root == null
            ? Object.FindObjectsOfType<TMP_Text>(true)
            : root.GetComponentsInChildren<TMP_Text>(true);

        int count = 0;
        foreach (TMP_Text t in texts)
        {
            string objPath = GetPath(t.transform, root != null ? root.transform : null);
            // Запоминаем исходное состояние только один раз
            if (!backup.entries.Exists(e => e.assetPath == assetPath && e.objectPath == objPath))
            {
                Color c = t.color;
                backup.entries.Add(new Entry
                {
                    assetPath = assetPath,
                    objectPath = objPath,
                    fontGuid = t.font != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t.font)) : "",
                    fontStyle = (int)t.fontStyle,
                    r = c.r, g = c.g, b = c.b, a = c.a
                });
            }

            Undo.RecordObject(t, "TMP style");
            t.font = font;
            t.fontStyle = FontStyles.Normal;
            t.color = Color.white;
            EditorUtility.SetDirty(t);
            count++;
        }
        return count;
    }

    private static int RestoreEntries(GameObject root, List<Entry> entries)
    {
        int count = 0;
        TMP_Text[] texts = root == null
            ? Object.FindObjectsOfType<TMP_Text>(true)
            : root.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text t in texts)
        {
            string objPath = GetPath(t.transform, root != null ? root.transform : null);
            Entry e = entries.Find(x => x.objectPath == objPath);
            if (e == null) continue;

            Undo.RecordObject(t, "TMP revert");
            if (!string.IsNullOrEmpty(e.fontGuid))
            {
                TMP_FontAsset original = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(e.fontGuid));
                if (original != null) t.font = original;
            }
            t.fontStyle = (FontStyles)e.fontStyle;
            t.color = new Color(e.r, e.g, e.b, e.a);
            EditorUtility.SetDirty(t);
            count++;
        }
        return count;
    }

    // Путь объекта от корня сцены/префаба (для сопоставления при откате)
    private static string GetPath(Transform t, Transform stop)
    {
        string p = t.name;
        Transform cur = t.parent;
        while (cur != null && cur != stop)
        {
            p = cur.name + "/" + p;
            cur = cur.parent;
        }
        return p;
    }
}
