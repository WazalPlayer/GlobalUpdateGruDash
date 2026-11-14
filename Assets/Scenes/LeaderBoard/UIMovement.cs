using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Двигает НЕСКОЛЬКО объектов синхронно: по кнопкам Next / Previous (MoveForward / MoveBackward).
// Список пунктов: objectsToMove. Нельзя уйти дальше последнего и левее первого.
public class UIMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveDistance = 100f;   // на сколько сдвигается каждый объект за шаг
    [SerializeField] private float moveDuration = 0.5f;   // длительность движения

    [Header("Объекты, которые двигаются вместе")]
    [SerializeField] private RectTransform[] objectsToMove;

    [Tooltip("Сколько пунктов в списке. 0 = равно количеству объектов в objectsToMove")]
    [SerializeField] private int itemCount = 0;

    private Vector2[] startPositions;
    private Coroutine movementCoroutine;
    private bool isMoving = false;
    private int currentIndex = 0;

    private int ItemCount => itemCount > 0 ? itemCount : (objectsToMove != null ? objectsToMove.Length : 0);

    void Start()
    {
        // Запоминаем стартовые позиции каждого объекта
        if (objectsToMove == null) return;
        startPositions = new Vector2[objectsToMove.Length];
        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] != null) startPositions[i] = objectsToMove[i].anchoredPosition;
        }
    }

    // Next: вперёд, но не дальше последнего пункта
    public void MoveForward()
    {
        if (isMoving || currentIndex >= ItemCount - 1) return;
        currentIndex++;
        StartMovement(Vector2.right * moveDistance);
    }

    // Previous: назад, но не левее первого пункта
    public void MoveBackward()
    {
        if (isMoving || currentIndex <= 0) return;
        currentIndex--;
        StartMovement(Vector2.left * moveDistance);
    }

    // Удобные имена для кнопок
    public void Next() => MoveForward();
    public void Previous() => MoveBackward();

    public void MoveUp()
    {
        if (isMoving) return;
        StartMovement(Vector2.up * moveDistance, keepIndex: true);
    }

    public void MoveDown()
    {
        if (isMoving) return;
        StartMovement(Vector2.down * moveDistance, keepIndex: true);
    }

    // Вернуть все объекты на стартовые места и первый пункт
    public void ResetPosition()
    {
        if (isMoving || startPositions == null) return;
        currentIndex = 0;
        StartReset();
    }

    // Можно ли сейчас листать (для гашения кнопок)
    public bool CanMoveForward => !isMoving && currentIndex < ItemCount - 1;
    public bool CanMoveBackward => !isMoving && currentIndex > 0;

    // ---------- движение ----------

    private void StartMovement(Vector2 delta, bool keepIndex = false)
    {
        if (objectsToMove == null) return;
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MoveRoutine(delta));
    }

    private void StartReset()
    {
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(ResetRoutine());
    }

    // Все объекты едут одновременно на одинаковый шаг
    private IEnumerator MoveRoutine(Vector2 delta)
    {
        isMoving = true;

        int n = objectsToMove.Length;
        Vector2[] from = new Vector2[n];
        Vector2[] to = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            if (objectsToMove[i] == null) continue;
            from[i] = objectsToMove[i].anchoredPosition;
            to[i] = from[i] + delta;
        }

        yield return Animate(from, to);
        isMoving = false;
        movementCoroutine = null;
    }

    // Возврат каждого объекта на его стартовую позицию
    private IEnumerator ResetRoutine()
    {
        isMoving = true;

        int n = objectsToMove.Length;
        Vector2[] from = new Vector2[n];
        Vector2[] to = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            if (objectsToMove[i] == null) continue;
            from[i] = objectsToMove[i].anchoredPosition;
            to[i] = startPositions[i];
        }

        yield return Animate(from, to);
        isMoving = false;
        movementCoroutine = null;
    }

    private IEnumerator Animate(Vector2[] from, Vector2[] to)
    {
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
            for (int i = 0; i < objectsToMove.Length; i++)
            {
                if (objectsToMove[i] != null)
                    objectsToMove[i].anchoredPosition = Vector2.Lerp(from[i], to[i], progress);
            }
            yield return null;
        }

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] != null) objectsToMove[i].anchoredPosition = to[i];
        }
    }
}
