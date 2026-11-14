using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveDistance = 100f; // Расстояние движения
    [SerializeField] private float moveDuration = 0.5f; // Длительность движения
    [SerializeField] private RectTransform objectToMove; // Объект для движения
    
    [Header("Границы списка (для выбора сложности/уровней)")]
    [Tooltip("Сколько пунктов в списке. 0 = взять количество дочерних объектов у objectToMove")]
    [SerializeField] private int itemCount = 0;

    private Vector2 startPosition;
    private Vector2 targetPosition;
    private Coroutine movementCoroutine;
    private bool isMoving = false;
    private int currentIndex = 0;

    private int ItemCount => itemCount > 0 ? itemCount : (objectToMove != null ? objectToMove.childCount : 0);

    void Start()
    {
        // Сохраняем начальную позицию
        startPosition = objectToMove.anchoredPosition;
    }

    // Вперёд: нельзя дальше последнего пункта
    public void MoveForward()
    {
        if (isMoving) return;
        if (currentIndex >= ItemCount - 1) return;

        currentIndex++;
        Vector2 newPosition = objectToMove.anchoredPosition + Vector2.right * moveDistance;
        StartMovement(newPosition);
    }

    // Назад: нельзя левее первого пункта
    public void MoveBackward()
    {
        if (isMoving) return;
        if (currentIndex <= 0) return;

        currentIndex--;
        Vector2 newPosition = objectToMove.anchoredPosition + Vector2.left * moveDistance;
        StartMovement(newPosition);
    }

    // Можно ли сейчас листать вперёд/назад (для скрытия/гашения кнопок)
    public bool CanMoveForward => !isMoving && currentIndex < ItemCount - 1;
    public bool CanMoveBackward => !isMoving && currentIndex > 0;

    // Метод для движения вверх
    public void MoveUp()
    {
        if (isMoving) return;
        
        Vector2 newPosition = objectToMove.anchoredPosition + Vector2.up * moveDistance;
        StartMovement(newPosition);
    }

    // Метод для движения вниз
    public void MoveDown()
    {
        if (isMoving) return;
        
        Vector2 newPosition = objectToMove.anchoredPosition + Vector2.down * moveDistance;
        StartMovement(newPosition);
    }

    // Метод для сброса в начальную позицию
    public void ResetPosition()
    {
        if (isMoving) return;
        currentIndex = 0;
        StartMovement(startPosition);
    }

    private void StartMovement(Vector2 targetPos)
    {
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);
            
        targetPosition = targetPos;
        movementCoroutine = StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        isMoving = true;
        Vector2 startPos = objectToMove.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsedTime / moveDuration);
            objectToMove.anchoredPosition = Vector2.Lerp(startPos, targetPosition, progress);
            yield return null;
        }

        objectToMove.anchoredPosition = targetPosition;
        isMoving = false;
        movementCoroutine = null;
    }
}