using UnityEngine;

public class PointCollector : MonoBehaviour
{
    [Header("Настройки очков")]
    public int pointsValue = 1; // Количество очков за сбор
    public bool destroyOnCollect = true; // Уничтожать ли объект после сбора
    
    [Header("Эффекты")]
    public GameObject collectEffect; // Эффект при сборе (опционально)
    public AudioClip collectSound; // Звук при сборе (опционально)
    
    void OnTriggerEnter(Collider other)
    {
        // Проверяем тег объекта
        if (other.CompareTag("Player"))
        {
            CollectPoints(other.gameObject);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Проверяем тег объекта
        if (collision.gameObject.CompareTag("Player"))
        {
            CollectPoints(collision.gameObject);
        }
    }
    
    private void CollectPoints(GameObject player)
    {
        // Добавляем очки через ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointsValue);
        }
        else
        {
            Debug.LogWarning("ScoreManager не найден!");
        }
        
        // Воспроизводим эффект
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, transform.rotation);
        }
        
        // Воспроизводим звук
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Уничтожаем объект если нужно
        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            // Можно отключить коллайдер вместо уничтожения
            GetComponent<Collider>().enabled = false;
            GetComponent<Renderer>().enabled = false;
        }
        
        Debug.Log($"Собрано {pointsValue} очков!");
    }
    
    // Метод для изменения значения очков через код
    public void SetPointsValue(int newValue)
    {
        pointsValue = newValue;
    }
}