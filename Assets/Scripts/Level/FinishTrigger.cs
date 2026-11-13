using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    [SerializeField] private int starsToAdd = 1; // Сколько звёзд даётся за уровень

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StarManager.Instance.AddStars(starsToAdd);
            
            // Здесь можно добавить другие эффекты завершения уровня
            Debug.Log($"Level complete! Added {starsToAdd} star(s)");
            
            // Отключаем объект, чтобы нельзя было повторно получить звёзды
            gameObject.SetActive(false);
        }
    }
}