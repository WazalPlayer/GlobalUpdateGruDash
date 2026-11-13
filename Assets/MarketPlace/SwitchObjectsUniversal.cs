using UnityEngine;
using System.Collections.Generic;

public enum ActivationMode
{
    ByKey,          // по нажатию клавиши
    ByMouseClick,   // по клику на объект (нужен Collider)
    ByGUIButton     // через GUI-кнопку (OnGUI)
}

public class SwitchObjectsUniversal : MonoBehaviour
{
    [Header("Режим срабатывания")]
    public ActivationMode mode = ActivationMode.ByKey;

    [Header("Настройки для режима ByKey")]
    public KeyCode key = KeyCode.Space;

    [Header("Списки объектов")]
    public List<GameObject> objectsToDeactivate;
    public List<GameObject> objectsToActivate;

    // Флаг, чтобы не срабатывало каждый кадр при зажатой клавише
    private bool keyWasPressed = false;

    void Update()
    {
        if (mode == ActivationMode.ByKey)
        {
            if (Input.GetKeyDown(key))
            {
                Switch();
            }
        }
    }

    // Для клика по объекту (требуется Collider)
    void OnMouseDown()
    {
        if (mode == ActivationMode.ByMouseClick)
        {
            Switch();
        }
    }

    // Старый добрый GUI (рисуется поверх всего)
    void OnGUI()
    {
        if (mode == ActivationMode.ByGUIButton)
        {
            // Создаём кнопку размером 200x50 в центре экрана
            float x = (Screen.width - 200) / 2;
            float y = (Screen.height - 50) / 2;
            if (GUI.Button(new Rect(x, y, 200, 50), "Переключить объекты"))
            {
                Switch();
            }
        }
    }

    // Основной метод переключения
    public void Switch()
    {
        // Выключаем
        foreach (var obj in objectsToDeactivate)
            if (obj != null) obj.SetActive(false);

        // Включаем
        foreach (var obj in objectsToActivate)
            if (obj != null) obj.SetActive(true);

        Debug.Log("Объекты переключены!");
    }
}