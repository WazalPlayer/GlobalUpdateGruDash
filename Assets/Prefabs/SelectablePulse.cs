using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectablePulse : MonoBehaviour
{
    [Header("Настройки выделения")]
    public Color pulseColor = Color.cyan;
    public float pulseSpeed = 3f;
    public KeyCode selectKey = KeyCode.Mouse0;
    
    [Header("Автонастройки")]
    public Camera mainCamera;
    public LayerMask selectableLayer = 1;
    
    private GameObject selectedObject;
    private Coroutine pulseCoroutine;
    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    
    void Update()
    {
        // Выбор объекта по клику
        if (Input.GetKeyDown(selectKey))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, selectableLayer))
            {
                SelectObject(hit.collider.gameObject);
            }
            else
            {
                DeselectObject();
            }
        }
    }
    
    public void SelectObject(GameObject obj)
    {
        // Снимаем выделение с предыдущего объекта
        DeselectObject();
        
        selectedObject = obj;
        StartPulseEffect(obj);
    }
    
    public void DeselectObject()
    {
        if (selectedObject != null)
        {
            StopPulseEffect(selectedObject);
            selectedObject = null;
        }
    }
    
    private void StartPulseEffect(GameObject obj)
    {
        // Сохраняем оригинальный цвет
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && !originalColors.ContainsKey(obj))
        {
            originalColors[obj] = renderer.material.color;
        }
        
        // Запускаем корутину переливания
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
            
        pulseCoroutine = StartCoroutine(PulseEffect(obj));
    }
    
    private void StopPulseEffect(GameObject obj)
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Восстанавливаем оригинальный цвет
        if (originalColors.ContainsKey(obj))
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = originalColors[obj];
            }
            originalColors.Remove(obj);
        }
    }
    
    private IEnumerator PulseEffect(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) yield break;
        
        Color originalColor = originalColors[obj];
        
        while (true)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            renderer.material.color = Color.Lerp(originalColor, pulseColor, t);
            yield return null;
        }
    }
}