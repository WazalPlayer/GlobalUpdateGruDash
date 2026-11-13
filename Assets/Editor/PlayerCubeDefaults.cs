using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Принудительно ставит дефолтные настройки куба на все PlayerController:
// во всех сценах проекта и во всех префабах. Остальные поля не трогает.
// Запуск: Tools > Player > Reset Cube Defaults
public static class PlayerCubeDefaults
{
    // Только поля куба (имена совпадают с полями PlayerController)
    private static readonly string[] CubeFields =
    {
        "jumpForce",
        "cubeGravity",
        "cubeMaxFallSpeed",
        "cubeSpinSpeed",
        "holdToJump"
    };

    [MenuItem("Tools/Player/Reset Cube Defaults")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("Reset Cube Defaults",
            "Сбросить настройки куба на дефолтные во всех сценах и префабах?\n" +
            "Несохранённые изменения в открытой сцене будут предложены к сохранению.",
            "Да", "Отмена"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Dictionary<string, object> defaults = ReadDefaults();
        if (defaults == null)
            return;

        int sceneCount = 0;
        int prefabCount = 0;

        // Сцены
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            bool changed = false;
            foreach (PlayerController pc in Object.FindObjectsOfType<PlayerController>(true))
            {
                ApplyDefaults(pc, defaults);
                changed = true;
                sceneCount++;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        // Префабы
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PlayerController[] pcs = root.GetComponentsInChildren<PlayerController>(true);
                if (pcs.Length == 0) continue;

                foreach (PlayerController pc in pcs)
                {
                    ApplyDefaults(pc, defaults);
                    prefabCount++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PlayerCubeDefaults] Сброшено: сцены — {sceneCount}, префабы — {prefabCount}. " +
                  "Значения: " + string.Join(", ", defaults.Keys));
    }

    // Читает дефолтные значения из временного экземпляра (его поля заполняются инициализаторами класса)
    private static Dictionary<string, object> ReadDefaults()
    {
        GameObject temp = new GameObject("__PlayerCubeDefaults");
        try
        {
            PlayerController pc = temp.AddComponent<PlayerController>();
            SerializedObject so = new SerializedObject(pc);

            var result = new Dictionary<string, object>();
            foreach (string name in CubeFields)
            {
                SerializedProperty prop = so.FindProperty(name);
                if (prop == null)
                {
                    Debug.LogError($"[PlayerCubeDefaults] Поле '{name}' не найдено в PlayerController");
                    return null;
                }

                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Float: result[name] = prop.floatValue; break;
                    case SerializedPropertyType.Boolean: result[name] = prop.boolValue; break;
                    default:
                        Debug.LogError($"[PlayerCubeDefaults] Тип поля '{name}' не поддерживается");
                        return null;
                }
            }
            return result;
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    private static void ApplyDefaults(PlayerController pc, Dictionary<string, object> defaults)
    {
        SerializedObject so = new SerializedObject(pc);
        foreach (var kv in defaults)
        {
            SerializedProperty prop = so.FindProperty(kv.Key);
            if (prop == null) continue;

            if (kv.Value is float f) prop.floatValue = f;
            else if (kv.Value is bool b) prop.boolValue = b;
        }
        so.ApplyModifiedProperties();

        // Rigidbody2D куба: дискретная проверка столкновений и без интерполяции (дефолт)
        Rigidbody2D rb = pc.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            rb.interpolation = RigidbodyInterpolation2D.None;
            EditorUtility.SetDirty(rb);
        }

        EditorUtility.SetDirty(pc);
    }
}
