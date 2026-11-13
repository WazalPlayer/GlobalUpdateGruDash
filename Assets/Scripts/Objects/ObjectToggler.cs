using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ObjectToggler : MonoBehaviour
{
    [Header("Настройки объектов")]
    [Tooltip("Объекты для активации")]
    [SerializeField] private List<GameObject> objectsToEnable = new List<GameObject>();
    [Tooltip("Объекты для деактивации")]
    [SerializeField] private List<GameObject> objectsToDisable = new List<GameObject>();
    
    [Header("Тайминги")]
    [Tooltip("Задержка перед активацией")]
    [SerializeField] private float enableDelay = 0f;
    [Tooltip("Задержка перед деактивацией")]
    [SerializeField] private float disableDelay = 0f;

    public void ToggleObjects()
    {
        StartCoroutine(ToggleObjectsRoutine());
    }

    private IEnumerator ToggleObjectsRoutine()
    {
        // Деактивация объектов
        if (disableDelay > 0)
            yield return new WaitForSeconds(disableDelay);

        foreach (var obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Активация объектов
        if (enableDelay > 0)
            yield return new WaitForSeconds(enableDelay);

        foreach (var obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    // API для динамического управления
    public void AddObjectToEnable(GameObject obj) => AddObjectToList(obj, objectsToEnable);
    public void AddObjectToDisable(GameObject obj) => AddObjectToList(obj, objectsToDisable);
    public void RemoveObjectToEnable(GameObject obj) => RemoveObjectFromList(obj, objectsToEnable);
    public void RemoveObjectToDisable(GameObject obj) => RemoveObjectFromList(obj, objectsToDisable);

    private void AddObjectToList(GameObject obj, List<GameObject> list)
    {
        if (obj != null && !list.Contains(obj))
            list.Add(obj);
    }

    private void RemoveObjectFromList(GameObject obj, List<GameObject> list)
    {
        if (obj != null && list.Contains(obj))
            list.Remove(obj);
    }
}