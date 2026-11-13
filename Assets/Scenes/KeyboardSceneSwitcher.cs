using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class KeyboardSceneSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class SceneHotkey
    {
        public KeyCode key;
        public string sceneName;
        public bool requireModifier = false;
        public KeyCode modifierKey = KeyCode.LeftControl;
        public float delay = 0f;
    }

    [Header("Настройки горячих клавиш")]
    public List<SceneHotkey> sceneHotkeys = new List<SceneHotkey>();

    [Header("Общие настройки")]
    public bool resetTimeScale = true;
    public bool showDebugMessages = true;

    private void Update()
    {
        CheckHotkeys();
    }

    private void CheckHotkeys()
    {
        foreach (var hotkey in sceneHotkeys)
        {
            bool modifierPressed = !hotkey.requireModifier || Input.GetKey(hotkey.modifierKey);
            
            if (modifierPressed && Input.GetKeyDown(hotkey.key))
            {
                if (hotkey.delay > 0)
                {
                    StartCoroutine(LoadSceneWithDelay(hotkey.sceneName, hotkey.delay));
                }
                else
                {
                    LoadScene(hotkey.sceneName);
                }
            }
        }
    }

    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        LoadScene(sceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (resetTimeScale)
            Time.timeScale = 1f;

        if (string.IsNullOrEmpty(sceneName))
        {
            if (showDebugMessages)
                Debug.Log("Перезагрузка текущей сцены");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0)
        {
            if (showDebugMessages)
                Debug.Log($"Загрузка сцены: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Сцена '{sceneName}' не найдена в Build Settings!");
        }
    }

    // Метод для добавления горячих клавиш из других скриптов
    public void AddHotkey(KeyCode key, string sceneName, bool needModifier = false, KeyCode modifier = KeyCode.LeftControl, float delay = 0f)
    {
        sceneHotkeys.Add(new SceneHotkey {
            key = key,
            sceneName = sceneName,
            requireModifier = needModifier,
            modifierKey = modifier,
            delay = delay
        });
    }
}