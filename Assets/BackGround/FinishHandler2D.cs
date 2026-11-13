using UnityEngine;
using System.Collections;

public class FinishHandler2D : MonoBehaviour
{
    [Header("Настройки телепорта")]
    [SerializeField] private bool enableTeleport = true;
    [Tooltip("Включить или выключить телепортацию объекта")]
    
    [Header("Новая позиция")]
    [SerializeField] private Vector2 newPosition = Vector2.zero;
    [Tooltip("Сохранять текущую Z координату при перемещении")]
    [SerializeField] private bool preserveZPosition = true;

    [Header("Объекты для ВЫКЛЮЧЕНИЯ")]
    [SerializeField] private GameObject[] objectsToDisable;
    [Tooltip("Деактивировать объекты сразу или с задержкой")]
    [SerializeField] private float disableDelay = 0f;

    [Header("Объекты для ВКЛЮЧЕНИЯ")]
    [SerializeField] private GameObject[] objectsToEnable;
    [Tooltip("Активировать объекты сразу или с задержкой")]
    [SerializeField] private float enableDelay = 0f;

    [Header("Настройки триггера")]
    [SerializeField] private bool useCollision = true;
    [SerializeField] private bool useTrigger = true;

    [Header("Повторное использование")]
    [SerializeField] private bool canReuse = true;
    [Tooltip("Задержка перед повторным использованием (секунды)")]
    [SerializeField] private float reuseDelay = 0.5f;
    private bool canTrigger = true;

    [Header("Состояние объектов при повторном использовании")]
    [SerializeField] private bool resetObjectsOnReuse = false;
    [SerializeField] private bool invertObjectsOnReuse = false;

    [Header("Эффекты и анимация")]
    [SerializeField] private bool useEffects = false;
    [SerializeField] private ParticleSystem teleportEffect;
    [SerializeField] private AudioClip teleportSound;

    private Vector2[] originalDisableStates; // Сохраняем исходные состояния объектов для выключения
    private Vector2[] originalEnableStates;  // Сохраняем исходные состояния объектов для включения

    private void Start()
    {
        // Сохраняем исходные состояния объектов
        SaveOriginalStates();
    }

    private void SaveOriginalStates()
    {
        // Сохраняем состояние объектов для выключения
        if (objectsToDisable != null && objectsToDisable.Length > 0)
        {
            originalDisableStates = new Vector2[objectsToDisable.Length];
            for (int i = 0; i < objectsToDisable.Length; i++)
            {
                if (objectsToDisable[i] != null)
                {
                    originalDisableStates[i] = new Vector2(
                        objectsToDisable[i].activeSelf ? 1 : 0,
                        objectsToDisable[i].activeInHierarchy ? 1 : 0
                    );
                }
            }
        }

        // Сохраняем состояние объектов для включения
        if (objectsToEnable != null && objectsToEnable.Length > 0)
        {
            originalEnableStates = new Vector2[objectsToEnable.Length];
            for (int i = 0; i < objectsToEnable.Length; i++)
            {
                if (objectsToEnable[i] != null)
                {
                    originalEnableStates[i] = new Vector2(
                        objectsToEnable[i].activeSelf ? 1 : 0,
                        objectsToEnable[i].activeInHierarchy ? 1 : 0
                    );
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canTrigger) return;
        
        if (useCollision && collision.gameObject.CompareTag("Finish"))
        {
            ExecuteFinishActions();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canTrigger) return;
        
        if (useTrigger && other.CompareTag("Finish"))
        {
            ExecuteFinishActions();
        }
    }

    private void ExecuteFinishActions()
    {
        if (!canTrigger) return;
        
        // Воспроизводим эффекты перед действиями
        PlayEffects();

        // Телепортируем объект, если включено
        if (enableTeleport)
        {
            TeleportObject();
        }

        // Управляем объектами (включаем/выключаем)
        ManageObjects();

        // Если можно повторно использовать, запускаем задержку
        if (canReuse)
        {
            StartCoroutine(ResetTriggerCooldown());
        }
        else
        {
            // Если нельзя повторно использовать, блокируем дальнейшие срабатывания
            canTrigger = false;
        }
    }

    private IEnumerator ResetTriggerCooldown()
    {
        canTrigger = false;
        yield return new WaitForSeconds(reuseDelay);
        canTrigger = true;
        
        // Применяем настройки для объектов при повторном использовании
        if (resetObjectsOnReuse)
        {
            ResetObjectsToOriginalState();
        }
        else if (invertObjectsOnReuse)
        {
            InvertObjectsState();
        }
    }

    private void PlayEffects()
    {
        if (!useEffects) return;

        // Воспроизводим партиклы
        if (teleportEffect != null)
        {
            ParticleSystem effect = Instantiate(teleportEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        // Воспроизводим звук
        if (teleportSound != null)
        {
            AudioSource.PlayClipAtPoint(teleportSound, transform.position);
        }
    }

    private void TeleportObject()
    {
        // Вычисляем конечную позицию
        Vector3 finalPosition;
        if (preserveZPosition)
        {
            finalPosition = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        }
        else
        {
            finalPosition = new Vector3(newPosition.x, newPosition.y, 0);
        }

        // Перемещаем объект
        transform.position = finalPosition;

        // Логируем действие (только в редакторе)
        #if UNITY_EDITOR
        Debug.Log($"Объект {gameObject.name} перемещен в позицию {finalPosition}");
        #endif
    }

    private void ManageObjects()
    {
        // Выключаем объекты
        if (objectsToDisable != null && objectsToDisable.Length > 0)
        {
            if (disableDelay > 0)
            {
                StartCoroutine(DisableObjectsWithDelay());
            }
            else
            {
                DisableObjects();
            }
        }

        // Включаем объекты
        if (objectsToEnable != null && objectsToEnable.Length > 0)
        {
            if (enableDelay > 0)
            {
                StartCoroutine(EnableObjectsWithDelay());
            }
            else
            {
                EnableObjects();
            }
        }
    }

    private IEnumerator DisableObjectsWithDelay()
    {
        yield return new WaitForSeconds(disableDelay);
        DisableObjects();
    }

    private IEnumerator EnableObjectsWithDelay()
    {
        yield return new WaitForSeconds(enableDelay);
        EnableObjects();
    }

    private void DisableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                #if UNITY_EDITOR
                Debug.Log($"Объект {obj.name} выключен");
                #endif
            }
        }
    }

    private void EnableObjects()
    {
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                #if UNITY_EDITOR
                Debug.Log($"Объект {obj.name} включен");
                #endif
            }
        }
    }

    private void ResetObjectsToOriginalState()
    {
        // Восстанавливаем исходное состояние объектов для выключения
        if (originalDisableStates != null && objectsToDisable != null)
        {
            for (int i = 0; i < Mathf.Min(objectsToDisable.Length, originalDisableStates.Length); i++)
            {
                if (objectsToDisable[i] != null)
                {
                    objectsToDisable[i].SetActive(originalDisableStates[i].x > 0.5f);
                }
            }
        }

        // Восстанавливаем исходное состояние объектов для включения
        if (originalEnableStates != null && objectsToEnable != null)
        {
            for (int i = 0; i < Mathf.Min(objectsToEnable.Length, originalEnableStates.Length); i++)
            {
                if (objectsToEnable[i] != null)
                {
                    objectsToEnable[i].SetActive(originalEnableStates[i].x > 0.5f);
                }
            }
        }
    }

    private void InvertObjectsState()
    {
        // Инвертируем состояние объектов для выключения
        if (objectsToDisable != null)
        {
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }

        // Инвертируем состояние объектов для включения
        if (objectsToEnable != null)
        {
            foreach (GameObject obj in objectsToEnable)
            {
                if (obj != null)
                {
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }
    }

    // Методы для ручного управления из других скриптов
    public void ManualTriggerFinish()
    {
        if (canTrigger)
        {
            ExecuteFinishActions();
        }
    }

    public void SetNewPosition(Vector2 position)
    {
        newPosition = position;
    }

    public void SetTeleportEnabled(bool enabled)
    {
        enableTeleport = enabled;
    }

    public void ToggleTeleport()
    {
        enableTeleport = !enableTeleport;
    }

    public bool IsTeleportEnabled()
    {
        return enableTeleport;
    }

    public void AddObjectToDisable(GameObject obj)
    {
        if (obj == null) return;
        
        // Добавляем объект в массив
        var list = new System.Collections.Generic.List<GameObject>();
        if (objectsToDisable != null)
        {
            list.AddRange(objectsToDisable);
        }
        list.Add(obj);
        objectsToDisable = list.ToArray();
        
        // Сохраняем его исходное состояние
        SaveOriginalStates();
    }

    public void AddObjectToEnable(GameObject obj)
    {
        if (obj == null) return;
        
        // Добавляем объект в массив
        var list = new System.Collections.Generic.List<GameObject>();
        if (objectsToEnable != null)
        {
            list.AddRange(objectsToEnable);
        }
        list.Add(obj);
        objectsToEnable = list.ToArray();
        
        // Сохраняем его исходное состояние
        SaveOriginalStates();
    }

    public void ForceResetTrigger()
    {
        canTrigger = true;
        StopAllCoroutines();
    }

    // Визуализация в редакторе
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Если телепорт отключен, не рисуем линии перемещения
        if (!enableTeleport) return;
        
        // Рисуем линию от текущей позиции к новой позиции
        Vector3 currentPos = transform.position;
        Vector3 targetPos = preserveZPosition 
            ? new Vector3(newPosition.x, newPosition.y, currentPos.z) 
            : new Vector3(newPosition.x, newPosition.y, 0);

        // Линия перемещения
        Gizmos.color = canTrigger ? Color.green : Color.gray;
        Gizmos.DrawLine(currentPos, targetPos);
        
        // Точка назначения
        Gizmos.color = canTrigger ? Color.yellow : Color.gray;
        Gizmos.DrawWireSphere(targetPos, 0.5f);

        // Текст с информацией о повторном использовании
        if (!canTrigger && canReuse)
        {
            UnityEditor.Handles.Label(targetPos + Vector3.up * 0.7f, $"Перезарядка: {reuseDelay}с");
        }

        // Область триггера (если есть коллайдер)
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.color = canTrigger ? new Color(1, 0.5f, 0, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
            if (collider is BoxCollider2D boxCollider)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            }
            else if (collider is CircleCollider2D circleCollider)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
        }
    }
    #endif
}