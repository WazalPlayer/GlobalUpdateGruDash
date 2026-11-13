using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class UnbreakableButton : MonoBehaviour
{
    [Header("Настройки кнопки")]
    [Tooltip("Сцена для загрузки (оставьте пустым для текущей)")]
    public string targetScene;
    
    [Tooltip("Нужно ли выключать кнопку при нажатии")]
    public bool disableOnClick = true;
    
    [Tooltip("Восстанавливать Time.timeScale при нажатии")]
    public bool resetTimeScale = true;

    private Button button;
    private bool isSubscribed = false;

    private void Awake()
    {
        // Получаем компонент кнопки
        button = GetComponent<Button>();
        
        // Автоматическая подписка
        SubscribeToButton();
        
        // Обработчик перезагрузки сцены
        SceneManager.sceneLoaded += OnSceneReloaded;
    }

    private void OnDestroy()
    {
        // Отписываемся при уничтожении
        UnsubscribeFromButton();
        SceneManager.sceneLoaded -= OnSceneReloaded;
    }

    private void OnSceneReloaded(Scene scene, LoadSceneMode mode)
    {
        // При перезагрузке сцены повторно подписываемся
        SubscribeToButton();
    }

    private void SubscribeToButton()
    {
        // Если уже подписаны или кнопка не существует - выходим
        if (isSubscribed || button == null) return;
        
        button.onClick.AddListener(OnButtonClicked);
        isSubscribed = true;
    }

    private void UnsubscribeFromButton()
    {
        // Если не подписаны или кнопка не существует - выходим
        if (!isSubscribed || button == null) return;
        
        button.onClick.RemoveListener(OnButtonClicked);
        isSubscribed = false;
    }

    private void OnButtonClicked()
    {
        // Восстанавливаем временную шкалу если нужно
        if (resetTimeScale) Time.timeScale = 1f;
        
        // Выключаем кнопку если требуется
        if (disableOnClick && button != null) 
            button.interactable = false;
        
        // Загружаем сцену если указана
        if (!string.IsNullOrEmpty(targetScene))
        {
            if (SceneUtility.GetBuildIndexByScenePath(targetScene) >= 0)
            {
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                Debug.LogError($"Сцена {targetScene} не найдена в Build Settings!");
                if (button != null) button.interactable = true;
            }
        }
    }

    // Автоматически включаем кнопку если она была выключена
    private void OnEnable()
    {
        if (button != null) 
            button.interactable = true;
    }
}