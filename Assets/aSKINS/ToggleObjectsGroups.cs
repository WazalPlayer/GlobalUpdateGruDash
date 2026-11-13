using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ToggleObjectsGroups : MonoBehaviour
{
    [Header("Объекты для СКРЫТИЯ при нажатии")]
    public List<GameObject> objectsToHide;
    
    [Header("Объекты для ПОКАЗА при нажатии")]
    public List<GameObject> objectsToShow;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("Скрипт должен быть на кнопке!");
        }
    }

    void OnButtonClick()
    {
        // Скрываем все объекты из первого списка
        foreach (GameObject obj in objectsToHide)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // Показываем все объекты из второго списка
        foreach (GameObject obj in objectsToShow)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        Debug.Log("Объекты скрыты и показаны");
    }
}