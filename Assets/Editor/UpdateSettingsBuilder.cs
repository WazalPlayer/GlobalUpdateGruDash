using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Добавляет в выбранную в Hierarchy панель настроек переключатель «Автообновление» и номер версии.
// Запуск: выделите SettingsPanel в Hierarchy, затем Tools > Online > Add Update Settings
public static class UpdateSettingsBuilder
{
    [MenuItem("Tools/Online/Add Update Settings to Selected Panel")]
    public static void Build()
    {
        GameObject panel = Selection.activeGameObject;
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Настройки", "Выделите панель настроек (SettingsPanel) в Hierarchy.", "OK");
            return;
        }

        // Корень строки настроек
        Transform oldRoot = panel.transform.Find("UpdateSettings");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot.gameObject);

        GameObject root = new GameObject("UpdateSettings", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Add Update Settings");
        root.transform.SetParent(panel.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.1f, 0.05f);
        rootRect.anchorMax = new Vector2(0.9f, 0.2f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Переключатель
        GameObject toggleGo = new GameObject("AutoUpdateToggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleGo.transform.SetParent(root.transform, false);
        RectTransform toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 0.5f);
        toggleRect.anchorMax = new Vector2(0f, 0.5f);
        toggleRect.pivot = new Vector2(0f, 0.5f);
        toggleRect.sizeDelta = new Vector2(60, 60);
        toggleRect.anchoredPosition = new Vector2(0, 0);
        Image bg = toggleGo.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.9f);

        GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(toggleGo.transform, false);
        check.GetComponent<Image>().color = new Color(0.45f, 0.85f, 0.2f);
        RectTransform checkRect = check.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        Toggle toggle = toggleGo.GetComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = true;

        // Подпись переключателя
        Text label = CreateText("AutoUpdateLabel", root.transform, "Автообновление", 34, TextAnchor.MiddleLeft);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0.6f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(0, 60);
        labelRect.anchoredPosition = new Vector2(80, 0);

        // Версия
        Text version = CreateText("VersionText", root.transform, "Версия: " + Application.version, 30, TextAnchor.MiddleRight);
        RectTransform versionRect = version.rectTransform;
        versionRect.anchorMin = new Vector2(0.6f, 0.5f);
        versionRect.anchorMax = new Vector2(1f, 0.5f);
        versionRect.pivot = new Vector2(1f, 0.5f);
        versionRect.sizeDelta = new Vector2(0, 60);
        versionRect.anchoredPosition = new Vector2(0, 0);

        // Строка с информацией об обновлении (появляется, если есть новая версия)
        Text info = CreateText("UpdateInfoText", root.transform, "", 26, TextAnchor.MiddleCenter);
        RectTransform infoRect = info.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 0f);
        infoRect.anchorMax = new Vector2(1f, 0.4f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;
        info.color = new Color(1f, 0.8f, 0.3f);

        SettingsUpdateToggle script = root.AddComponent<SettingsUpdateToggle>();
        script.autoUpdateToggle = toggle;
        script.versionText = version;
        script.updateInfoText = info;

        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);
        Debug.Log("[UpdateSettingsBuilder] Добавлено: переключатель автообновления и версия в " + panel.name);
    }

    private static Text CreateText(string name, Transform parent, string text, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        return t;
    }
}
