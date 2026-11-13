using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelTransition : MonoBehaviour
{
    public GameObject interactionPanel; // Панель с кнопками (должна быть настроена в инспекторе)
    public string nextSceneName = "NextLevel"; // Имя следующей сцены
    public string menuSceneName = "MainMenu"; // Имя сцены меню

    private bool isPlayerInRange = false;

    private void Start()
    {
        // Скрываем панель при старте
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Если игрок в зоне и нажал клавишу взаимодействия (например E)
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ShowInteractionPanel();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            // Можно показать подсказку, что нужно нажать E
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            HideInteractionPanel();
        }
    }

    private void ShowInteractionPanel()
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
            // Останавливаем время в игре (если нужно)
            Time.timeScale = 0f;
        }
    }

    private void HideInteractionPanel()
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
            // Возобновляем время в игре
            Time.timeScale = 1f;
        }
    }

    // Метод для кнопки "Следующий уровень"
    public void LoadNextLevel()
    {
        Time.timeScale = 1f; // Восстанавливаем время перед загрузкой сцены
        SceneManager.LoadScene(nextSceneName);
    }

    // Метод для кнопки "В меню"
    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Восстанавливаем время перед загрузкой сцены
        SceneManager.LoadScene(menuSceneName);
    }
}