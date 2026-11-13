using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToMainMenu : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Название сцены главного меню (по умолчанию 'MainMenu')")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Нужно ли подтверждение перед выходом?")]
    public bool requireConfirmation = true;

    [Tooltip("Задержка перед загрузкой меню (секунды)")]
    public float delayBeforeLoad = 0.5f;

    private bool isWaitingForConfirmation = false;

    private void Update()
    {
        // Если нажат ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (requireConfirmation && !isWaitingForConfirmation)
            {
                Debug.Log("Нажмите ESC ещё раз для выхода в меню");
                isWaitingForConfirmation = true;
                Invoke("ResetConfirmation", 2f); // Сброс подтверждения через 2 секунды
            }
            else
            {
                LoadMainMenu();
            }
        }
    }

    private void ResetConfirmation()
    {
        isWaitingForConfirmation = false;
    }

    public void LoadMainMenu()
    {
        if (delayBeforeLoad > 0)
        {
            Invoke("LoadSceneDelayed", delayBeforeLoad);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void LoadSceneDelayed()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}