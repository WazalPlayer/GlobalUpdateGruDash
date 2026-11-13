using UnityEngine;
using UnityEngine.UI;

public class ColorController : MonoBehaviour
{
    [Header("UI Слайдеры")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;

    [Header("Настройки цвета")]
    [Range(0f, 1f)] public float redValue = 1f;
    [Range(0f, 1f)] public float greenValue = 1f;
    [Range(0f, 1f)] public float blueValue = 1f;

    [Header("Объект для изменения")]
    public Renderer targetObject;

    // Ключи для сохранения
    private const string RED_KEY = "SavedRedValue";
    private const string GREEN_KEY = "SavedGreenValue";
    private const string BLUE_KEY = "SavedBlueValue";

    void Start()
    {
        // Загрузка сохраненных значений
        LoadSavedColors();
        
        // Инициализация слайдеров
        InitializeSliders();
        
        // Получение объекта, если не задан
        if (targetObject == null)
        {
            targetObject = GetComponent<Renderer>();
        }
        
        // Применяем начальный цвет
        ApplyColor();
    }

    void InitializeSliders()
    {
        if (redSlider != null)
        {
            redSlider.value = redValue;
            redSlider.onValueChanged.AddListener(OnRedChanged);
        }
        
        if (greenSlider != null)
        {
            greenSlider.value = greenValue;
            greenSlider.onValueChanged.AddListener(OnGreenChanged);
        }
        
        if (blueSlider != null)
        {
            blueSlider.value = blueValue;
            blueSlider.onValueChanged.AddListener(OnBlueChanged);
        }
    }

    // Методы для изменения значений
    public void OnRedChanged(float value)
    {
        redValue = value;
        SaveColorValues();
        ApplyColor();
    }

    public void OnGreenChanged(float value)
    {
        greenValue = value;
        SaveColorValues();
        ApplyColor();
    }

    public void OnBlueChanged(float value)
    {
        blueValue = value;
        SaveColorValues();
        ApplyColor();
    }

    // Применение цвета к объекту
    void ApplyColor()
    {
        if (targetObject != null && targetObject.material != null)
        {
            Color newColor = new Color(redValue, greenValue, blueValue);
            targetObject.material.color = newColor;
        }
    }

    // Метод для сброса к белому цвету
    public void ResetToWhite()
    {
        redValue = 1f;
        greenValue = 1f;
        blueValue = 1f;
        
        if (redSlider != null) redSlider.value = redValue;
        if (greenSlider != null) greenSlider.value = greenValue;
        if (blueSlider != null) blueSlider.value = blueValue;
        
        SaveColorValues();
        ApplyColor();
    }

    // Метод для установки черного цвета
    public void SetToBlack()
    {
        redValue = 0f;
        greenValue = 0f;
        blueValue = 0f;
        
        if (redSlider != null) redSlider.value = redValue;
        if (greenSlider != null) greenSlider.value = greenValue;
        if (blueSlider != null) blueSlider.value = blueValue;
        
        SaveColorValues();
        ApplyColor();
    }

    // Метод для установки случайного цвета
    public void SetRandomColor()
    {
        redValue = Random.Range(0f, 1f);
        greenValue = Random.Range(0f, 1f);
        blueValue = Random.Range(0f, 1f);
        
        if (redSlider != null) redSlider.value = redValue;
        if (greenSlider != null) greenSlider.value = greenValue;
        if (blueSlider != null) blueSlider.value = blueValue;
        
        SaveColorValues();
        ApplyColor();
    }

    // Сохранение значений цвета
    void SaveColorValues()
    {
        PlayerPrefs.SetFloat(RED_KEY, redValue);
        PlayerPrefs.SetFloat(GREEN_KEY, greenValue);
        PlayerPrefs.SetFloat(BLUE_KEY, blueValue);
        PlayerPrefs.Save(); // Важно вызывать Save() для сохранения на диск
    }

    // Загрузка сохраненных значений цвета
    void LoadSavedColors()
    {
        // Проверяем, есть ли сохраненные значения
        if (PlayerPrefs.HasKey(RED_KEY))
        {
            redValue = PlayerPrefs.GetFloat(RED_KEY);
        }
        
        if (PlayerPrefs.HasKey(GREEN_KEY))
        {
            greenValue = PlayerPrefs.GetFloat(GREEN_KEY);
        }
        
        if (PlayerPrefs.HasKey(BLUE_KEY))
        {
            blueValue = PlayerPrefs.GetFloat(BLUE_KEY);
        }
    }

    // Метод для сброса сохраненных значений (можно использовать в меню настроек)
    public void ResetSavedColors()
    {
        PlayerPrefs.DeleteKey(RED_KEY);
        PlayerPrefs.DeleteKey(GREEN_KEY);
        PlayerPrefs.DeleteKey(BLUE_KEY);
        
        // Сбрасываем к значениям по умолчанию
        redValue = 1f;
        greenValue = 1f;
        blueValue = 1f;
        
        if (redSlider != null) redSlider.value = redValue;
        if (greenSlider != null) greenSlider.value = greenValue;
        if (blueSlider != null) blueSlider.value = blueValue;
        
        ApplyColor();
    }

    // Метод для принудительного сохранения (можно вызывать при выходе из игры)
    public void ForceSave()
    {
        SaveColorValues();
    }

    void OnDestroy()
    {
        // Сохраняем значения при уничтожении объекта
        SaveColorValues();
        
        // Отписываемся от событий при уничтожении
        if (redSlider != null) redSlider.onValueChanged.RemoveListener(OnRedChanged);
        if (greenSlider != null) greenSlider.onValueChanged.RemoveListener(OnGreenChanged);
        if (blueSlider != null) blueSlider.onValueChanged.RemoveListener(OnBlueChanged);
    }

    // Вызывается при выключении приложения
    void OnApplicationQuit()
    {
        SaveColorValues();
    }

    // Вызывается при потере фокуса приложением (например, при переключении на другое окно)
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveColorValues();
        }
    }
}