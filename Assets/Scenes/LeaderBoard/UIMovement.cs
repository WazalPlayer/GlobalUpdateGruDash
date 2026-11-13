using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveDistance = 100f; // Расстояние движения
    [SerializeField] private float moveDuration = 0.5f; // Длительность движения
    [SerializeField] private RectTransform objectToMove; // Объект для движения
    
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private Coroutine movementCoroutine;
    private bool isMoving = false;

    void Start()
    {
        // Сохраняем начальную позицию
        startPosition = objectToMove.anchoredPosition;
    }

    // Метод для движения вперед
    public void MoveForward()
    {
        if (isMoving) return;
        
        Vector2 newPosition = objectToMove.anchoredPosition + Vector2.right * moveDistance;
        StartMovement(newPosition);
    }

    // Метод для движения назад
    public void MoveBackward()
    {
        if (isMoving) return;
        
        Vector2 newPosition = objectToMove.anchoredPosition + Vector2.left * moveDistance;
        StartMovement(newPosition);
    }

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