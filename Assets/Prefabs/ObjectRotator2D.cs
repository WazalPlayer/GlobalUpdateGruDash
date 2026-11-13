using UnityEngine;
using System.Collections;

public class ObjectRotator2D : MonoBehaviour
{
    [Header("Настройки вращения")]
    [SerializeField] private float rotationSpeed = 180f; // градусов в секунду
    [SerializeField] private float targetRotation = 90f; // целевой угол поворота
    
    [Header("Управление")]
    [SerializeField] private bool useLeftClick = true;
    [SerializeField] private bool useShift = true;
    [SerializeField] private bool useTouch = true;
    
    private bool isRotating = false;
    private float startRotation;
    private float currentRotationProgress = 0f;
    private Quaternion targetQuaternion;
    
    void Update()
    {
        // Проверяем ввод
        if (!isRotating && CheckInput())
        {
            StartRotation();
        }
        
        // Выполняем вращение
        if (isRotating)
        {
            PerformRotation();
        }
    }
    
    private bool CheckInput()
    {
        // ЛКМ
        if (useLeftClick && Input.GetMouseButtonDown(0))
            return true;
            
        // Shift
        if (useShift && Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            return true;
            
        // Касание экрана
        if (useTouch && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;
            
        return false;
    }
    
    private void StartRotation()
    {
        isRotating = true;
        startRotation = transform.eulerAngles.z;
        currentRotationProgress = 0f;
        
        // Вычисляем целевой угол (текущий угол + 90 градусов)
        float newRotation = startRotation + targetRotation;
        targetQuaternion = Quaternion.Euler(0f, 0f, newRotation);
    }
    
    private void PerformRotation()
    {
        // Увеличиваем прогресс вращения
        currentRotationProgress += rotationSpeed * Time.deltaTime;
        
        if (currentRotationProgress >= Mathf.Abs(targetRotation))
        {
            // Завершаем вращение
            transform.rotation = targetQuaternion;
            isRotating = false;
        }
        else
        {
            // Плавно вращаем объект
            float currentAngle = Mathf.Lerp(startRotation, startRotation + targetRotation, 
                                          currentRotationProgress / Mathf.Abs(targetRotation));
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
    }
    
    // Метод для сброса вращения (можно вызвать извне)
    public void ResetRotation()
    {
        isRotating = false;
        transform.rotation = Quaternion.identity;
    }
    
    // Метод для установки скорости вращения
    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }
    
    // Метод для установки угла поворота
    public void SetTargetRotation(float newAngle)
    {
        targetRotation = newAngle;
    }
}