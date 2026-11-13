using UnityEngine;
using UnityEngine.SceneManagement;

public class Finish : MonoBehaviour
{
    [Header("Настройки")]
    public int starsToAdd = 10; // Количество звёзд за уровень
    public string menuSceneName = "MainMenu"; // Название сцены меню
    public float loadDelay = 1f; // Задержка перед загрузкой меню

    [Header("Эффекты")]
    public ParticleSystem finishEffect;
    public AudioClip finishSound;

    private bool isFinished = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isFinished) return;
        
        if (other.CompareTag("Player"))
        {
            FinishLevel();
        }
    }

    private void FinishLevel()
    {
        isFinished = true;
        
        // 1. Добавляем звёзды
        AddStars();
        
        // 2. Проигрываем эффекты
        PlayEffects();
        
        // 3. Переходим в меню с задержкой
        Invoke("LoadMenu", loadDelay);
    }

    private void AddStars()
    {
        // Получаем StarManager (убедитесь, что он есть на сцене)
        StarManager starManager = FindObjectOfType<StarManager>();
        
        if (starManager != null)
        {
            starManager.AddStars(starsToAdd);
            Debug.Log($"Добавлено {starsToAdd} звёзд. Всего: {starManager.GetTotalStars()}");
        }
        else
        {
            Debug.LogWarning("StarManager не найден на сцене!");
        }
    }

    private void PlayEffects()
    {
        // Визуальный эффект
        if (finishEffect != null)
        {
            finishEffect.Play();
        }
        
        // Звуковой эффект
        if (finishSound != null)
        {
            AudioSource.PlayClipAtPoint(finishSound, Camera.main.transform.position);
        }
    }

    private void LoadMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    // Для вызова из анимации или других скриптов
    public void ForceFinish()
    {
        if (!isFinished)
        {
            FinishLevel();
        }
    }
}