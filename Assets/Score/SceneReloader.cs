using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SceneReloader : MonoBehaviour
{
    public Button reloadButton;

    void Start()
    {
        // Подписываемся в коде, а не в инспекторе
        if (reloadButton != null)
        {
            reloadButton.onClick.AddListener(ReloadScene);
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (reloadButton != null)
        {
            reloadButton.onClick.RemoveAllListeners();
        }
    }

    public void ReloadScene()
    {
        // Удаляем лишние EventSystem
        var eventSystems = FindObjectsOfType<EventSystem>();
        if (eventSystems.Length > 1)
        {
            Destroy(eventSystems[1].gameObject);
        }

        // Сбрасываем время (если игра была на паузе)
        Time.timeScale = 1f;

        // Перезагружаем сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}