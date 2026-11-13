using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class UltimateSceneLoader : MonoBehaviour
{
    [Header("Настройки сцены")]
    [Tooltip("Имя сцены для загрузки")]
    [SerializeField] private string targetSceneName = "Level_1";
    [Tooltip("Использовать асинхронную загрузку")]
    [SerializeField] private bool useAsync = false;
    [Tooltip("Задержка перед загрузкой")]
    [SerializeField] private float delayBeforeLoad = 0f;
    [Tooltip("Сбросить Time.timeScale")]
    [SerializeField] private bool resetTimeScale = true;

    [Header("Визуальные эффекты")]
    [SerializeField] private Animator fadeAnimator;
    [SerializeField] private string fadeTrigger = "FadeOut";

    [Header("Дополнительные компоненты")]
    [SerializeField] private ObjectToggler objectToggler;

    private Button button;
    private static bool isLoading = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);

        if (objectToggler == null)
            objectToggler = GetComponent<ObjectToggler>();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        isLoading = true;

        // 1. Управление объектами
        if (objectToggler != null)
            objectToggler.ToggleObjects();

        // 2. Проверка сцены
        if (!IsSceneValid(targetSceneName))
        {
            Debug.LogError($"Сцена '{targetSceneName}' не найдена!");
            isLoading = false;
            yield break;
        }

        // 3. Сброс временной шкалы
        if (resetTimeScale)
            Time.timeScale = 1f;

        // 4. Эффекты перехода
        if (fadeAnimator != null && !string.IsNullOrEmpty(fadeTrigger))
        {
            fadeAnimator.SetTrigger(fadeTrigger);
            yield return new WaitForSeconds(fadeAnimator.GetCurrentAnimatorStateInfo(0).length);
        }

        // 5. Задержка перед загрузкой
        if (delayBeforeLoad > 0)
            yield return new WaitForSecondsRealtime(delayBeforeLoad);

        // 6. Загрузка сцены
        if (useAsync)
        {
            yield return StartCoroutine(LoadAsyncScene());
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }

        isLoading = false;
    }

    private IEnumerator LoadAsyncScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            if (asyncLoad.progress >= 0.9f)
                asyncLoad.allowSceneActivation = true;

            yield return null;
        }
    }

    private bool IsSceneValid(string sceneName)
    {
        #if UNITY_EDITOR
        foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
        {
            if (scene.path.Contains(sceneName + ".unity"))
                return true;
        }
        return false;
        #else
        return SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0;
        #endif
    }
}