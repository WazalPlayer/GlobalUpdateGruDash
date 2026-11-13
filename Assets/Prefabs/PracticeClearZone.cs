using UnityEngine;

public class PracticeClearZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.ClearAllPracticePoints();
                Debug.Log("Practice points cleared by trigger");
                // После срабатывания можно удалить триггер, чтобы не срабатывал повторно
            }
        }
    }
}