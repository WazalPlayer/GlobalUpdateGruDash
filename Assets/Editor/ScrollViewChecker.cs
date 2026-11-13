using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Проверяет все ScrollRect в открытой сцене и пишет в Console, почему они не прокручиваются.
// Запуск: Tools > UI > Check ScrollViews in Open Scene
public static class ScrollViewChecker
{
    [MenuItem("Tools/UI/Check ScrollViews in Open Scene")]
    public static void Check()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== ScrollView check: {EditorSceneManager.GetActiveScene().name} ===");

        ScrollRect[] rects = Object.FindObjectsOfType<ScrollRect>(true);
        if (rects.Length == 0) sb.AppendLine("ScrollRect в сцене не найдено.");

        foreach (ScrollRect r in rects)
        {
            string path = GetPath(r.transform);
            sb.AppendLine($"\n[{path}]");
            sb.AppendLine($"  enabled={r.enabled}, active={r.gameObject.activeInHierarchy}, vertical={r.vertical}, horizontal={r.horizontal}");
            sb.AppendLine($"  movementType={r.movementType}, elasticity={r.elasticity}, inertia={r.inertia}");

            if (r.viewport == null) sb.AppendLine("  ПРОБЛЕМА: Viewport не назначен");
            if (r.content == null)
            {
                sb.AppendLine("  ПРОБЛЕМА: Content не назначен");
                continue;
            }

            RectTransform vp = r.viewport != null ? r.viewport : r.GetComponent<RectTransform>();
            float vpH = vp.rect.height;
            float ctH = r.content.rect.height;
            sb.AppendLine($"  viewport высота={vpH:0}, content высота={ctH:0}");

            if (ctH <= vpH + 1f)
                sb.AppendLine("  ПРОБЛЕМА: content не выше viewport — прокручивать нечего. Нужны ContentSizeFitter (Vertical: Preferred Size) и Layout Group на Content, либо задайте высоту Content.");

            Graphic vpGraphic = vp.GetComponent<Graphic>();
            if (vpGraphic == null)
                sb.AppendLine("  ПРОБЛЕМА: у Viewport нет Image/Graphic — не на что нажать для прокрутки. Добавьте Image с альфой 0 и Raycast Target.");
            else if (!vpGraphic.raycastTarget)
                sb.AppendLine("  ПРОБЛЕМА: у Viewport выключен Raycast Target — свайп не ловится.");

            if (vp.GetComponent<Mask>() == null && vp.GetComponent<RectMask2D>() == null)
                sb.AppendLine("  ПРОБЛЕМА: у Viewport нет Mask/RectMask2D — контент не обрезается.");

            // Что перекрывает область списка: Graphic с raycast выше по иерархии/порядку
            Canvas canvas = r.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(false))
                {
                    if (g.raycastTarget && g.transform != vp && g.transform.IsChildOf(canvas.transform) &&
                        IsOver(g.rectTransform, vp) && g.transform.GetSiblingIndex() > 0 && !g.transform.IsChildOf(r.transform))
                    {
                        sb.AppendLine($"  Возможно перекрывает: {GetPath(g.transform)} (raycastTarget=on)");
                    }
                }
            }
        }

        Debug.Log(sb.ToString());
    }

    private static bool IsOver(RectTransform a, RectTransform b)
    {
        Rect ra = RectFromCorners(a);
        Rect rb = RectFromCorners(b);
        return ra.Overlaps(rb);
    }

    private static Rect RectFromCorners(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
