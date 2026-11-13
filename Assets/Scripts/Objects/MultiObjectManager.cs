using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MultiObjectManager : MonoBehaviour
{
    [System.Serializable]
    public class ObjectGroup
    {
        public string groupName;
        public GameObject[] objectsToShow;   // Объекты для показа
        public GameObject[] objectsToHide;   // Объекты для скрытия
    }
    
    [Header("Группы объектов")]
    public ObjectGroup[] objectGroups;
    
    [Header("Кнопки")]
    public Button[] toggleButtons; // Кнопки для каждой группы
    
    private void Start()
    {
        // Назначаем обработчики для всех кнопок
        for (int i = 0; i < toggleButtons.Length && i < objectGroups.Length; i++)
        {
            int index = i; // Важно: создаем локальную копию для замыкания
            toggleButtons[i].onClick.AddListener(() => ToggleGroup(index));
        }
    }
    
    private void ToggleGroup(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= objectGroups.Length)
            return;
            
        ObjectGroup group = objectGroups[groupIndex];
        
        // Показываем нужные объекты
        foreach (GameObject obj in group.objectsToShow)
        {
            if (obj != null)
                obj.SetActive(true);
        }
        
        // Скрываем ненужные объекты
        foreach (GameObject obj in group.objectsToHide)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        // Убираем все обработчики
        foreach (Button button in toggleButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}