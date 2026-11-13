using UnityEngine;

public class EditorObject : MonoBehaviour
{
    void Start()
    {
        // Помечаем объект для редактора
        gameObject.tag = "EditorObject";
    }
}