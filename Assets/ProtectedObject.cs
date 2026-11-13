using UnityEngine;

public class ProtectedObject : MonoBehaviour
{
    [Tooltip("Скрыть объект вместо удаления")]
    public bool hideInsteadOfDestroy = true;
    
    [Tooltip("Показывать предупреждение при попытке удаления")]
    public bool showWarning = true;
    
    private void OnDestroy()
    {
        if (hideInsteadOfDestroy)
        {
            // Отменяем удаление
            Debug.LogWarning($"Попытка удаления защищенного объекта: {gameObject.name}");
            
            // Скрываем объект вместо удаления
            gameObject.SetActive(false);
            
            // Предотвращаем уничтожение
            if (Application.isPlaying)
            {
                Debug.Log($"Объект {gameObject.name} скрыт вместо удаления");
                
                if (showWarning)
                {
                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayDialog("Защита объекта", 
                        $"Объект '{gameObject.name}' защищен от удаления и был скрыт вместо этого.", "OK");
                    #endif
                }
            }
        }
    }
    
    // Метод для безопасного удаления (если действительно нужно)
    public void SafeDestroy()
    {
        hideInsteadOfDestroy = false;
        Destroy(gameObject);
    }
    
    // Метод для показа объекта
    public void ShowObject()
    {
        gameObject.SetActive(true);
    }
}