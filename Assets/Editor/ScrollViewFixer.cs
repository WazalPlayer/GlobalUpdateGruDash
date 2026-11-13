using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Чинит все ScrollRect во всех сценах и префабах проекта.
// Запуск: Tools > UI > Fix ScrollViews in All Scenes and Prefabs
// Что делает: назначает Viewport и Content, включает Raycast у Viewport,
// добавляет маску, ставит зажатую прокрутку без отскока, добавляет EventSystem в сцену.
// Что НЕ трогает: размеры и положение объектов, цвета, и Content без Layout Group (его отчёт выведет).
public static class ScrollViewFixer
{
    [MenuItem("Tools/UI/Fix ScrollViews in All Scenes and Prefabs")]
    public static void FixAll()
    {
        if (!EditorUtility.DisplayDialog("Fix ScrollViews",
            "Исправить все ScrollRect во всех сценах и префабах? Сцены будут сохранены.\n" +
            "Несохранённые изменения в открытой сцене будут предложены к сохранению.",
            "Да", "Отмена"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var report = new StringBuilder();
        int fixedRects = 0;
        int sceneCount = 0;
        int prefabCount = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = FixScene(scene.name, report);
            fixedRects += n;
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sceneCount++;
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int n = FixHierarchy(root, path, report);
                if (n > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabCount++;
                    fixedRects += n;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ScrollViewFixer] Исправлено ScrollRect: {fixedRects}. Сцен изменено: {sceneCount}, префабов: {prefabCount}.\n" +
                  (report.Length > 0 ? "Требуют ручной проверки:\n" + report : "Всё исправлено автоматически."));
    }

    private static int FixScene(string sceneName, StringBuilder report)
    {
        int count = 0;
        foreach (ScrollRect r in Object.FindObjectsOfType<ScrollRect>(true))
        {
            count += FixRect(r, sceneName + " / " + GetPath(r.transform), report) ? 1 : 0;
        }

        if (count > 0 && Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        return count;
    }

    private static int FixHierarchy(GameObject root, string prefabPath, StringBuilder report)
    {
        int count = 0;
        foreach (ScrollRect r in root.GetComponentsInChildren<ScrollRect>(true))
        {
            count += FixRect(r, prefabPath + " / " + GetPath(r.transform), report) ? 1 : 0;
        }
        return count;
    }

    // Возвращает true, если что-то изменилось
    private static bool FixRect(ScrollRect r, string where, StringBuilder report)
    {
        bool changed = false;
        Undo.RecordObject(r, "Fix ScrollRect");

        if (!r.enabled) { r.enabled = true; changed = true; }

        RectTransform self = r.GetComponent<RectTransform>();
        if (r.viewport == null)
        {
            Transform vp = self.Find("Viewport");
            r.viewport = vp != null ? vp.GetComponent<RectTransform>() : self;
            changed = true;
        }

        RectTransform vpRect = r.viewport;
        if (r.content == null)
        {
            Transform c = vpRect.Find("Content");
            if (c == null && vpRect.childCount > 0) c = vpRect.GetChild(0);
            if (c == null) c = self.childCount > 0 ? self.GetChild(0) : null;
            if (c != null && c.GetComponent<RectTransform>() != null)
            {
                r.content = c.GetComponent<RectTransform>();
                changed = true;
            }
        }

        if (r.movementType != ScrollRect.MovementType.Clamped || r.elasticity != 0f)
        {
            r.movementType = ScrollRect.MovementType.Clamped;
            r.elasticity = 0f;
            changed = true;
        }

        // Viewport должен ловить касания и обрезать содержимое
        Graphic vpGraphic = vpRect.GetComponent<Graphic>();
        if (vpGraphic == null)
        {
            Image img = vpRect.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0f);
            changed = true;
        }
        else if (!vpGraphic.raycastTarget)
        {
            vpGraphic.raycastTarget = true;
            changed = true;
        }

        if (vpRect.GetComponent<Mask>() == null && vpRect.GetComponent<RectMask2D>() == null)
        {
            vpRect.gameObject.AddComponent<RectMask2D>();
            changed = true;
        }

        // Content: высота должна зависеть от детей
        if (r.content != null && r.vertical)
        {
            RectTransform content = r.content;
            LayoutGroup layout = content.GetComponent<LayoutGroup>();
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (layout != null)
            {
                if (fitter == null)
                {
                    fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                    changed = true;
                }
                if (fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                {
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    changed = true;
                }
            }
            else
            {
                report.AppendLine($"[{where}] Content без Layout Group: высоту задайте вручную или добавьте Vertical Layout Group.");
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(r);
        }
        return changed;
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
