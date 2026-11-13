using UnityEngine;

public class LevelCompletionTrigger : MonoBehaviour
{
    [Header("Настройки разблокировки")]
    [Tooltip("Тег объекта для разблокировки")]
    public string unlockTag = "Item1";

    [Tooltip("Деактивировать после использования")]
    public bool disableAfterUse = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UnlockManager.Instance?.UnlockObject(unlockTag);
            
            if (disableAfterUse)
            {
                gameObject.SetActive(false);
            }
        }
    }
}