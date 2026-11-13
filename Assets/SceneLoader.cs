using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Настройки сцены")]
    public string sceneName; // Имя сцены для загрузки
    public int sceneBuildIndex = -1; // Индекс сцены в Build Settings
    
    [Header("Режим загрузки")]
    public bool useAsyncLoading = false; // Асинхронная загрузка
    public LoadSceneMode loadMode = LoadSceneMode.Single; // Режим загрузки
    
    void Start()
    {
        // Автоматически находим кнопку и назначаем обработчик
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(LoadTargetScene);
        }
        else
        {
            Debug.LogWarning("SceneLoader: На объекте нет компонента Button! Скрипт можно использовать через другие методы.");
        }
    }

    // Основной метод загрузки сцены
    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            LoadSceneByName(sceneName);
        }
        else if (sceneBuildIndex >= 0)
        {
            LoadSceneByIndex(sceneBuildIndex);
        }
        else
        {
            Debug.LogError("SceneLoader: Не указано имя сцены или build index!");
        }
    }

    // Загрузка по имени сцены
    public void LoadSceneByName(string name)
    {
        if (useAsyncLoading)
        {
            SceneManager.LoadSceneAsync(name, loadMode);
        }
        else
        {
            SceneManager.LoadScene(name, loadMode);
        }
        Debug.Log("Загружаем сцену: " + name);
    }

    // Загрузка по индексу сцены
    public void LoadSceneByIndex(int buildIndex)
    {
        if (useAsyncLoading)
        {
            SceneManager.LoadSceneAsync(buildIndex, loadMode);
        }
        else
        {
            SceneManager.LoadScene(buildIndex, loadMode);
        }
        Debug.Log("Загружаем сцену с индексом: " + buildIndex);
    }

    // Загрузка с задержкой
    public void LoadSceneWithDelay(float delay)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Invoke("LoadSceneByNameWithDelay", delay);
        }
        else if (sceneBuildIndex >= 0)
        {
            Invoke("LoadSceneByIndexWithDelay", delay);
        }
    }

    private void LoadSceneByNameWithDelay()
    {
        LoadSceneByName(sceneName);
    }

    private void LoadSceneByIndexWithDelay()
    {
        LoadSceneByIndex(sceneBuildIndex);
    }

    // Статические методы для вызова из других скриптов
    public static void LoadSceneStatic(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static void LoadSceneStatic(int buildIndex)
    {
        SceneManager.LoadScene(buildIndex);
    }
}