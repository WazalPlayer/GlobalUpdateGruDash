using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Полный переход: UI Text -> TextMeshProUGUI во всём проекте (скрипты, сцены, префабы).
// Ссылки сохраняются: перед удалением старых компонентов записываются все ссылки на них,
// после перекомпиляции скриптов ссылки восстанавливаются из этой записи.
// Резервная копия: Library/TextToTmpBackup. Откат: Tools > UI > Revert Text to TMP.
public static class TextToTmpConverter
{
    private const string FontPath = "Assets/Fonts/RussoOne-Regular.ttf";
    private const string TmpAssetPath = "Assets/Fonts/RussoOne SDF.asset";
    private const float OutlineWidth = 0.15f;
    private const string PhaseKey = "TextToTmp.Phase";
    private static readonly string BackupDir = Path.Combine("Library", "TextToTmpBackup");
    private static readonly string ManifestPath = Path.Combine(BackupDir, "manifest.txt");
    private static readonly string RefsPath = Path.Combine("Library", "TextToTmpRefs.json");

    [Serializable]
    private class RefEntry
    {
        public string assetPath;   // сцена или префаб
        public string ownerPath;   // объект-владелец ссылки (путь), "." — корень префаба
        public string compType;    // полное имя класса компонента-владельца
        public string propPath;    // путь к полю (SerializedProperty.propertyPath)
        public string targetPath;  // объект, где был Text (путь)
    }

    [Serializable]
    private class RefList
    {
        public List<RefEntry> items = new List<RefEntry>();
    }

    // ---------- Конвертация ----------

    [MenuItem("Tools/UI/Convert Text to TMP (Whole Project)")]
    public static void ConvertAll()
    {
        if (EditorPrefs.GetString(PhaseKey, "") != "")
        {
            EditorUtility.DisplayDialog("Text → TMP", "Преобразование уже выполняется или не завершено. Дождитесь перекомпиляции.", "OK");
            return;
        }
        if (Directory.Exists(BackupDir))
        {
            EditorUtility.DisplayDialog("Text → TMP",
                "Резервная копия уже есть (Library/TextToTmpBackup). Сначала выполните Revert Text to TMP, иначе копия будет потеряна.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Text → TMP",
            "Преобразовать ВСЕ Text в TextMeshPro во всём проекте? Будет создана резервная копия, откат — Tools → UI → Revert Text to TMP.",
            "Да", "Отмена"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        TMP_FontAsset font = PrepareFont();
        if (font == null) return;

        BackupAll();

        var refs = new RefList();
        int prefabs = 0, scenes = 0, comps = 0;

        // Этап 1: префабы
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int n = ConvertRoots(new GameObject[] { root }, root.transform, path, font, refs);
                if (n > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabs++;
                    comps += n;
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // Этап 1: сцены
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = ConvertRoots(scene.GetRootGameObjects(), null, path, font, refs);
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                scenes++;
                comps += n;
            }
        }

        File.WriteAllText(RefsPath, JsonUtility.ToJson(refs, true), new UTF8Encoding(false));

        // Этап 2: скрипты
        int changedScripts = ConvertScripts();
        AssetDatabase.SaveAssets();

        // Этап 3: восстановление ссылок (после перекомпиляции или сразу, если скрипты не менялись)
        EditorPrefs.SetString(PhaseKey, "restore");
        AssetDatabase.Refresh();
        if (changedScripts == 0)
            EditorApplication.delayCall += RestoreReferences;

        Debug.Log($"[TextToTmp] Этап 1: заменено {comps} компонентов (префабов {prefabs}, сцен {scenes}), " +
                  $"ссылок записано {refs.items.Count}. Скриптов изменено {changedScripts}. Ссылки восстанавливаются после перекомпиляции.");
    }

    // Вызывается Unity после перекомпиляции скриптов
    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (EditorPrefs.GetString(PhaseKey, "") == "restore")
            EditorApplication.delayCall += RestoreReferences;
    }

    // ---------- Этап 1: замена компонентов и запись ссылок ----------

    // Параметры старого Text, которые переносим в TMP
    private class TextProps
    {
        public string text;
        public int fontSize;
        public TextAnchor alignment;
        public bool wrap;
        public bool richText;
        public bool raycastTarget;
        public bool autoSize;
        public bool maskable;
        public bool enabled;
    }

    private static int ConvertRoots(GameObject[] roots, Transform stop, string assetPath, TMP_FontAsset font, RefList refs)
    {
        var texts = new List<Text>();
        foreach (GameObject r in roots) texts.AddRange(r.GetComponentsInChildren<Text>(true));
        if (texts.Count == 0) return 0;

        // 1. Запоминаем параметры и владельцев объекта Text
        var owners = new Dictionary<Text, GameObject>();
        var props = new Dictionary<Text, TextProps>();
        foreach (Text t in texts)
        {
            owners[t] = t.gameObject;
            props[t] = new TextProps
            {
                text = t.text,
                fontSize = t.fontSize,
                alignment = t.alignment,
                wrap = t.horizontalOverflow == HorizontalWrapMode.Wrap,
                richText = t.supportRichText,
                raycastTarget = t.raycastTarget,
                autoSize = t.resizeTextForBestFit,
                maskable = t.maskable,
                enabled = t.enabled
            };
        }

        // 2. Записываем все ссылки на старые Text, пока они существуют
        foreach (GameObject r in roots)
        {
            foreach (Component c in r.GetComponentsInChildren<Component>(true))
            {
                if (c is Text) continue;
                var so = new SerializedObject(c);
                SerializedProperty it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Text target = it.objectReferenceValue as Text;
                    if (target == null || !props.ContainsKey(target)) continue;

                    refs.items.Add(new RefEntry
                    {
                        assetPath = assetPath,
                        ownerPath = PathOf(c.transform, stop),
                        compType = c.GetType().FullName,
                        propPath = it.propertyPath,
                        targetPath = PathOf(target.transform, stop)
                    });
                }
            }
        }

        // 3. Удаляем старый Text (иначе на объекте два Graphic и TMP не добавится)
        foreach (Text t in texts) UnityEngine.Object.DestroyImmediate(t);

        // 4. Добавляем TextMeshProUGUI с теми же параметрами
        foreach (Text t in texts)
        {
            GameObject go = owners[t];
            TextProps p = props[t];
            TextMeshProUGUI n = go.AddComponent<TextMeshProUGUI>();
            ApplyProps(p, n, font);
        }
        return texts.Count;
    }

    private static void ApplyProps(TextProps p, TextMeshProUGUI n, TMP_FontAsset font)
    {
        n.font = font;
        n.text = p.text;
        n.fontSize = p.fontSize;
        n.color = Color.white;
        n.alignment = MapAlignment(p.alignment);
        n.enableWordWrapping = p.wrap;
        n.richText = p.richText;
        n.raycastTarget = p.raycastTarget;
        n.fontStyle = FontStyles.Normal;
        n.enableAutoSizing = p.autoSize;
        n.maskable = p.maskable;
        n.enabled = p.enabled;
    }

    private static TextAlignmentOptions MapAlignment(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }

    // ---------- Этап 2: скрипты ----------

    private static int ConvertScripts()
    {
        int changed = 0;
        foreach (string file in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
        {
            string norm = file.Replace('\\', '/');
            if (norm.Contains("/Editor/TextToTmp") || norm.Contains("/Editor/TmpStyleTool")) continue;

            string src = File.ReadAllText(file);
            string dst = ConvertSource(src);
            if (dst != src)
            {
                File.WriteAllText(file, dst, new UTF8Encoding(false));
                changed++;
            }
        }
        return changed;
    }

    private static string ConvertSource(string s)
    {
        string r = s;
        r = Regex.Replace(r, @"\bUnityEngine\.UI\.Text\b", "TMPro.TextMeshProUGUI");
        // Тип Text как тип переменной, поля, параметра или generic-аргумента
        r = Regex.Replace(r, @"(?<![\w.""])Text(?=\[\]|\s*>|\s+[A-Za-z_]\w*\s*[;=,)\{])", "TextMeshProUGUI");

        if (r == s) return s;

        r = r.Replace("TextAnchor.UpperLeft", "TextAlignmentOptions.TopLeft")
             .Replace("TextAnchor.UpperCenter", "TextAlignmentOptions.Top")
             .Replace("TextAnchor.UpperRight", "TextAlignmentOptions.TopRight")
             .Replace("TextAnchor.MiddleLeft", "TextAlignmentOptions.Left")
             .Replace("TextAnchor.MiddleCenter", "TextAlignmentOptions.Center")
             .Replace("TextAnchor.MiddleRight", "TextAlignmentOptions.Right")
             .Replace("TextAnchor.LowerLeft", "TextAlignmentOptions.BottomLeft")
             .Replace("TextAnchor.LowerCenter", "TextAlignmentOptions.Bottom")
             .Replace("TextAnchor.LowerRight", "TextAlignmentOptions.BottomRight");

        r = Regex.Replace(r, @"\.horizontalOverflow\s*=\s*HorizontalWrapMode\.Wrap\s*;", ".enableWordWrapping = true;");
        r = Regex.Replace(r, @"\.horizontalOverflow\s*=\s*HorizontalWrapMode\.Overflow\s*;", ".enableWordWrapping = false;");
        r = Regex.Replace(r, @"\.verticalOverflow\s*=\s*VerticalWrapMode\.\w+\s*;", "");
        r = Regex.Replace(r, @"\.resizeTextForBestFit\s*=", ".enableAutoSizing =");
        r = r.Replace("FontStyle.Bold", "FontStyles.Bold")
             .Replace("FontStyle.Italic", "FontStyles.Italic")
             .Replace("FontStyle.BoldAndItalic", "FontStyles.Bold | FontStyles.Italic")
             .Replace("FontStyle.Normal", "FontStyles.Normal");

        if (!r.Contains("using TMPro;")) r = "using TMPro;\n" + r;
        return r;
    }

    // ---------- Этап 3: восстановление ссылок ----------

    private static void RestoreReferences()
    {
        if (EditorPrefs.GetString(PhaseKey, "") != "restore") return;

        int ok = 0, fail = 0;
        try
        {
            if (!File.Exists(RefsPath))
            {
                Debug.Log("[TextToTmp] Записи ссылок нет, восстанавливать нечего.");
                return;
            }
            RefList refs = JsonUtility.FromJson<RefList>(File.ReadAllText(RefsPath));
            if (refs == null) return;

            foreach (var group in refs.items.GroupBy(e => e.assetPath))
            {
                string path = group.Key;
                if (path.EndsWith(".prefab"))
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        foreach (RefEntry e in group) { if (Restore(e, new[] { root }, root.transform)) ok++; else fail++; }
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                else
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    GameObject[] roots = scene.GetRootGameObjects();
                    foreach (RefEntry e in group) { if (Restore(e, roots, null)) ok++; else fail++; }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TextToTmp] Этап 3: ссылок восстановлено {ok}, не удалось {fail}." +
                      (fail > 0 ? " Не восстановленные смотрите в предупреждениях выше." : ""));
        }
        finally
        {
            EditorPrefs.DeleteKey(PhaseKey);
        }
    }

    private static bool Restore(RefEntry e, GameObject[] roots, Transform stop)
    {
        GameObject owner = ResolveGo(roots, stop, e.ownerPath);
        GameObject target = ResolveGo(roots, stop, e.targetPath);
        if (owner == null || target == null)
        {
            Debug.LogWarning($"[TextToTmp] Не найден объект: владелец '{e.ownerPath}' или цель '{e.targetPath}' в {e.assetPath}");
            return false;
        }

        Component comp = owner.GetComponents<Component>().FirstOrDefault(c => c.GetType().FullName == e.compType);
        TextMeshProUGUI tmp = target.GetComponent<TextMeshProUGUI>();
        if (comp == null || tmp == null)
        {
            Debug.LogWarning($"[TextToTmp] Нет компонента {e.compType} или TMP на {e.ownerPath} в {e.assetPath}");
            return false;
        }

        var so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(e.propPath);
        if (p == null)
        {
            Debug.LogWarning($"[TextToTmp] Не найдено поле {e.propPath} у {e.compType} в {e.assetPath}");
            return false;
        }
        p.objectReferenceValue = tmp;
        so.ApplyModifiedProperties();
        return true;
    }

    // Путь объекта относительно корня (для префаба — относительно корня префаба, "." — сам корень)
    private static string PathOf(Transform t, Transform stop)
    {
        if (stop != null && t == stop) return ".";
        string p = t.name;
        Transform cur = t.parent;
        while (cur != null && cur != stop)
        {
            p = cur.name + "/" + p;
            cur = cur.parent;
        }
        return p;
    }

    private static GameObject ResolveGo(GameObject[] roots, Transform stop, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (path == ".") return stop != null ? stop.gameObject : null;

        string[] parts = path.Split('/');
        Transform cur;
        int start;
        if (stop != null)
        {
            cur = stop;
            start = 0;
        }
        else
        {
            GameObject root = roots.FirstOrDefault(r => r.name == parts[0]);
            if (root == null) return null;
            cur = root.transform;
            start = 1;
        }

        for (int i = start; i < parts.Length; i++)
        {
            cur = cur.Find(parts[i]);
            if (cur == null) return null;
        }
        return cur.gameObject;
    }

    // ---------- Резервная копия и откат ----------

    private static void BackupAll()
    {
        Directory.CreateDirectory(BackupDir);
        var manifest = new List<string>();
        foreach (string ext in new[] { "*.cs", "*.unity", "*.prefab" })
        {
            foreach (string file in Directory.GetFiles("Assets", ext, SearchOption.AllDirectories))
            {
                string rel = file.Replace('\\', '/');
                string dst = Path.Combine(BackupDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(file, dst, true);
                manifest.Add(rel);
            }
        }
        File.WriteAllLines(ManifestPath, manifest, new UTF8Encoding(false));
        Debug.Log($"[TextToTmp] Резервная копия: {manifest.Count} файлов в {Path.GetFullPath(BackupDir)}");
    }

    [MenuItem("Tools/UI/Revert Text to TMP (restore backup)")]
    public static void Revert()
    {
        if (!File.Exists(ManifestPath))
        {
            EditorUtility.DisplayDialog("Откат Text → TMP", "Резервной копии нет.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Откат Text → TMP",
            "Вернуть все скрипты, сцены и префабы из резервной копии? Изменения после преобразования будут потеряны.",
            "Да", "Отмена"))
            return;

        // Закрываем сцены, чтобы не писать поверх открытой
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int restored = 0;
        foreach (string rel in File.ReadAllLines(ManifestPath))
        {
            if (string.IsNullOrWhiteSpace(rel)) continue;
            string src = Path.Combine(BackupDir, rel);
            if (!File.Exists(src)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(rel));
            File.Copy(src, rel, true);
            restored++;
        }

        EditorPrefs.DeleteKey(PhaseKey);
        if (File.Exists(RefsPath)) File.Delete(RefsPath);
        AssetDatabase.Refresh();
        // Файлы восстановлены из копии, сама копия больше не нужна: удаляем, чтобы можно было снова запустить преобразование
        try { Directory.Delete(BackupDir, true); }
        catch (IOException ex) { Debug.LogWarning("[TextToTmp] Не удалось удалить резервную копию: " + ex.Message); }
        Debug.Log($"[TextToTmp] Откат выполнен: восстановлено {restored} файлов. Резервная копия удалена, преобразование можно запускать снова.");
    }

    // ---------- Шрифт ----------

    private static TMP_FontAsset PrepareFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpAssetPath);
        // Битый ассет без материала (сохранился неполным) пересоздаём
        if (font != null && font.material == null)
        {
            AssetDatabase.DeleteAsset(TmpAssetPath);
            font = null;
        }
        if (font == null)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (ttf == null)
            {
                EditorUtility.DisplayDialog("Text → TMP", "Нет файла шрифта " + FontPath, "OK");
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
}
