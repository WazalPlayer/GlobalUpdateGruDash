using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonClicker : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneToLoad;  // Оставьте пустым для перезагрузки текущей
    public float delayBeforeLoad = 0.5f;
    public bool resetTimeScale = true;

    [Header("Button Settings")]
    public bool disableOnClick = true;
    public bool playSound = true;
    public AudioClip clickSound;

    [Header("Transition Effects")]
    public Animator fadeAnimator;
    public string fadeOutTrigger = "FadeOut";

    private Button button;
    private AudioSource audioSource;
    private bool isLoading = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null && playSound)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (isLoading) return;
        
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        isLoading = true;

        // 1. Восстановление Time.timeScale
        if (resetTimeScale)
            Time.timeScale = 1f;

        // 2. Блокировка кнопки
        if (disableOnClick)
            button.interactable = false;

        // 3. Воспроизведение звука
        if (playSound && clickSound != null)
            audioSource.PlayOneShot(clickSound);

        // 4. Эффект перехода
        if (fadeAnimator != null)
        {
            fadeAnimator.SetTrigger(fadeOutTrigger);
            yield return new WaitForSeconds(fadeAnimator.GetCurrentAnimatorStateInfo(0).length);
        }

        // 5. Задержка перед загрузкой
        if (delayBeforeLoad > 0)
            yield return new WaitForSecondsRealtime(delayBeforeLoad);

        // 6. Загрузка сцены
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            if (SceneUtility.GetBuildIndexByScenePath(sceneToLoad) >= 0)
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogError($"Scene '{sceneToLoad}' not found in Build Settings!");
            }
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClick);
    }

    // Автоматическое восстановление кнопки
    private void OnEnable()
    {
        if (button != null)
            button.interactable = true;
        isLoading = false;
    }
}