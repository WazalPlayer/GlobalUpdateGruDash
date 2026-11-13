using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoaderV2 : MonoBehaviour
{
    public Button loadSceneButton;
    public string sceneName;
    
    // Ссылки на объекты которые должны сохраниться
    public GameObject[] objectsToKeep;

    void Start()
    {
        if (loadSceneButton != null)
        {
            loadSceneButton.onClick.AddListener(LoadSceneV2);
        }
    }

    public void LoadSceneV2()
    {
        // Сохраняем все объекты
        foreach (GameObject obj in objectsToKeep)
        {
            if (obj != null)
                DontDestroyOnLoad(obj);
        }
        
        // Загружаем сцену
        SceneManager.LoadScene(sceneName);
    }

    [ContextMenu("AutoAssign")]
    void AutoAssign()
    {
        if (loadSceneButton == null)
            loadSceneButton = GetComponent<Button>();
    }
}