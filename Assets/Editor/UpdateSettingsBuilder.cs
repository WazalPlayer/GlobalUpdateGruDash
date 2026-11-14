using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Добавляет в выбранную в Hierarchy панель настроек блок «Обновления»:
// переключатель автопроверки, номер версии, информацию, кнопки «Проверить» и «Скачать».
// Запуск: выделите SettingsPanel в Hierarchy, затем Tools > Online > Add Update Settings to Selected Panel
public static class UpdateSettingsBuilder
{
    private static readonly Color Green = new Color(0.45f, 0.85f, 0.2f, 1f);

    [MenuItem("Tools/Online/Add Update Settings to Selected Panel")]
    public static void Build()
    {
        GameObject panel = Selection.activeGameObject;
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Настройки", "Выделите панель настроек (SettingsPanel) в Hierarchy.", "OK");
            return;
        }

        Transform old = panel.transform.Find("UpdateSettings");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject root = new GameObject("UpdateSettings", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Add Update Settings");
        root.transform.SetParent(panel.transform, false);
        SetRect(root.GetComponent<RectTransform>(), 0.1f, 0.05f, 0.9f, 0.25f);

        // Переключатель автопроверки (слева сверху)
        GameObject toggleGo = new GameObject("AutoUpdateToggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleGo.transform.SetParent(root.transform, false);
        SetRect(toggleGo.GetComponent<RectTransform>(), 0f, 0.55f, 0.08f, 0.95f);
        Image bg = toggleGo.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.9f);

        GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(toggleGo.transform, false);
        check.GetComponent<Image>().color = Green;
        SetRect(check.GetComponent<RectTransform>(), 0.2f, 0.2f, 0.8f, 0.8f);

        Toggle toggle = toggleGo.GetComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = true;

        Text label = CreateText("AutoUpdateLabel", root.transform, "Проверять обновления при запуске", 30, TextAnchor.MiddleLeft);
        SetRect(label.rectTransform, 0.1f, 0.55f, 0.6f, 0.95f);

        Text version = CreateText("VersionText", root.transform, "Версия: " + Application.version, 30, TextAnchor.MiddleRight);
        SetRect(version.rectTransform, 0.6f, 0.55f, 1f, 0.95f);

        Text info = CreateText("UpdateInfoText", root.transform, "", 26, TextAnchor.MiddleCenter);
        info.color = new Color(1f, 0.8f, 0.3f);
        SetRect(info.rectTransform, 0f, 0.25f, 1f, 0.5f);

        Button check2 = CreateButton("CheckUpdateButton", root.transform, "Проверить", Green);
        SetRect(check2.GetComponent<RectTransform>(), 0f, 0f, 0.45f, 0.2f);

        Button download = CreateButton("DownloadButton", root.transform, "Скачать", Green);
        SetRect(download.GetComponent<RectTransform>(), 0.55f, 0f, 1f, 0.2f);
        download.gameObject.SetActive(false);

        SettingsUpdateToggle script = root.AddComponent<SettingsUpdateToggle>();
        script.autoUpdateToggle = toggle;
        script.versionText = version;
        script.updateInfoText = info;
        script.checkButton = check2;
        script.downloadButton = download;

        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);
        Debug.Log("[UpdateSettingsBuilder] Блок «Обновления» добавлен в " + panel.name + " и сохранён.");
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

    private static Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;
        Text t = CreateText(name + "Label", go.transform, label, 30, TextAnchor.MiddleCenter);
        SetRect(t.rectTransform, 0, 0, 1, 1);
        return b;
    }

    private static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
