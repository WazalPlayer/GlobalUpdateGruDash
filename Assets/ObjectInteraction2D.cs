using UnityEngine;

public class ObjectInteraction2D : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    public InteractionType interactionType = InteractionType.Push;
    
    [Header("Настройки отталкивания")]
    public float force1 = 5f;
    public float force2 = 10f;
    public float force3 = 15f;
    
    [Header("Настройки переворота")]
    public float flipDuration = 0.5f;
    
    private Rigidbody2D rb;
    private bool isUpsideDown = false;
    private bool isFlipping = false;
    
    public enum InteractionType
    {
        Push,
        Flip
    }
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D не найден на объекте!");
        }
    }
    
    void OnMouseDown()
    {
        if (isFlipping) return;
        
        switch (interactionType)
        {
            case InteractionType.Push:
                ApplyPushForce();
                break;
            case InteractionType.Flip:
                StartCoroutine(FlipObject());
                break;
        }
    }
    
    void ApplyPushForce()
    {
        if (rb == null) return;
        
        // Получаем позицию клика в мировых координатах
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        // Направление от объекта к точке клика
        Vector2 direction = (mousePos - transform.position).normalized;
        
        // Применяем силу в зависимости от количества нажатий
        float currentForce = GetCurrentForce();
        rb.AddForce(-direction * currentForce, ForceMode2D.Impulse);
        
        Debug.Log($"Применена сила: {currentForce} в направлении: {-direction}");
    }
    
    float GetCurrentForce()
    {
        // Можно изменить логику выбора силы
        // Сейчас просто рандомный выбор между тремя силами
        float[] forces = { force1, force2, force3 };
        return forces[Random.Range(0, forces.Length)];
    }
    
    System.Collections.IEnumerator FlipObject()
    {
        isFlipping = true;
        
        float elapsedTime = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation;
        
        if (isUpsideDown)
        {
            // Возвращаем в нормальное положение
            targetRotation = Quaternion.identity;
        }
        else
        {
            // Переворачиваем вверх ногами (180 градусов по Z)
            targetRotation = Quaternion.Euler(0, 0, 180f);
        }
        
        while (elapsedTime < flipDuration)
        {
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / flipDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.rotation = targetRotation;
        isUpsideDown = !isUpsideDown;
        isFlipping = false;
        
        Debug.Log($"Объект перевернут: {(isUpsideDown ? "вверх ногами" : "нормально")}");
    }
    
    // Для мобильных устройств (касание экрана)
    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                // Проверяем, было ли касание по объекту
                Vector2 touchPos = Camera.main.ScreenToWorldPoint(touch.position);
                Collider2D hitCollider = Physics2D.OverlapPoint(touchPos);
                
                if (hitCollider != null && hitCollider.gameObject == gameObject)
                {
                    OnMouseDown();
                }
            }
        }
    }
    
    // Метод для смены типа взаимодействия во время игры
    public void SetInteractionType(InteractionType newType)
    {
        interactionType = newType;
    }
    
    // Метод для установки конкретной силы отталкивания
    public void SetPushForces(float newForce1, float newForce2, float newForce3)
    {
        force1 = newForce1;
        force2 = newForce2;
        force3 = newForce3;
    }
}