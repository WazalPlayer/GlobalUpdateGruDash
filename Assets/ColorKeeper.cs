using UnityEngine;
using UnityEngine.UI;   // для работы с UI Image

/// <summary>
/// Компонент для поддержания заданного цвета на 2D-объекте (спрайт или UI).
/// Добавляется на объект, в инспекторе настраивается цвет.
/// При спавне (Start) и периодически (с заданным интервалом) восстанавливает цвет,
/// если он был изменён другими скриптами.
/// </summary>
public class ColorKeeper : MonoBehaviour
{
    [Header("Целевой цвет")]
    [Tooltip("Цвет, который должен быть у объекта.")]
    public Color targetColor = Color.white;

    [Header("Настройки применения")]
    [Tooltip("Применить цвет сразу при старте (рекомендуется).")]
    public bool applyOnStart = true;

    [Tooltip("Интервал проверки в секундах. 0 — отключить.")]
    public float checkInterval = 60f;

    [Tooltip("Если включено, при изменении цвета в инспекторе во время игры он применится сразу.")]
    public bool applyOnValidateInPlayMode = true;

    [Tooltip("Если включено, цвет будет применяться даже в режиме редактирования (без запуска игры).")]
    public bool applyInEditMode = false;   // осторожно, может влиять на префабы

    // Компоненты-рендереры (кешируем для производительности)
    private SpriteRenderer spriteRenderer;
    private Image image;
    private Renderer genericRenderer;   // для 3D-объектов (если понадобится)

    void Start()
    {
        // Кешируем компоненты при старте
        CacheRenderers();

        if (applyOnStart)
            ApplyColor();
        
        if (checkInterval > 0f)
            InvokeRepeating(nameof(CheckAndApply), checkInterval, checkInterval);
    }

    void OnValidate()
    {
        // Вызывается редактором при изменении любого поля в инспекторе
        // Применяем цвет, только если игра запущена и опция включена
        if (Application.isPlaying && applyOnValidateInPlayMode)
            ApplyColor();
        else if (!Application.isPlaying && applyInEditMode)
            ApplyColor();  // применяем в редакторе (осторожно!)
    }

    void OnDestroy()
    {
        // Отменяем все Invoke, чтобы избежать ошибок
        CancelInvoke();
    }

    /// <summary>
    /// Принудительно устанавливает целевой цвет на найденный рендерер.
    /// Можно вызывать из других скриптов.
    /// </summary>
    public void ApplyColor()
    {
        // Если компоненты ещё не закешированы – делаем это
        if (spriteRenderer == null && image == null && genericRenderer == null)
            CacheRenderers();

        // Пытаемся установить цвет в порядке приоритета: SpriteRenderer ? Image ? Renderer
        if (spriteRenderer != null)
            spriteRenderer.color = targetColor;
        else if (image != null)
            image.color = targetColor;
        else if (genericRenderer != null)
            genericRenderer.material.color = targetColor;
    }

    /// <summary>
    /// Проверяет, совпадает ли текущий цвет с целевым, и если нет – восстанавливает.
    /// </summary>
    private void CheckAndApply()
    {
        if (!IsColorMatching())
            ApplyColor();
    }

    /// <summary>
    /// Сравнивает текущий цвет с целевым (с учётом используемого рендерера).
    /// </summary>
    private bool IsColorMatching()
    {
        Color current = GetCurrentColor();
        return current == targetColor;
    }

    /// <summary>
    /// Получает текущий цвет объекта в зависимости от того, какой рендерер используется.
    /// </summary>
    private Color GetCurrentColor()
    {
        if (spriteRenderer != null)
            return spriteRenderer.color;
        if (image != null)
            return image.color;
        if (genericRenderer != null)
            return genericRenderer.material.color;
        return Color.clear;   // если ничего нет
    }

    /// <summary>
    /// Находит и кеширует все доступные рендереры на объекте.
    /// </summary>
    private void CacheRenderers()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();
        genericRenderer = GetComponent<Renderer>();
    }
}