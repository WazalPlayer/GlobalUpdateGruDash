using UnityEngine;

public class SpeedChanger : MonoBehaviour
{
    public enum SpeedType { Slow, Normal, Fast, Faster, SuperFast }
    [SerializeField] private SpeedType speedType = SpeedType.Normal;
    
    // Задаем конкретные значения скорости для каждого типа
    [SerializeField] private float[] speedValues = { 3.5f, 5f, 6f, 7f, 8f };
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Устанавливаем скорость игрока в соответствии с выбранным типом
                player.moveSpeed = speedValues[(int)speedType];
                
                // Можно добавить визуальный эффект
                ShowEffect();
            }
        }
    }
    
    private void ShowEffect()
    {
        // Визуальный эффект при смене скорости
        Debug.Log($"Speed changed to: {speedValues[(int)speedType]}");
    }
    
    private void OnValidate()
    {
        // Визуальная настройка в редакторе
        string[] speedNames = { "Slow (3.5)", "Normal (5)", "Fast (6)", "Faster (7)", "SuperFast (8)" };
        gameObject.name = $"SpeedChanger_{speedNames[(int)speedType]}";
    }
}