using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelCreator : MonoBehaviour
{
    [Header("Основные настройки")]
    public GameObject[] objectPrefabs;
    public Button[] objectButtons;
    public Button saveButton;
    public Button clearButton;
    public Button resetSelectionButton;
    public Button deleteModeButton;
    public Button supabaseUploadButton;
    public Button supabaseBrowseButton;
    public Button testConnectionButton;
    public Button marketplaceButton;
    public InputField levelNameInput;
    public Text levelCounterText;
    public Text modeText;

    [Header("Локальные сохранения")]
    public GameObject saveNamePanel;            // Панель с полем ввода названия и кнопками
    public InputField saveNameInputField;       // Поле ввода внутри панели
    public Button confirmSaveButton;            // Кнопка "Сохранить" на панели
    public Button cancelSaveButton;             // Кнопка "Отмена" на панели
    public Button localLevelsButton;            // Кнопка открытия списка локальных уровней
    public LevelMenu levelMenu;                 // Ссылка на LevelMenu (можно найти FindObjectOfType)
    

    [Header("Настройки Z координаты")]
    public InputField zPositionInputField;
    public Slider zPositionSlider;
    public Text zPositionValueText;
    private float currentZ = 0f;

    [Header("Группы и триггеры")]
    public Button groupButton;
    public Button moveTriggerButton;
    public Button colorTriggerButton;
    public Button pulseTriggerButton;
    public Button alphaTriggerButton;
    public InputField groupIdInput;
    public InputField targetGroupIdInput;
    public InputField triggerDurationInput;
    public InputField targetAlphaInput;
    public InputField moveTargetXInput;
    public InputField moveTargetYInput;
    public InputField pulseScaleInput;
    public ColorPicker colorPicker;
    
    [Header("Система магнита")]
    public Button magnetToggleButton;
    public Text magnetText;
    public InputField magnetRadiusInput;
    public Toggle showCentresToggle;
    
    [Header("Вращение и цвет")]
    public Slider rotationSlider;
    public Text rotationValueText;
    public InputField rotationInputField;
    public Slider colorRSlider;
    public Slider colorGSlider;
    public Slider colorBSlider;
    public Image colorPreviewImage;
    public Text colorValueText;
    
    [Header("Префабы и сохранение")]
    public Button createPrefabsButton;
    public Button fixMaterialsButton;
    public Button exportToUnityButton;
    public InputField prefabsFolderInput;
    public Text prefabCreationStatus;
    
    [Header("UI и другие элементы")]
    public MarketplaceUI marketplaceUI;
    public Text notificationText;
    
    [Header("Настройки удаления")]
    public float deleteRadius = 2.0f;
    public LayerMask objectLayerMask = -1;
    
    // Списки объектов
    private List<GameObject> placedObjects = new List<GameObject>();
    private List<GameObject> centreObjects = new List<GameObject>();
    
    // Переменные состояния
    private int selectedIndex = -1;
    private int levelCount = 0;
    private bool isDeleteMode = false;
    private bool isGroupMode = false;
    private bool isMoveTriggerMode = false;
    private bool isColorTriggerMode = false;
    private bool isPulseTriggerMode = false;
    private bool isAlphaTriggerMode = false;
    private bool isMagnetMode = true; // ВКЛЮЧЕН ПО УМОЛЧАНИЮ

    // Система групп
    private Dictionary<int, List<GameObject>> objectGroups = new Dictionary<int, List<GameObject>>();
    private int currentGroupId = 1;
    private Color[] groupColors = new Color[] {
        Color.red, Color.green, Color.blue, Color.yellow, 
        Color.cyan, Color.magenta, Color.white, Color.gray
    };

    // Система магнита
    private float magnetRadius = 3.0f;
    private bool showCentres = false;
    private Material centreHighlightMaterial;
    private List<GameObject> centreVisuals = new List<GameObject>();

    // Настройки вращения и цвета
    private float currentRotation = 0f;
    private Color currentColor = Color.white;

    // Для определения клика (без перетаскивания)
    private Vector2 inputStartPos;
    private bool isInputDragging = false;
    private float clickThreshold = 10f;
    private Vector2 lastInputPosition;

    void Start()
    {
        levelCount = PlayerPrefs.GetInt("LevelCount", 0);
        
        for (int i = 0; i < objectButtons.Length; i++)
        {
            if (objectButtons[i] != null)
            {
                int index = i;
                objectButtons[i].onClick.AddListener(() => SelectObject(index));
            }
        }
        
        // Изменено: кнопка сохранения теперь открывает панель ввода имени
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveButtonClick);
        if (clearButton != null) clearButton.onClick.AddListener(ClearLevel);
        if (resetSelectionButton != null) resetSelectionButton.onClick.AddListener(ResetAllModesIncludingGroup);
        if (deleteModeButton != null) deleteModeButton.onClick.AddListener(ToggleDeleteMode);
        
        if (groupButton != null) groupButton.onClick.AddListener(ToggleGroupMode);
        if (moveTriggerButton != null) moveTriggerButton.onClick.AddListener(ToggleMoveTriggerMode);
        if (colorTriggerButton != null) colorTriggerButton.onClick.AddListener(ToggleColorTriggerMode);
        if (pulseTriggerButton != null) pulseTriggerButton.onClick.AddListener(TogglePulseTriggerMode);
        if (alphaTriggerButton != null) alphaTriggerButton.onClick.AddListener(ToggleAlphaTriggerMode);
        
        if (magnetToggleButton != null) magnetToggleButton.onClick.AddListener(ToggleMagnetMode);
        if (supabaseUploadButton != null) supabaseUploadButton.onClick.AddListener(UploadToSupabase);
        if (testConnectionButton != null) testConnectionButton.onClick.AddListener(TestSupabaseConnection);
        if (marketplaceButton != null) marketplaceButton.onClick.AddListener(ShowMarketplace);
        
        if (createPrefabsButton != null) createPrefabsButton.onClick.AddListener(CreateStaticPrefabs);
        if (fixMaterialsButton != null) fixMaterialsButton.onClick.AddListener(FixAllPurpleMaterials);
        if (exportToUnityButton != null) exportToUnityButton.onClick.AddListener(ExportCurrentLevelToUnityPackage);
        
        if (showCentresToggle != null)
            showCentresToggle.onValueChanged.AddListener(ToggleCentreVisibility);
            
        if (magnetRadiusInput != null)
        {
            magnetRadiusInput.text = magnetRadius.ToString("F1");
            magnetRadiusInput.onEndEdit.AddListener(OnMagnetRadiusChanged);
        }
        
        if (zPositionInputField != null)
        {
            zPositionInputField.text = currentZ.ToString("F2");
            zPositionInputField.contentType = InputField.ContentType.DecimalNumber;
            zPositionInputField.onEndEdit.AddListener(OnZPositionInputChanged);
        }

        if (zPositionSlider != null)
        {
            zPositionSlider.minValue = -10f;
            zPositionSlider.maxValue = 10f;
            zPositionSlider.value = currentZ;
            zPositionSlider.onValueChanged.AddListener(OnZPositionChanged);
        }

        if (zPositionValueText != null)
            zPositionValueText.text = currentZ.ToString("F2");
        
        if (prefabsFolderInput != null)
            prefabsFolderInput.text = "Assets/Prefabs/LevelObjects/";

        // Локальные сохранения
        if (saveNamePanel != null)
            saveNamePanel.SetActive(false);

        if (confirmSaveButton != null)
            confirmSaveButton.onClick.AddListener(ConfirmSave);
        if (cancelSaveButton != null)
            cancelSaveButton.onClick.AddListener(CancelSave);

        if (localLevelsButton != null)
            localLevelsButton.onClick.AddListener(OpenLocalLevels);

        if (levelMenu == null)
            levelMenu = FindObjectOfType<LevelMenu>();
        
        InitializeRotationAndColorUI();
        InitializeGroups();
        FindAllCentreObjects();
        
        if (showCentresToggle != null)
            showCentresToggle.isOn = showCentres;
        
        UpdateButtonColors();
        UpdateLevelCounter();
        UpdateModeText();
        
        StartCoroutine(AutoCheckSupabaseConnection());
    }

    // ---------- Сохранение с вводом имени ----------
    public void OnSaveButtonClick()
    {
        if (placedObjects.Count == 0)
        {
            ShowNotification("❌ Уровень пуст!", Color.red);
            return;
        }
        if (saveNamePanel != null)
        {
            saveNamePanel.SetActive(true);
            if (saveNameInputField != null)
                saveNameInputField.text = "";
        }
    }

    void ConfirmSave()
    {
        string levelName = saveNameInputField?.text?.Trim();
        if (string.IsNullOrEmpty(levelName))
        {
            ShowNotification("Введите название уровня!", Color.red);
            return;
        }
        if (saveNamePanel != null)
            saveNamePanel.SetActive(false);
        SaveLevelWithName(levelName);
    }

    void CancelSave()
    {
        if (saveNamePanel != null)
            saveNamePanel.SetActive(false);
    }

    void SaveLevelWithName(string levelName)
    {
        LevelDataWrapper dataWrapper = GetCurrentLevelDataForSupabase();
        string levelJson = JsonUtility.ToJson(dataWrapper);

        LocalLevelMeta meta = new LocalLevelMeta
        {
            level_name = levelName,
            author = GetPlayerName(),
            save_date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            level_data = levelJson
        };

        string finalJson = JsonUtility.ToJson(meta, true);
        string directory = Path.Combine(Application.persistentDataPath, "LocalLevels");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string fileName = levelName.Replace(" ", "_") + ".json";
        string filePath = Path.Combine(directory, fileName);

        try
        {
            File.WriteAllText(filePath, finalJson);
            levelCount++;
            PlayerPrefs.SetInt("LevelCount", levelCount);
            UpdateLevelCounter();
            ShowNotification($"✅ Уровень '{levelName}' сохранён локально", Color.green);
        }
        catch (System.Exception e)
        {
            ShowNotification($"❌ Ошибка сохранения: {e.Message}", Color.red);
        }
    }

    public void OpenLocalLevels()
    {
        if (levelMenu != null)
            levelMenu.ShowLocalLevels();
        else
            ShowNotification("❌ LevelMenu не найден", Color.red);
    }

    public void LoadLocalLevel(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ShowNotification("❌ Файл уровня не найден", Color.red);
            return;
        }

        string json = File.ReadAllText(filePath);
        LocalLevelMeta meta = JsonUtility.FromJson<LocalLevelMeta>(json);
        if (meta == null || string.IsNullOrEmpty(meta.level_data))
        {
            ShowNotification("❌ Повреждённый файл уровня", Color.red);
            return;
        }

        LevelDataWrapper data = JsonUtility.FromJson<LevelDataWrapper>(meta.level_data);
        if (data == null)
        {
            ShowNotification("❌ Неверный формат данных уровня", Color.red);
            return;
        }

        ClearLevel();
        LoadLevelData(data);   // <-- теперь метод существует

        if (levelNameInput != null)
            levelNameInput.text = meta.level_name;

        ShowNotification($"✅ Уровень '{meta.level_name}' загружен", Color.green);
    }

    // ---------- Вынесенная логика загрузки данных ----------
    private void LoadLevelData(LevelDataWrapper data)
    {
        // Восстановление настроек магнита
        if (data.magnetSettings != null)
        {
            isMagnetMode = data.magnetSettings.useMagnet;
            magnetRadius = data.magnetSettings.magnetRadius;
            showCentres = data.magnetSettings.showCentres;
            if (magnetRadiusInput != null) magnetRadiusInput.text = magnetRadius.ToString("F1");
            if (showCentresToggle != null) showCentresToggle.isOn = showCentres;
            UpdateCentreVisuals();
        }

        // Группы
        if (data.groups != null)
        {
            foreach (var groupData in data.groups)
            {
                objectGroups[groupData.groupId] = new List<GameObject>();
            }
        }

        int createdObjects = 0;
        int failedObjects = 0;

        // Создание объектов
        if (data.objects != null)
        {
            foreach (var objData in data.objects)
            {
                if (objData.objectType >= 0 && objData.objectType < objectPrefabs.Length && objectPrefabs[objData.objectType] != null)
                {
                    Vector3 position = new Vector3(objData.x, objData.y, objData.z);
                    Quaternion rotation = Quaternion.Euler(0, 0, objData.rotation);
                    GameObject newObj = Instantiate(objectPrefabs[objData.objectType], position, rotation);

                    ObjectTypeMarker marker = newObj.AddComponent<ObjectTypeMarker>();
                    marker.prefabIndex = objData.objectType;
                    newObj.name = $"{objectPrefabs[objData.objectType].name}_{createdObjects}";

                    placedObjects.Add(newObj);
                    createdObjects++;
                    AddColliderIfMissing(newObj);

                    Color objColor = new Color(objData.colorR, objData.colorG, objData.colorB, objData.colorA);
                    ApplyColorToObject(newObj, objColor);

                    if (objData.groupId > 0)
                    {
                        AddObjectToGroup(newObj, objData.groupId);
                    }

                    if (objData.isCentre)
                    {
                        newObj.tag = "Centre";
                        centreObjects.Add(newObj);
                    }

                    StartCoroutine(HighlightObject(newObj));
                }
                else
                {
                    failedObjects++;
                }
            }
        }

        // Создание триггеров
        if (data.triggers != null)
        {
            foreach (var triggerData in data.triggers)
            {
                CreateTriggerFromData(triggerData);
                createdObjects++;
            }
        }

        FocusCameraOnLevel();
        UpdateLevelCounter();
        UpdateButtonColors();
        UpdateModeText();
        UpdateCentreVisuals();

        Debug.Log($"Загрузка завершена: создано {createdObjects}, ошибок {failedObjects}");
    }


    void OnEnable()
    {
        FindAllCentreObjects();
        UpdateCentreVisuals();
    }

    public class ObjectTypeMarker : MonoBehaviour
    {
        public int prefabIndex = -1;
    }

    private void InitializeRotationAndColorUI()
    {
        if (rotationSlider != null)
        {
            rotationSlider.minValue = 0f;
            rotationSlider.maxValue = 360f;
            rotationSlider.value = currentRotation;
            rotationSlider.onValueChanged.AddListener(OnRotationChanged);
            
            if (rotationValueText != null)
                rotationValueText.text = $"{currentRotation:F0}°";
        }
        
        if (rotationInputField != null)
        {
            rotationInputField.text = currentRotation.ToString("F0");
            rotationInputField.contentType = InputField.ContentType.DecimalNumber;
            rotationInputField.onEndEdit.AddListener(OnRotationInputChanged);
        }
        
        if (colorRSlider != null)
        {
            colorRSlider.minValue = 0f;
            colorRSlider.maxValue = 1f;
            colorRSlider.value = currentColor.r;
            colorRSlider.onValueChanged.AddListener(OnColorRChanged);
        }
        
        if (colorGSlider != null)
        {
            colorGSlider.minValue = 0f;
            colorGSlider.maxValue = 1f;
            colorGSlider.value = currentColor.g;
            colorGSlider.onValueChanged.AddListener(OnColorGChanged);
        }
        
        if (colorBSlider != null)
        {
            colorBSlider.minValue = 0f;
            colorBSlider.maxValue = 1f;
            colorBSlider.value = currentColor.b;
            colorBSlider.onValueChanged.AddListener(OnColorBChanged);
        }
        
        UpdateColorPreview();
    }

    private void OnRotationChanged(float value)
    {
        currentRotation = value;
        
        if (rotationValueText != null)
            rotationValueText.text = $"{currentRotation:F0}°";
        
        if (rotationInputField != null)
            rotationInputField.text = currentRotation.ToString("F0");
            
        ShowNotification($"Вращение: {currentRotation:F0}°", Color.cyan);
    }

    private void OnRotationInputChanged(string value)
    {
        if (float.TryParse(value, out float newRotation))
        {
            newRotation = Mathf.Clamp(newRotation, 0f, 360f);
            currentRotation = newRotation;
            
            if (rotationSlider != null)
                rotationSlider.value = currentRotation;
            
            if (rotationValueText != null)
                rotationValueText.text = $"{currentRotation:F0}°";
            
            ShowNotification($"Вращение установлено: {currentRotation:F0}°", Color.cyan);
        }
        else
        {
            if (rotationInputField != null)
                rotationInputField.text = currentRotation.ToString("F0");
            
            ShowNotification("Некорректное значение! Введите число от 0 до 360", Color.red);
        }
    }

    private void OnZPositionChanged(float value)
    {
        currentZ = value;
        
        if (zPositionValueText != null)
            zPositionValueText.text = currentZ.ToString("F2");
        
        if (zPositionInputField != null)
            zPositionInputField.text = currentZ.ToString("F2");
        
        ShowNotification($"Z: {currentZ:F2}", Color.cyan);
    }

    private void OnZPositionInputChanged(string value)
    {
        if (float.TryParse(value, out float newZ))
        {
            currentZ = Mathf.Clamp(newZ, -10f, 10f);
            
            if (zPositionSlider != null)
                zPositionSlider.value = currentZ;
            
            if (zPositionValueText != null)
                zPositionValueText.text = currentZ.ToString("F2");
            
            ShowNotification($"Z установлено: {currentZ:F2}", Color.cyan);
        }
        else
        {
            if (zPositionInputField != null)
                zPositionInputField.text = currentZ.ToString("F2");
            
            ShowNotification("Некорректное значение Z! Введите число от -10 до 10", Color.red);
        }
    }

    private void OnColorRChanged(float value)
    {
        currentColor.r = value;
        UpdateColorPreview();
    }

    private void OnColorGChanged(float value)
    {
        currentColor.g = value;
        UpdateColorPreview();
    }

    private void OnColorBChanged(float value)
    {
        currentColor.b = value;
        UpdateColorPreview();
    }

    private void UpdateColorPreview()
    {
        if (colorPreviewImage != null)
            colorPreviewImage.color = currentColor;
            
        if (colorValueText != null)
            colorValueText.text = $"R:{currentColor.r:F2} G:{currentColor.g:F2} B:{currentColor.b:F2}";
    }

    private void InitializeGroups()
    {
        for (int i = 1; i <= 8; i++)
        {
            objectGroups[i] = new List<GameObject>();
        }
        
        if (groupIdInput != null) groupIdInput.text = currentGroupId.ToString();
        if (targetGroupIdInput != null) targetGroupIdInput.text = "1";
        if (triggerDurationInput != null) triggerDurationInput.text = "1.0";
        if (targetAlphaInput != null) targetAlphaInput.text = "0.5";
        if (moveTargetXInput != null) moveTargetXInput.text = "5";
        if (moveTargetYInput != null) moveTargetYInput.text = "0";
        if (pulseScaleInput != null) pulseScaleInput.text = "1.5";
    }

    private void FindAllCentreObjects()
    {
        centreObjects.Clear();
        GameObject[] centreObjs = GameObject.FindGameObjectsWithTag("Centre");
        centreObjects.AddRange(centreObjs);
        Debug.Log($"🔍 Найдено объектов Centre: {centreObjects.Count}");
        UpdateCentreVisuals();
    }

    private void CreateCentreHighlightMaterial()
    {
        if (centreHighlightMaterial == null)
        {
            centreHighlightMaterial = new Material(Shader.Find("Sprites/Default"));
            centreHighlightMaterial.color = new Color(1f, 0.5f, 0f, 0.3f);
        }
    }

    private void UpdateCentreVisuals()
    {
        foreach (GameObject visual in centreVisuals)
        {
            if (visual != null) Destroy(visual);
        }
        centreVisuals.Clear();
        
        if (!showCentres) return;
        
        foreach (GameObject centreObj in centreObjects)
        {
            if (centreObj == null) continue;
            
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "CentreVisual";
            visual.transform.position = centreObj.transform.position;
            visual.transform.localScale = Vector3.one * 0.5f;
            
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null && centreHighlightMaterial != null)
            {
                renderer.material = centreHighlightMaterial;
            }
            
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            centreVisuals.Add(visual);
        }
    }

    public void ToggleGroupMode()
    {
        isGroupMode = !isGroupMode;
        ResetTriggerModes();
        
        if (isGroupMode && groupIdInput != null)
        {
            if (int.TryParse(groupIdInput.text, out int groupId))
            {
                currentGroupId = Mathf.Clamp(groupId, 1, 8);
                groupIdInput.text = currentGroupId.ToString();
            }
        }
        
        UpdateButtonColors();
        UpdateModeText();
        
        ShowNotification($"Режим групп: {(isGroupMode ? "ВКЛ (Группа " + currentGroupId + ")" : "ВЫКЛ")}", 
                        isGroupMode ? groupColors[currentGroupId - 1] : Color.white);
    }

    public void ToggleMoveTriggerMode()
    {
        isMoveTriggerMode = !isMoveTriggerMode;
        ResetOtherModes();
        UpdateButtonColors();
        UpdateModeText();
        
        ShowNotification($"Мув-триггер: {(isMoveTriggerMode ? "ВКЛ" : "ВЫКЛ")}", 
                        isMoveTriggerMode ? Color.cyan : Color.white);
    }

    public void ToggleColorTriggerMode()
    {
        isColorTriggerMode = !isColorTriggerMode;
        ResetOtherModes();
        UpdateButtonColors();
        UpdateModeText();
        
        ShowNotification($"Цвет-триггер: {(isColorTriggerMode ? "ВКЛ" : "ВЫКЛ")}", 
                        isColorTriggerMode ? Color.magenta : Color.white);
    }

    public void TogglePulseTriggerMode()
    {
        isPulseTriggerMode = !isPulseTriggerMode;
        ResetOtherModes();
        UpdateButtonColors();
        UpdateModeText();
        
        ShowNotification($"Пульс-триггер: {(isPulseTriggerMode ? "ВКЛ" : "ВЫКЛ")}", 
                        isPulseTriggerMode ? new Color(1f, 0.5f, 0f) : Color.white);
    }

    public void ToggleAlphaTriggerMode()
    {
        isAlphaTriggerMode = !isAlphaTriggerMode;
        ResetOtherModes();
        UpdateButtonColors();
        UpdateModeText();
        
        ShowNotification($"Альфа-триггер: {(isAlphaTriggerMode ? "ВКЛ" : "ВЫКЛ")}", 
                        isAlphaTriggerMode ? new Color(0.5f, 0.5f, 1f) : Color.white);
    }

    public void ToggleMagnetMode()
    {
        isMagnetMode = !isMagnetMode;
        
        UpdateButtonColors();
        UpdateModeText();
        
        string message = isMagnetMode ? 
            $"Режим магнита ВКЛ (Радиус: {magnetRadius:F1})" : 
            "Режим магнита ВЫКЛ";
            
        ShowNotification(message, isMagnetMode ? new Color(1f, 0.5f, 0f) : Color.white);
    }

    public void ToggleCentreVisibility(bool isVisible)
    {
        showCentres = isVisible;
        UpdateCentreVisuals();
        
        if (showCentresToggle != null)
            showCentresToggle.isOn = showCentres;
    }

    private void OnMagnetRadiusChanged(string value)
    {
        if (float.TryParse(value, out float newRadius) && newRadius > 0)
        {
            magnetRadius = newRadius;
            
            if (isMagnetMode)
            {
                ShowNotification($"Радиус магнита изменен: {magnetRadius:F1}", 
                               new Color(1f, 0.5f, 0f));
            }
        }
    }

    private void ResetOtherModes()
    {
        isDeleteMode = false;
        selectedIndex = -1;
        // isMagnetMode не сбрасываем
    }

    private void ResetTriggerModes()
    {
        isMoveTriggerMode = false;
        isColorTriggerMode = false;
        isPulseTriggerMode = false;
        isAlphaTriggerMode = false;
        isDeleteMode = false;
        selectedIndex = -1;
        // isMagnetMode не сбрасываем
    }

    private Vector3 SnapToNearestCentre(Vector3 position)
    {
        if (!isMagnetMode || centreObjects.Count == 0) return position;
        
        GameObject nearestCentre = null;
        float nearestDistance = float.MaxValue;
        
        foreach (GameObject centreObj in centreObjects)
        {
            if (centreObj == null) continue;
            
            float distance = Vector3.Distance(position, centreObj.transform.position);
            if (distance < magnetRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCentre = centreObj;
            }
        }
        
        if (nearestCentre != null)
        {
            Debug.Log($"🧲 Объект примагничен к центру на расстоянии {nearestDistance:F2}");
            return nearestCentre.transform.position;
        }
        
        return position;
    }

    void SelectObject(int index)
    {
        if (index < 0 || index >= objectPrefabs.Length) return;
        if (objectPrefabs[index] == null) return;
        
        bool wasGroupMode = isGroupMode;
        int savedGroupId = currentGroupId;
        
        ResetAllModesExceptGroup();
        
        if (wasGroupMode)
        {
            isGroupMode = true;
            currentGroupId = savedGroupId;
        }
        
        selectedIndex = index;
        UpdateButtonColors();
        UpdateModeText();
        
        ShowNotification($"Выбран объект: {objectPrefabs[index].name}" + 
                        (isGroupMode ? $" (Группа {currentGroupId})" : ""), Color.cyan);
    }

    private void ResetAllModesExceptGroup()
    {
        selectedIndex = -1;
        isDeleteMode = false;
        isMoveTriggerMode = false;
        isColorTriggerMode = false;
        isPulseTriggerMode = false;
        isAlphaTriggerMode = false;
        // isMagnetMode НЕ сбрасываем
    }

    public void ResetAllModesIncludingGroup()
    {
        selectedIndex = -1;
        isDeleteMode = false;
        isGroupMode = false;
        isMoveTriggerMode = false;
        isColorTriggerMode = false;
        isPulseTriggerMode = false;
        isAlphaTriggerMode = false;
        // isMagnetMode НЕ СБРАСЫВАЕМ – отключается только своей кнопкой
        UpdateButtonColors();
        UpdateModeText();
    }

    // ================= ОБРАБОТКА ВВОДА (для мобильных и ПК) =================
    void Update()
    {
        HandlePlacementInput();

        // Горячие клавиши (без изменений)
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMagnetMode();
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
            {
                IncreaseMagnetRadius();
            }
            else if (Input.GetKeyDown(KeyCode.Minus))
            {
                DecreaseMagnetRadius();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetGroupId(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetGroupId(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetGroupId(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetGroupId(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetGroupId(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetGroupId(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SetGroupId(7);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SetGroupId(8);

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
        {
            UploadToSupabase();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.B))
        {
            ShowMarketplace();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.U))
        {
            CreateStaticPrefabs();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
        {
            FixAllPurpleMaterials();
        }
    }

    private void HandlePlacementInput()
    {
        bool isPointerDown = false;
        Vector2 currentPos = Vector2.zero;

        // ---- Обработка мыши ----
        if (Input.GetMouseButtonDown(0))
        {
            inputStartPos = Input.mousePosition;
            lastInputPosition = inputStartPos;
            isInputDragging = false;
        }
        else if (Input.GetMouseButton(0))
        {
            currentPos = Input.mousePosition;
            lastInputPosition = currentPos;
            if (Vector2.Distance(inputStartPos, currentPos) > clickThreshold)
            {
                isInputDragging = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (!isInputDragging && !IsPointerOverUIObject())
            {
                ProcessPlacementClick(lastInputPosition);
            }
            isInputDragging = false;
        }

        // ---- Обработка касаний (мобильные) ----
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    inputStartPos = touch.position;
                    lastInputPosition = inputStartPos;
                    isInputDragging = false;
                    break;
                case TouchPhase.Moved:
                    lastInputPosition = touch.position;
                    if (Vector2.Distance(inputStartPos, touch.position) > clickThreshold)
                    {
                        isInputDragging = true;
                    }
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (!isInputDragging && !IsPointerOverUIObject())
                    {
                        ProcessPlacementClick(lastInputPosition);
                    }
                    isInputDragging = false;
                    break;
            }
        }
    }

    private void ProcessPlacementClick(Vector2 screenPos)
    {
        if (isDeleteMode)
        {
            DeleteObjectAtPosition(screenPos);
        }
        else if (selectedIndex != -1 || isMoveTriggerMode || isColorTriggerMode || 
                 isPulseTriggerMode || isAlphaTriggerMode)
        {
            PlaceObject(screenPos);
        }
    }

    // ================= МЕТОДЫ РАЗМЕЩЕНИЯ (С ПАРАМЕТРОМ screenPos) =================

    void PlaceObject(Vector2 screenPos)
    {
        if (isMoveTriggerMode || isColorTriggerMode || isPulseTriggerMode || isAlphaTriggerMode)
        {
            PlaceTriggerObject(screenPos);
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= objectPrefabs.Length) return;
        if (objectPrefabs[selectedIndex] == null) return;

        Vector3 mousePos = screenPos;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = currentZ;
        
        Vector3 snappedPos = SnapToNearestCentre(worldPos);
        snappedPos.z = currentZ;
        
        Quaternion rotation = Quaternion.Euler(0, 0, currentRotation);
        GameObject newObj = Instantiate(objectPrefabs[selectedIndex], snappedPos, rotation);
        
        if (newObj != null)
        {
            AddColliderIfMissing(newObj);
            
            if (isGroupMode)
            {
                // Добавляем в группу и применяем цвет группы
                AddObjectToGroup(newObj, currentGroupId);
                ApplyGroupColor(newObj, currentGroupId);
                // Индивидуальный цвет не применяем
            }
            else
            {
                // Применяем выбранный цвет
                ApplyColorToObject(newObj, currentColor);
            }
            
            placedObjects.Add(newObj);
            UpdateLevelCounter();
            UpdateButtonColors();
            
            string posInfo = (Vector3.Distance(worldPos, snappedPos) > 0.01f) ? 
                $"примагниченная позиция {snappedPos} (исходная {worldPos})" : 
                $"позиция {worldPos}";
                
            Debug.Log($"✅ Размещен объект: {newObj.name} на {posInfo}, вращение: {currentRotation}°, цвет: {(isGroupMode ? "групповой" : currentColor.ToString())}" + 
                     (isGroupMode ? $" (Группа {currentGroupId})" : ""));
        }
    }

    void PlaceTriggerObject(Vector2 screenPos)
    {
        Vector3 mousePos = screenPos;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        Vector3 snappedPos = SnapToNearestCentre(worldPos);

        GameObject triggerObj = new GameObject();
        
        if (isMoveTriggerMode)
        {
            triggerObj.name = "MoveTrigger";
            MoveTrigger moveTrigger = triggerObj.AddComponent<MoveTrigger>();
            
            if (targetGroupIdInput != null && int.TryParse(targetGroupIdInput.text, out int targetGroup))
            {
                moveTrigger.targetGroupId = Mathf.Clamp(targetGroup, 1, 8);
            }
            
            if (triggerDurationInput != null && float.TryParse(triggerDurationInput.text, out float duration))
            {
                moveTrigger.moveDuration = duration;
            }
            
            float targetX = 5f;
            float targetY = 0f;
            
            if (moveTargetXInput != null && float.TryParse(moveTargetXInput.text, out float x))
            {
                targetX = x;
            }
            
            if (moveTargetYInput != null && float.TryParse(moveTargetYInput.text, out float y))
            {
                targetY = y;
            }
            
            moveTrigger.targetPosition = new Vector3(targetX, targetY, 0f);
            moveTrigger.moveEaseType = EaseType.EaseInOut;
            
            Debug.Log($"🎯 Move Trigger: группа {moveTrigger.targetGroupId}, позиция {moveTrigger.targetPosition}");
        }
        else if (isColorTriggerMode)
        {
            triggerObj.name = "ColorTrigger";
            ColorTrigger colorTrigger = triggerObj.AddComponent<ColorTrigger>();
            
            if (targetGroupIdInput != null && int.TryParse(targetGroupIdInput.text, out int targetGroup))
            {
                colorTrigger.targetGroupId = Mathf.Clamp(targetGroup, 1, 8);
            }
            
            if (triggerDurationInput != null && float.TryParse(triggerDurationInput.text, out float duration))
            {
                colorTrigger.colorDuration = duration;
            }
            
            colorTrigger.targetColor = colorPicker != null ? colorPicker.CurrentColor : Color.red;
            colorTrigger.colorEaseType = EaseType.EaseInOut;
            
            Debug.Log($"🎨 Color Trigger: группа {colorTrigger.targetGroupId}, цвет {colorTrigger.targetColor}");
        }
        else if (isPulseTriggerMode)
        {
            triggerObj.name = "PulseTrigger";
            PulseTrigger pulseTrigger = triggerObj.AddComponent<PulseTrigger>();
            
            if (targetGroupIdInput != null && int.TryParse(targetGroupIdInput.text, out int targetGroup))
            {
                pulseTrigger.targetGroupId = Mathf.Clamp(targetGroup, 1, 8);
            }
            
            if (triggerDurationInput != null && float.TryParse(triggerDurationInput.text, out float duration))
            {
                pulseTrigger.pulseDuration = duration;
            }
            
            float pulseScale = 1.5f;
            if (pulseScaleInput != null && float.TryParse(pulseScaleInput.text, out float scale))
            {
                pulseScale = scale;
            }
            
            pulseTrigger.pulseType = PulseType.GrowAndShrink;
            pulseTrigger.pulseScale = pulseScale;
            
            Debug.Log($"💓 Pulse Trigger: группа {pulseTrigger.targetGroupId}, масштаб {pulseTrigger.pulseScale}");
        }
        else if (isAlphaTriggerMode)
        {
            triggerObj.name = "AlphaTrigger";
            AlphaTrigger alphaTrigger = triggerObj.AddComponent<AlphaTrigger>();
            
            if (targetGroupIdInput != null && int.TryParse(targetGroupIdInput.text, out int targetGroup))
            {
                alphaTrigger.targetGroupId = Mathf.Clamp(targetGroup, 1, 8);
            }
            
            if (triggerDurationInput != null && float.TryParse(triggerDurationInput.text, out float duration))
            {
                alphaTrigger.alphaDuration = duration;
            }
            
            float targetAlpha = 0.5f;
            if (targetAlphaInput != null && float.TryParse(targetAlphaInput.text, out float alpha))
            {
                targetAlpha = Mathf.Clamp01(alpha);
            }
            
            alphaTrigger.targetAlpha = targetAlpha;
            alphaTrigger.alphaEaseType = EaseType.EaseInOut;
            
            Debug.Log($"🌫️ Alpha Trigger: группа {alphaTrigger.targetGroupId}, прозрачность {alphaTrigger.targetAlpha}");
        }

        TriggerActivator activator = triggerObj.AddComponent<TriggerActivator>();
        activator.levelCreator = this;

        SpriteRenderer sr = triggerObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateTriggerSprite();
        sr.color = GetTriggerColor();
        
        BoxCollider2D collider = triggerObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1f);
        
        triggerObj.transform.position = snappedPos;
        placedObjects.Add(triggerObj);
        
        UpdateLevelCounter();
        
        string posInfo = (Vector3.Distance(worldPos, snappedPos) > 0.01f) ? 
            $"примагниченная позиция {snappedPos}" : 
            $"позиция {worldPos}";
            
        ShowNotification($"✅ Размещен {triggerObj.name} на {posInfo}", GetTriggerColor());
    }

    // ================= МЕТОДЫ УДАЛЕНИЯ (С ПАРАМЕТРОМ screenPos) =================

    void DeleteObjectAtPosition(Vector2 screenPos)
    {
        Debug.Log("🎯 Попытка удаления объекта...");

        Vector3 mouseScreenPos = screenPos;
        mouseScreenPos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        if (isMagnetMode)
        {
            GameObject nearestCentre = GetNearestCentre(worldPos, magnetRadius);
            if (nearestCentre != null)
            {
                foreach (GameObject obj in placedObjects)
                {
                    if (obj != null && Vector3.Distance(obj.transform.position, nearestCentre.transform.position) < 0.1f)
                    {
                        DeleteSpecificObject(obj);
                        return;
                    }
                }
            }
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, objectLayerMask);
        
        Debug.Log($"🎯 Рейкаст обнаружено объектов: {hits.Length}");

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"🎯 Проверяем объект: {hitObject.name}");

            GameObject objectToDelete = FindObjectInPlacedObjects(hitObject);
            
            if (objectToDelete != null)
            {
                Debug.Log($"✅ Найден объект для удаления: {objectToDelete.name}");
                DeleteSpecificObject(objectToDelete);
                return;
            }
        }

        Debug.Log($"🔍 Поиск объектов вокруг позиции: {worldPos}");
        DeleteNearestObject(worldPos);
    }

    private GameObject GetNearestCentre(Vector3 position, float maxDistance)
    {
        GameObject nearestCentre = null;
        float nearestDistance = float.MaxValue;
        
        foreach (GameObject centreObj in centreObjects)
        {
            if (centreObj == null) continue;
            
            float distance = Vector3.Distance(position, centreObj.transform.position);
            if (distance < maxDistance && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCentre = centreObj;
            }
        }
        
        return nearestCentre;
    }

    private GameObject FindObjectInPlacedObjects(GameObject hitObject)
    {
        if (placedObjects.Contains(hitObject))
            return hitObject;

        foreach (GameObject placedObj in placedObjects)
        {
            if (placedObj == null) continue;
            
            Transform[] allChildren = placedObj.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.gameObject == hitObject)
                {
                    return placedObj;
                }
            }
        }

        return null;
    }

    void DeleteNearestObject(Vector3 position)
    {
        GameObject nearestObject = null;
        float nearestDistance = float.MaxValue;

        Debug.Log($"🔍 Поиск ближайшего объекта в радиусе {deleteRadius}. Всего объектов: {placedObjects.Count}");

        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                float distance = Vector3.Distance(obj.transform.position, position);
                Debug.Log($"📏 Расстояние до {obj.name}: {distance}");
                
                if (distance < deleteRadius && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestObject = obj;
                }
            }
        }

        if (nearestObject != null)
        {
            Debug.Log($"🎯 Найден ближайший объект: {nearestObject.name} (расстояние: {nearestDistance})");
            DeleteSpecificObject(nearestObject);
        }
        else
        {
            Debug.Log("❌ Не найден объект для удаления в радиусе");
            ShowNotification("❌ Не найден объект для удаления", Color.red);
        }
    }

    void DeleteSpecificObject(GameObject objToDelete)
    {
        if (objToDelete != null && placedObjects.Contains(objToDelete))
        {
            string objectName = objToDelete.name;
            
            if (objToDelete.CompareTag("Centre"))
            {
                centreObjects.Remove(objToDelete);
                UpdateCentreVisuals();
            }
            
            RemoveObjectFromGroup(objToDelete);
            
            placedObjects.Remove(objToDelete);
            Destroy(objToDelete);
            UpdateLevelCounter();
            UpdateButtonColors();
            
            ShowNotification($"🗑️ Удален объект: {objectName}", Color.yellow);
            Debug.Log($"✅ Успешно удален объект: {objectName}");
        }
    }

    // ================= ОСТАЛЬНЫЕ МЕТОДЫ (полные) =================

    public void ActivateTrigger(GameObject triggerObj)
    {
        Debug.Log($"🎯 Активирован триггер: {triggerObj.name}");

        MoveTrigger moveTrigger = triggerObj.GetComponent<MoveTrigger>();
        ColorTrigger colorTrigger = triggerObj.GetComponent<ColorTrigger>();
        PulseTrigger pulseTrigger = triggerObj.GetComponent<PulseTrigger>();
        AlphaTrigger alphaTrigger = triggerObj.GetComponent<AlphaTrigger>();

        if (moveTrigger != null)
        {
            StartCoroutine(ActivateMoveTrigger(moveTrigger));
        }
        else if (colorTrigger != null)
        {
            StartCoroutine(ActivateColorTrigger(colorTrigger));
        }
        else if (pulseTrigger != null)
        {
            StartCoroutine(ActivatePulseTrigger(pulseTrigger));
        }
        else if (alphaTrigger != null)
        {
            StartCoroutine(ActivateAlphaTrigger(alphaTrigger));
        }
    }

    private IEnumerator ActivateMoveTrigger(MoveTrigger trigger)
    {
        Debug.Log($"🎯 Активация Move Trigger: группа {trigger.targetGroupId}, целевая позиция {trigger.targetPosition}");
        
        if (objectGroups.ContainsKey(trigger.targetGroupId))
        {
            var groupObjects = objectGroups[trigger.targetGroupId];
            Debug.Log($"🎯 Найдено объектов в группе: {groupObjects.Count}");
            
            foreach (var obj in groupObjects)
            {
                if (obj != null)
                {
                    Debug.Log($"🎯 Перемещаем объект {obj.name} из позиции {obj.transform.position} в {trigger.targetPosition}");
                    StartCoroutine(MoveObject(obj, trigger.targetPosition, trigger.moveDuration, trigger.moveEaseType));
                }
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Группа {trigger.targetGroupId} не найдена!");
        }
        yield return null;
    }

    private IEnumerator ActivateColorTrigger(ColorTrigger trigger)
    {
        if (objectGroups.ContainsKey(trigger.targetGroupId))
        {
            var groupObjects = objectGroups[trigger.targetGroupId];
            foreach (var obj in groupObjects)
            {
                if (obj != null)
                {
                    StartCoroutine(ChangeObjectColor(obj, trigger.targetColor, trigger.colorDuration, trigger.colorEaseType));
                }
            }
        }
        yield return null;
    }

    private IEnumerator ActivatePulseTrigger(PulseTrigger trigger)
    {
        if (objectGroups.ContainsKey(trigger.targetGroupId))
        {
            var groupObjects = objectGroups[trigger.targetGroupId];
            foreach (var obj in groupObjects)
            {
                if (obj != null)
                {
                    StartCoroutine(PulseObject(obj, trigger.pulseScale, trigger.pulseDuration, trigger.pulseType));
                }
            }
        }
        yield return null;
    }

    private IEnumerator ActivateAlphaTrigger(AlphaTrigger trigger)
    {
        if (objectGroups.ContainsKey(trigger.targetGroupId))
        {
            var groupObjects = objectGroups[trigger.targetGroupId];
            foreach (var obj in groupObjects)
            {
                if (obj != null)
                {
                    StartCoroutine(ChangeObjectAlpha(obj, trigger.targetAlpha, trigger.alphaDuration, trigger.alphaEaseType));
                }
            }
        }
        yield return null;
    }

    private IEnumerator MoveObject(GameObject obj, Vector3 targetPosition, float duration, EaseType easeType)
    {
        Vector3 startPosition = obj.transform.position;
        float elapsed = 0f;

        Debug.Log($"🎯 Начало движения: {obj.name} из {startPosition} в {targetPosition} за {duration} сек");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = ApplyEase(t, easeType);
            
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        obj.transform.position = targetPosition;
        Debug.Log($"🎯 Завершено движение: {obj.name} теперь в {targetPosition}");
    }

    private IEnumerator ChangeObjectColor(GameObject obj, Color targetColor, float duration, EaseType easeType)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = ApplyEase(t, easeType);
                
                spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            spriteRenderer.color = targetColor;
        }
        else
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                Color startColor = mat.color;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    t = ApplyEase(t, easeType);
                    
                    mat.color = Color.Lerp(startColor, targetColor, t);
                    yield return null;
                }

                mat.color = targetColor;
            }
        }
    }

    private IEnumerator ChangeObjectAlpha(GameObject obj, float targetAlpha, float duration, EaseType easeType)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = ApplyEase(t, easeType);
                
                spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            spriteRenderer.color = targetColor;
        }
        else
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                Color startColor = mat.color;
                Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    t = ApplyEase(t, easeType);
                    
                    mat.color = Color.Lerp(startColor, targetColor, t);
                    yield return null;
                }

                mat.color = targetColor;
            }
        }
    }

    private IEnumerator PulseObject(GameObject obj, float pulseScale, float duration, PulseType pulseType)
    {
        Vector3 originalScale = obj.transform.localScale;
        Vector3 targetScale = originalScale * pulseScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (pulseType == PulseType.Grow)
            {
                obj.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            }
            else if (pulseType == PulseType.Shrink)
            {
                obj.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            }
            else if (pulseType == PulseType.GrowAndShrink)
            {
                if (t < 0.5f)
                {
                    obj.transform.localScale = Vector3.Lerp(originalScale, targetScale, t * 2f);
                }
                else
                {
                    obj.transform.localScale = Vector3.Lerp(targetScale, originalScale, (t - 0.5f) * 2f);
                }
            }
            yield return null;
        }

        obj.transform.localScale = originalScale;
    }

    private float ApplyEase(float t, EaseType easeType)
    {
        switch (easeType)
        {
            case EaseType.EaseIn:
                return t * t;
            case EaseType.EaseOut:
                return 1f - (1f - t) * (1f - t);
            case EaseType.EaseInOut:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            default:
                return t;
        }
    }

    private Color GetTriggerColor()
    {
        if (isMoveTriggerMode) return Color.cyan;
        if (isColorTriggerMode) return Color.magenta;
        if (isPulseTriggerMode) return new Color(1f, 0.5f, 0f);
        if (isAlphaTriggerMode) return new Color(0.5f, 0.5f, 1f);
        return Color.white;
    }

    private Sprite CreateTriggerSprite()
    {
        Texture2D texture = new Texture2D(16, 16);
        Color[] pixels = new Color[16 * 16];
        Color triggerColor = GetTriggerColor();
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = triggerColor;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
    }

    private void AddObjectToGroup(GameObject obj, int groupId)
    {
        if (objectGroups.ContainsKey(groupId))
        {
            objectGroups[groupId].Add(obj);
            
            ObjectGroup objectGroup = obj.GetComponent<ObjectGroup>();
            if (objectGroup == null)
            {
                objectGroup = obj.AddComponent<ObjectGroup>();
            }
            objectGroup.groupId = groupId;
            
            Debug.Log($"📦 Объект {obj.name} добавлен в группу {groupId}");
        }
    }

    private void ApplyGroupColor(GameObject obj, int groupId)
    {
        Color groupColor = groupColors[groupId - 1];
        ApplyColorToObject(obj, groupColor);
    }

    public void RemoveObjectFromGroup(GameObject obj)
    {
        ObjectGroup objectGroup = obj.GetComponent<ObjectGroup>();
        if (objectGroup != null)
        {
            int groupId = objectGroup.groupId;
            if (objectGroups.ContainsKey(groupId))
            {
                objectGroups[groupId].Remove(obj);
            }
            Destroy(objectGroup);
        }
    }

    private void ApplyColorToObject(GameObject obj, Color color)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Material newMaterial = new Material(renderer.material);
                newMaterial.color = color;
                renderer.material = newMaterial;
            }
        }
        
        SpriteRenderer[] childSpriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer childRenderer in childSpriteRenderers)
        {
            if (childRenderer != null)
            {
                childRenderer.color = color;
            }
        }
        
        Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer childRenderer in childRenderers)
        {
            if (childRenderer != null && childRenderer.GetComponent<SpriteRenderer>() == null)
            {
                Material newChildMaterial = new Material(childRenderer.material);
                newChildMaterial.color = color;
                childRenderer.material = newChildMaterial;
            }
        }
    }

    public LevelDataWrapper GetCurrentLevelDataForSupabase()
    {
        LevelDataWrapper levelData = new LevelDataWrapper();
        levelData.objects = new List<LevelObjectData>();
        levelData.groups = new List<GroupData>();
        levelData.triggers = new List<TriggerData>();
        levelData.magnetSettings = new MagnetSettingsData();
        
        levelData.magnetSettings.useMagnet = isMagnetMode;
        levelData.magnetSettings.magnetRadius = magnetRadius;
        levelData.magnetSettings.showCentres = showCentres;
        
        foreach (GameObject obj in placedObjects)
        {
            if (obj == null) continue;

            if (obj.GetComponent<MoveTrigger>() != null || 
                obj.GetComponent<ColorTrigger>() != null ||
                obj.GetComponent<PulseTrigger>() != null ||
                obj.GetComponent<AlphaTrigger>() != null)
            {
                SaveTriggerData(obj, levelData.triggers);
                continue;
            }

            int prefabIndex = GetPrefabIndex(obj);
            if (prefabIndex == -1) continue;

            LevelObjectData objData = new LevelObjectData();
            objData.objectType = prefabIndex;
            objData.x = obj.transform.position.x;
            objData.y = obj.transform.position.y;
            objData.z = obj.transform.position.z;
            
            objData.rotation = obj.transform.rotation.eulerAngles.z;
            
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                objData.colorR = spriteRenderer.color.r;
                objData.colorG = spriteRenderer.color.g;
                objData.colorB = spriteRenderer.color.b;
                objData.colorA = spriteRenderer.color.a;
            }
            else
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    objData.colorR = renderer.material.color.r;
                    objData.colorG = renderer.material.color.g;
                    objData.colorB = renderer.material.color.b;
                    objData.colorA = renderer.material.color.a;
                }
                else
                {
                    objData.colorR = currentColor.r;
                    objData.colorG = currentColor.g;
                    objData.colorB = currentColor.b;
                    objData.colorA = 1f;
                }
            }
            
            ObjectGroup objectGroup = obj.GetComponent<ObjectGroup>();
            if (objectGroup != null)
            {
                objData.groupId = objectGroup.groupId;
            }
            
            if (obj.CompareTag("Centre"))
            {
                objData.isCentre = true;
            }
            
            levelData.objects.Add(objData);
        }
        
        foreach (var group in objectGroups)
        {
            if (group.Value.Count > 0)
            {
                GroupData groupData = new GroupData();
                groupData.groupId = group.Key;
                groupData.objectCount = group.Value.Count;
                levelData.groups.Add(groupData);
            }
        }

        return levelData;
    }

    private void SaveTriggerData(GameObject triggerObj, List<TriggerData> triggers)
    {
        TriggerData triggerData = new TriggerData();
        triggerData.x = triggerObj.transform.position.x;
        triggerData.y = triggerObj.transform.position.y;
        triggerData.z = triggerObj.transform.position.z;

        MoveTrigger moveTrigger = triggerObj.GetComponent<MoveTrigger>();
        ColorTrigger colorTrigger = triggerObj.GetComponent<ColorTrigger>();
        PulseTrigger pulseTrigger = triggerObj.GetComponent<PulseTrigger>();
        AlphaTrigger alphaTrigger = triggerObj.GetComponent<AlphaTrigger>();

        if (moveTrigger != null)
        {
            triggerData.triggerType = "move";
            triggerData.targetGroupId = moveTrigger.targetGroupId;
            triggerData.duration = moveTrigger.moveDuration;
            triggerData.targetX = moveTrigger.targetPosition.x;
            triggerData.targetY = moveTrigger.targetPosition.y;
        }
        else if (colorTrigger != null)
        {
            triggerData.triggerType = "color";
            triggerData.targetGroupId = colorTrigger.targetGroupId;
            triggerData.duration = colorTrigger.colorDuration;
            triggerData.colorR = colorTrigger.targetColor.r;
            triggerData.colorG = colorTrigger.targetColor.g;
            triggerData.colorB = colorTrigger.targetColor.b;
            triggerData.colorA = colorTrigger.targetColor.a;
        }
        else if (pulseTrigger != null)
        {
            triggerData.triggerType = "pulse";
            triggerData.targetGroupId = pulseTrigger.targetGroupId;
            triggerData.duration = pulseTrigger.pulseDuration;
            triggerData.pulseScale = pulseTrigger.pulseScale;
        }
        else if (alphaTrigger != null)
        {
            triggerData.triggerType = "alpha";
            triggerData.targetGroupId = alphaTrigger.targetGroupId;
            triggerData.duration = alphaTrigger.alphaDuration;
            triggerData.targetAlpha = alphaTrigger.targetAlpha;
        }

        triggers.Add(triggerData);
    }

    public void LoadLevelFromSupabase(SupabaseLevelData levelData)
    {
        if (levelData == null)
        {
            ShowNotification("❌ Данные уровня пусты!", Color.red);
            return;
        }

        Debug.Log($"🎮 Загружаем уровень из Supabase: {levelData.level_name}");
        Debug.Log($"📦 JSON данные: {levelData.level_data}");
        
        if (!IsValidJson(levelData.level_data))
        {
            ShowNotification("❌ Неверный формат данных уровня", Color.red);
            return;
        }
        
        try
        {
            ClearLevel();
            
            if (levelNameInput != null)
            {
                levelNameInput.text = levelData.level_name;
            }

            LevelDataWrapper gameLevelData = JsonUtility.FromJson<LevelDataWrapper>(levelData.level_data);
            
            if (gameLevelData != null)
            {
                Debug.Log($"✅ JSON распарсен. Объектов: {gameLevelData.objects?.Count ?? 0}, Триггеров: {gameLevelData.triggers?.Count ?? 0}");
                
                if (gameLevelData.magnetSettings != null)
                {
                    isMagnetMode = gameLevelData.magnetSettings.useMagnet;
                    magnetRadius = gameLevelData.magnetSettings.magnetRadius;
                    showCentres = gameLevelData.magnetSettings.showCentres;
                    
                    if (magnetRadiusInput != null) magnetRadiusInput.text = magnetRadius.ToString("F1");
                    if (showCentresToggle != null) showCentresToggle.isOn = showCentres;
                    
                    UpdateCentreVisuals();
                }
                
                if (gameLevelData.groups != null)
                {
                    foreach (var groupData in gameLevelData.groups)
                    {
                        objectGroups[groupData.groupId] = new List<GameObject>();
                    }
                }

                int createdObjects = 0;
                int failedObjects = 0;
                
                if (gameLevelData.objects != null && gameLevelData.objects.Count > 0)
                {
                    foreach (var objData in gameLevelData.objects)
                    {
                        Debug.Log($"🔧 Создаем объект типа {objData.objectType} на позиции ({objData.x}, {objData.y}, {objData.z})");
                        
                        if (objData.objectType >= 0 && objData.objectType < objectPrefabs.Length && objectPrefabs[objData.objectType] != null)
                        {
                            Vector3 position = new Vector3(objData.x, objData.y, objData.z);
                            
                            Quaternion rotation = Quaternion.Euler(0, 0, objData.rotation);
                            GameObject newObj = Instantiate(objectPrefabs[objData.objectType], position, rotation);

                            // <-- ДОБАВЬТЕ ЭТИ ДВЕ СТРОКИ
                            ObjectTypeMarker marker = newObj.AddComponent<ObjectTypeMarker>();
                            marker.prefabIndex = objData.objectType;

                            newObj.name = $"{objectPrefabs[objData.objectType].name}_{createdObjects}";
                            
                            if (newObj != null)
                            {
                                placedObjects.Add(newObj);
                                createdObjects++;
                                
                                AddColliderIfMissing(newObj);
                                
                                // Применяем сохранённый цвет
                                Color objColor = new Color(objData.colorR, objData.colorG, objData.colorB, objData.colorA);
                                ApplyColorToObject(newObj, objColor);
                                
                                if (objData.groupId > 0)
                                {
                                    AddObjectToGroup(newObj, objData.groupId);
                                    // НЕ вызываем ApplyGroupColor, чтобы не перезаписывать сохранённый цвет
                                }
                                
                                if (objData.isCentre)
                                {
                                    newObj.tag = "Centre";
                                    centreObjects.Add(newObj);
                                }
                                
                                newObj.name = $"{objectPrefabs[objData.objectType].name}_{createdObjects}";
                                StartCoroutine(HighlightObject(newObj));
                                
                                Debug.Log($"✅ Создан объект: {newObj.name} в позиции {position}, вращение: {objData.rotation}°, цвет: {objColor}");
                            }
                            else
                            {
                                failedObjects++;
                            }
                        }
                        else
                        {
                            failedObjects++;
                        }
                    }
                }

                if (gameLevelData.triggers != null && gameLevelData.triggers.Count > 0)
                {
                    foreach (var triggerData in gameLevelData.triggers)
                    {
                        CreateTriggerFromData(triggerData);
                        createdObjects++;
                    }
                }
                
                FocusCameraOnLevel();
                
                ShowNotification($"✅ Создано объектов: {createdObjects}. Ошибок: {failedObjects}", 
                                failedObjects == 0 ? Color.green : Color.yellow);
                
                UpdateLevelCounter();
                UpdateButtonColors();
                UpdateModeText();
                UpdateCentreVisuals();
            }
            else
            {
                Debug.LogWarning("⚠️ В уровне нет объектов или ошибка парсинга");
                ShowNotification("⚠️ В уровне нет объектов", Color.yellow);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Критическая ошибка загрузки уровня: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            ShowNotification($"❌ Ошибка загрузки уровня: {e.Message}", Color.red);
        }
    }

    private void CreateTriggerFromData(TriggerData triggerData)
    {
        GameObject triggerObj = new GameObject();
        triggerObj.transform.position = new Vector3(triggerData.x, triggerData.y, triggerData.z);

        switch (triggerData.triggerType)
        {
            case "move":
                MoveTrigger moveTrigger = triggerObj.AddComponent<MoveTrigger>();
                moveTrigger.targetGroupId = triggerData.targetGroupId;
                moveTrigger.moveDuration = triggerData.duration;
                moveTrigger.targetPosition = new Vector3(triggerData.targetX, triggerData.targetY, 0);
                triggerObj.name = "MoveTrigger";
                break;
                
            case "color":
                ColorTrigger colorTrigger = triggerObj.AddComponent<ColorTrigger>();
                colorTrigger.targetGroupId = triggerData.targetGroupId;
                colorTrigger.colorDuration = triggerData.duration;
                colorTrigger.targetColor = new Color(triggerData.colorR, triggerData.colorG, triggerData.colorB, triggerData.colorA);
                triggerObj.name = "ColorTrigger";
                break;
                
            case "pulse":
                PulseTrigger pulseTrigger = triggerObj.AddComponent<PulseTrigger>();
                pulseTrigger.targetGroupId = triggerData.targetGroupId;
                pulseTrigger.pulseDuration = triggerData.duration;
                pulseTrigger.pulseScale = triggerData.pulseScale;
                triggerObj.name = "PulseTrigger";
                break;
                
            case "alpha":
                AlphaTrigger alphaTrigger = triggerObj.AddComponent<AlphaTrigger>();
                alphaTrigger.targetGroupId = triggerData.targetGroupId;
                alphaTrigger.alphaDuration = triggerData.duration;
                alphaTrigger.targetAlpha = triggerData.targetAlpha;
                triggerObj.name = "AlphaTrigger";
                break;
        }

        TriggerActivator activator = triggerObj.AddComponent<TriggerActivator>();
        activator.levelCreator = this;

        SpriteRenderer sr = triggerObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateTriggerSpriteForType(triggerData.triggerType);
        sr.color = GetTriggerColorByType(triggerData.triggerType);
        
        BoxCollider2D collider = triggerObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1f);
        
        placedObjects.Add(triggerObj);
    }

    private Sprite CreateTriggerSpriteForType(string triggerType)
    {
        Texture2D texture = new Texture2D(16, 16);
        Color[] pixels = new Color[16 * 16];
        Color triggerColor = GetTriggerColorByType(triggerType);
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = triggerColor;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
    }

    private Color GetTriggerColorByType(string triggerType)
    {
        switch (triggerType)
        {
            case "move": return Color.cyan;
            case "color": return Color.magenta;
            case "pulse": return new Color(1f, 0.5f, 0f);
            case "alpha": return new Color(0.5f, 0.5f, 1f);
            default: return Color.white;
        }
    }

    void UpdateButtonColors()
    {
        if (objectButtons != null)
        {
            for (int i = 0; i < objectButtons.Length; i++)
            {
                if (objectButtons[i] != null)
                {
                    objectButtons[i].image.color = (i == selectedIndex) ? Color.green : Color.white;
                }
            }
        }

        if (deleteModeButton != null)
        {
            deleteModeButton.image.color = isDeleteMode ? Color.red : Color.white;
        }

        if (groupButton != null)
        {
            groupButton.image.color = isGroupMode ? groupColors[currentGroupId - 1] : Color.white;
        }

        if (moveTriggerButton != null)
        {
            moveTriggerButton.image.color = isMoveTriggerMode ? Color.cyan : Color.white;
        }

        if (colorTriggerButton != null)
        {
            colorTriggerButton.image.color = isColorTriggerMode ? Color.magenta : Color.white;
        }

        if (pulseTriggerButton != null)
        {
            pulseTriggerButton.image.color = isPulseTriggerMode ? new Color(1f, 0.5f, 0f) : Color.white;
        }

        if (alphaTriggerButton != null)
        {
            alphaTriggerButton.image.color = isAlphaTriggerMode ? new Color(0.5f, 0.5f, 1f) : Color.white;
        }

        if (magnetToggleButton != null)
        {
            magnetToggleButton.image.color = isMagnetMode ? 
                new Color(1f, 0.5f, 0f) : Color.white;
        }

        UpdateOtherButtonColors();
    }

    void UpdateOtherButtonColors()
    {
        if (supabaseUploadButton != null)
        {
            bool hasObjects = placedObjects.Count > 0;
            bool isSupabaseReady = SupabaseManager.Instance != null;
            
            supabaseUploadButton.interactable = hasObjects && isSupabaseReady;
            
            if (!isSupabaseReady)
                supabaseUploadButton.image.color = Color.gray;
            else if (hasObjects)
                supabaseUploadButton.image.color = new Color(0.2f, 0.6f, 1f);
            else
                supabaseUploadButton.image.color = Color.gray;
        }

        if (supabaseBrowseButton != null)
        {
            bool isSupabaseReady = SupabaseManager.Instance != null;
            supabaseBrowseButton.interactable = isSupabaseReady;
            supabaseBrowseButton.image.color = isSupabaseReady ? new Color(0.8f, 0.4f, 1f) : Color.gray;
        }

        if (marketplaceButton != null)
        {
            bool isSupabaseReady = SupabaseManager.Instance != null;
            marketplaceButton.interactable = isSupabaseReady;
            marketplaceButton.image.color = isSupabaseReady ? new Color(0.4f, 0.8f, 0.2f) : Color.gray;
        }

        if (createPrefabsButton != null)
        {
            bool hasObjects = placedObjects.Count > 0;
            createPrefabsButton.interactable = hasObjects;
            createPrefabsButton.image.color = hasObjects ? new Color(0.8f, 0.4f, 0.8f) : Color.gray;
        }

        if (fixMaterialsButton != null)
        {
            fixMaterialsButton.image.color = Color.yellow;
        }

        if (exportToUnityButton != null)
        {
            bool hasObjects = placedObjects.Count > 0;
            exportToUnityButton.interactable = hasObjects;
            exportToUnityButton.image.color = hasObjects ? new Color(0.4f, 0.8f, 0.8f) : Color.gray;
        }
    }

    void UpdateModeText()
    {
        if (modeText != null)
        {
            string modeDescription = "";
            Color modeColor = Color.white;

            if (isDeleteMode)
            {
                modeDescription = "Режим: УДАЛЕНИЕ";
                modeColor = Color.red;
            }
            else if (isGroupMode)
            {
                modeDescription = $"Режим: ГРУППА {currentGroupId}";
                modeColor = groupColors[currentGroupId - 1];
            }
            else if (isMoveTriggerMode)
            {
                modeDescription = "Режим: МУВ-ТРИГГЕР";
                modeColor = Color.cyan;
            }
            else if (isColorTriggerMode)
            {
                modeDescription = "Режим: ЦВЕТ-ТРИГГЕР";
                modeColor = Color.magenta;
            }
            else if (isPulseTriggerMode)
            {
                modeDescription = "Режим: ПУЛЬС-ТРИГГЕР";
                modeColor = new Color(1f, 0.5f, 0f);
            }
            else if (isAlphaTriggerMode)
            {
                modeDescription = "Режим: АЛЬФА-ТРИГГЕР";
                modeColor = new Color(0.5f, 0.5f, 1f);
            }
            else if (selectedIndex != -1)
            {
                modeDescription = $"Режим: РАЗМЕЩЕНИЕ - {objectPrefabs[selectedIndex].name}";
                modeColor = Color.green;
            }
            else
            {
                modeDescription = "Режим: ВЫБОР";
                modeColor = Color.white;
            }

            if (selectedIndex != -1 && !isDeleteMode && !isGroupMode && 
                !isMoveTriggerMode && !isColorTriggerMode && !isPulseTriggerMode && !isAlphaTriggerMode)
            {
                modeDescription += $" [Вращение: {currentRotation:F0}° | Z: {currentZ:F2} | Цвет: {currentColor.r:F2},{currentColor.g:F2},{currentColor.b:F2}]";
            }

            if (isMagnetMode)
            {
                modeDescription += $" [МАГНИТ: {magnetRadius:F1}]";
                modeColor = Color.Lerp(modeColor, new Color(1f, 0.5f, 0f), 0.3f);
            }

            modeText.text = modeDescription;
            modeText.color = modeColor;
        }
    }

    public void ResetSelection()
    {
        ResetAllModesIncludingGroup();
        ShowNotification("Выбор сброшен", Color.white);
    }

    public void ToggleDeleteMode()
    {
        isDeleteMode = !isDeleteMode;
        if (isDeleteMode)
        {
            selectedIndex = -1;
            isMoveTriggerMode = false;
            isColorTriggerMode = false;
            isPulseTriggerMode = false;
            isAlphaTriggerMode = false;
        }
        UpdateButtonColors();
        UpdateModeText();
        
        ShowNotification("Режим удаления: " + (isDeleteMode ? "ВКЛ" : "ВЫКЛ"), 
                        isDeleteMode ? Color.red : Color.white);
    }

    public void ClearLevel()
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                if (obj.CompareTag("Centre") && !centreObjects.Contains(obj))
                {
                    centreObjects.Remove(obj);
                }
                Destroy(obj);
            }
        }
        placedObjects.Clear();
        
        foreach (var group in objectGroups)
        {
            group.Value.Clear();
        }
        
        UpdateLevelCounter();
        UpdateButtonColors();
        ShowNotification("Уровень очищен", Color.yellow);
    }

    private void IncreaseMagnetRadius()
    {
        magnetRadius = Mathf.Min(magnetRadius + 0.5f, 10.0f);
        UpdateMagnetRadiusUI();
    }

    private void DecreaseMagnetRadius()
    {
        magnetRadius = Mathf.Max(magnetRadius - 0.5f, 0.5f);
        UpdateMagnetRadiusUI();
    }

    private void UpdateMagnetRadiusUI()
    {
        if (magnetRadiusInput != null)
            magnetRadiusInput.text = magnetRadius.ToString("F1");
            
        if (isMagnetMode)
        {
            ShowNotification($"Радиус магнита: {magnetRadius:F1}", new Color(1f, 0.5f, 0f));
        }
    }

    private void SetGroupId(int groupId)
    {
        if (isGroupMode)
        {
            currentGroupId = Mathf.Clamp(groupId, 1, 8);
            if (groupIdInput != null) groupIdInput.text = currentGroupId.ToString();
            UpdateModeText();
            ShowNotification($"Активная группа: {currentGroupId}", groupColors[currentGroupId - 1]);
        }
    }

    private IEnumerator AutoCheckSupabaseConnection()
    {
        yield return new WaitForSeconds(1f);
        
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.TestConnection((success, message) => {
                ShowNotification(message, success ? Color.green : Color.red);
            });
        }
    }

    public void UploadToSupabase()
    {
        if (placedObjects.Count == 0)
        {
            ShowNotification("❌ Нельзя загрузить пустой уровень!", Color.red);
            return;
        }

        string levelName = "Без названия";
        if (levelNameInput != null && !string.IsNullOrEmpty(levelNameInput.text))
        {
            levelName = levelNameInput.text;
        }
        else
        {
            levelName = $"Уровень_{System.DateTime.Now:HHmmss}";
        }

        var levelDataWrapper = GetCurrentLevelDataForSupabase();
        string levelJson = JsonUtility.ToJson(levelDataWrapper);
        
        if (string.IsNullOrEmpty(levelJson) || levelJson == "{}")
        {
            ShowNotification("❌ Ошибка создания данных уровня", Color.red);
            return;
        }

        Debug.Log($"📦 JSON для сохранения: {levelJson}");

        var levelData = new SupabaseLevelData
        {
            level_name = levelName,
            author = GetPlayerName(),
            author_id = PlayerPrefs.GetString("PlayerID", ""),
            description = "Создан в редакторе уровней",
            level_data = levelJson,
            difficulty = CalculateDifficulty(),
            downloads = 0,
            likes = 0,
            version = 1
        };

        ShowNotification("📤 Загружаем уровень в облако...", Color.blue);

        SupabaseManager.Instance.UploadLevel(levelData, (success, message) => {
            if (success)
            {
                ShowNotification("✅ Уровень загружен в Supabase!", Color.green);
                if (levelNameInput != null) levelNameInput.text = "";
            }
            else
            {
                ShowNotification($"❌ {message}", Color.red);
            }
        });
    }

    public void TestSupabaseConnection()
    {
        ShowNotification("🔗 Тестируем подключение...", Color.blue);
        
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.TestConnection((success, message) => {
                ShowNotification(message, success ? Color.green : Color.red);
            });
        }
        else
        {
            ShowNotification("❌ SupabaseManager не инициализирован!", Color.red);
        }
    }

    public void ShowMarketplace()
    {
        if (marketplaceUI != null)
        {
            marketplaceUI.ShowMarketplace();
        }
        else
        {
            MarketplaceUI foundUI = FindObjectOfType<MarketplaceUI>();
            if (foundUI != null)
            {
                marketplaceUI = foundUI;
                marketplaceUI.ShowMarketplace();
            }
            else
            {
                ShowNotification("❌ MarketplaceUI не найден на сцене!", Color.red);
            }
        }
    }

    void RemoveLastObject()
    {
        if (placedObjects.Count > 0)
        {
            GameObject lastObj = placedObjects[placedObjects.Count - 1];
            placedObjects.RemoveAt(placedObjects.Count - 1);
            if (lastObj != null)
            {
                if (lastObj.CompareTag("Centre"))
                {
                    centreObjects.Remove(lastObj);
                    UpdateCentreVisuals();
                }
                
                RemoveObjectFromGroup(lastObj);
                Destroy(lastObj);
                UpdateLevelCounter();
                UpdateButtonColors();
                ShowNotification($"🗑️ Удален последний объект: {lastObj.name}", Color.yellow);
            }
        }
    }

    public void SaveLevel()
    {
        if (placedObjects.Count == 0)
        {
            ShowNotification("❌ Нельзя сохранить пустой уровень!", Color.red);
            return;
        }

        string levelName = "Level_" + (levelCount + 1);
        if (levelNameInput != null && !string.IsNullOrEmpty(levelNameInput.text))
        {
            levelName = levelNameInput.text;
        }

        var levelData = GetCurrentLevelDataForSupabase();
        string jsonData = JsonUtility.ToJson(levelData, true);
        
        LocalLevelData localLevelData = new LocalLevelData
        {
            level_name = levelName,
            author = GetPlayerName(),
            description = "Сохранен локально",
            level_data = jsonData,
            save_date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            version = 1
        };
        
        string finalJson = JsonUtility.ToJson(localLevelData, true);
        string fileName = "LocalLevel_" + levelName + ".json";
        string filePath = Path.Combine(Application.persistentDataPath, "LocalLevels", fileName);

        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, finalJson);
            
            levelCount++;
            PlayerPrefs.SetInt("LevelCount", levelCount);
            PlayerPrefs.Save();

            if (levelNameInput != null) levelNameInput.text = "";
            UpdateLevelCounter();
            
            ShowNotification($"✅ Уровень сохранен локально: {fileName}", Color.green);
            Debug.Log($"💾 Уровень сохранен в: {filePath}");
        }
        catch (System.Exception e)
        {
            ShowNotification($"❌ Ошибка сохранения: {e.Message}", Color.red);
        }
    }

int GetPrefabIndex(GameObject obj)
{
    if (obj == null) return -1;
    if (objectPrefabs == null) return -1;

    // 1. Проверяем маркер
    ObjectTypeMarker marker = obj.GetComponent<ObjectTypeMarker>();
    if (marker != null)
    {
        if (marker.prefabIndex >= 0 && marker.prefabIndex < objectPrefabs.Length)
            return marker.prefabIndex;
    }

    // 2. Запасной вариант — сравнение имён (для объектов, созданных вручную)
    string objName = obj.name.Replace("(Clone)", "").Trim();
    for (int i = 0; i < objectPrefabs.Length; i++)
    {
        if (objectPrefabs[i] == null) continue;
        if (objName == objectPrefabs[i].name)
            return i;
    }
    return -1;
}

    void UpdateLevelCounter()
    {
        if (levelCounterText != null)
        {
            levelCounterText.text = "Уровней: " + levelCount + "/100 | Объектов: " + placedObjects.Count + " | Центров: " + centreObjects.Count;
        }
    }

    public bool IsAnyObjectSelected()
    {
        return selectedIndex != -1;
    }

    private bool IsPointerOverUIObject()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private float CalculateDifficulty()
    {
        if (placedObjects.Count == 0) return 1.0f;
        
        int objectCount = placedObjects.Count;
        if (objectCount <= 5) return 1.0f;
        if (objectCount <= 10) return 2.0f;
        if (objectCount <= 20) return 3.0f;
        if (objectCount <= 30) return 4.0f;
        return 5.0f;
    }

    private string GetPlayerName()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Игрок_" + Random.Range(1000, 9999);
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
        return playerName;
    }

    private void ShowNotification(string message, Color color)
    {
        Debug.Log($"🔔 {message}");
        
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.color = color;
            StartCoroutine(ClearNotificationAfterDelay(3f));
        }
        
        if (modeText != null)
        {
            string originalText = modeText.text;
            Color originalColor = modeText.color;
            
            modeText.text = message;
            modeText.color = color;
            
            StartCoroutine(ResetModeText(originalText, originalColor, 3f));
        }
    }

    private IEnumerator ClearNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notificationText != null)
        {
            notificationText.text = "";
        }
    }

    private IEnumerator ResetModeText(string originalText, Color originalColor, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (modeText != null)
        {
            modeText.text = originalText;
            modeText.color = originalColor;
        }
    }

    private void AddColliderIfMissing(GameObject obj)
    {
        if (obj.GetComponent<Collider>() == null && obj.GetComponent<Collider2D>() == null)
        {
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                BoxCollider2D collider2D = obj.AddComponent<BoxCollider2D>();
                collider2D.size = spriteRenderer.bounds.size;
                Debug.Log($"📦 Добавлен BoxCollider2D к {obj.name}");
            }
            else
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (renderer is MeshRenderer)
                    {
                        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                            meshCollider.convex = true;
                            Debug.Log($"📦 Добавлен MeshCollider к {obj.name}");
                            return;
                        }
                    }
                    
                    BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                    if (renderer.bounds.size != Vector3.zero)
                    {
                        boxCollider.size = renderer.bounds.size;
                        boxCollider.center = renderer.bounds.center - obj.transform.position;
                    }
                    Debug.Log($"📦 Добавлен BoxCollider к {obj.name}");
                }
                else
                {
                    obj.AddComponent<BoxCollider>();
                    Debug.Log($"📦 Добавлен BoxCollider к {obj.name} (без рендерера)");
                }
            }
        }
    }

    private bool IsValidJson(string jsonString)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonString)) return false;
            
            jsonString = jsonString.Trim();
            if ((jsonString.StartsWith("{") && jsonString.EndsWith("}")) || 
                (jsonString.StartsWith("[") && jsonString.EndsWith("]")))
            {
                JsonUtility.FromJson<LevelDataWrapper>(jsonString);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private IEnumerator HighlightObject(GameObject obj)
    {
        if (obj == null) yield break;
        
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.green;
            
            yield return new WaitForSeconds(1.0f);
            
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }
        else
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.green;
                
                yield return new WaitForSeconds(1.0f);
                
                if (renderer != null)
                    renderer.material.color = originalColor;
            }
        }
    }

    private void FocusCameraOnLevel()
    {
        if (placedObjects.Count == 0) return;
        
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        Vector3 center = Vector3.zero;
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
                center += obj.transform.position;
        }
        center /= placedObjects.Count;
        
        mainCamera.transform.position = new Vector3(center.x, center.y, mainCamera.transform.position.z);
        
        Debug.Log($"📡 Камера перемещена к позиции: {center}");
    }

    public void AddCentreObject(GameObject centreObj)
    {
        if (centreObj != null && centreObj.CompareTag("Centre") && !centreObjects.Contains(centreObj))
        {
            centreObjects.Add(centreObj);
            UpdateCentreVisuals();
            Debug.Log($"✅ Добавлен новый объект Centre: {centreObj.name}");
        }
    }

    public void RefreshCentreObjects()
    {
        FindAllCentreObjects();
    }

    // ============= МЕТОДЫ ДЛЯ СОЗДАНИЯ ПРЕФАБОВ =============

    public void CreateStaticPrefabs()
    {
        if (placedObjects.Count == 0)
        {
            ShowNotification("❌ Нет объектов для создания префабов!", Color.red);
            return;
        }

#if UNITY_EDITOR
        string folderPath = "Assets/Prefabs/LevelObjects/";
        if (prefabsFolderInput != null && !string.IsNullOrEmpty(prefabsFolderInput.text))
        {
            folderPath = prefabsFolderInput.text;
            if (!folderPath.EndsWith("/")) folderPath += "/";
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parentFolder = "Assets";
            string[] folders = folderPath.Replace("Assets/", "").Split('/');
            
            for (int i = 0; i < folders.Length; i++)
            {
                string currentPath = parentFolder + "/" + folders[i];
                if (i > 0)
                {
                    currentPath = parentFolder;
                    for (int j = 0; j <= i; j++)
                    {
                        currentPath += "/" + folders[j];
                    }
                }
                
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);
                }
                parentFolder = currentPath;
            }
        }

        int createdPrefabs = 0;
        int failedPrefabs = 0;
        
        Dictionary<string, List<GameObject>> objectsByType = new Dictionary<string, List<GameObject>>();
        
        foreach (GameObject obj in placedObjects)
        {
            if (obj == null) continue;
            
            if (obj.GetComponent<MoveTrigger>() != null || 
                obj.GetComponent<ColorTrigger>() != null ||
                obj.GetComponent<PulseTrigger>() != null ||
                obj.GetComponent<AlphaTrigger>() != null) continue;
            
            string prefabName = GetPrefabNameForCreation(obj);
            if (string.IsNullOrEmpty(prefabName)) continue;
            
            if (!objectsByType.ContainsKey(prefabName))
            {
                objectsByType[prefabName] = new List<GameObject>();
            }
            objectsByType[prefabName].Add(obj);
        }
        
        foreach (var kvp in objectsByType)
        {
            string prefabName = kvp.Key;
            List<GameObject> objects = kvp.Value;
            
            if (objects.Count > 0)
            {
                GameObject sampleObject = objects[0];
                GameObject prefabObject = CreatePrefabGameObject(sampleObject, prefabName);
                
                string prefabPath = folderPath + prefabName + ".prefab";
                
                try
                {
                    if (File.Exists(prefabPath.Replace("Assets/", Application.dataPath + "/")))
                    {
                        AssetDatabase.DeleteAsset(prefabPath);
                    }
                    
                    PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
                    createdPrefabs++;
                    
                    Debug.Log($"✅ Создан префаб: {prefabPath}");
                    
                    DestroyImmediate(prefabObject);
                }
                catch (System.Exception e)
                {
                    failedPrefabs++;
                    Debug.LogError($"❌ Ошибка создания префаба {prefabName}: {e.Message}");
                    
                    if (prefabObject != null)
                        DestroyImmediate(prefabObject);
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string message = $"✅ Создано префабов: {createdPrefabs}";
        if (failedPrefabs > 0) message += $", ошибок: {failedPrefabs}";
        
        ShowNotification(message, failedPrefabs == 0 ? Color.green : Color.yellow);
        
        if (prefabCreationStatus != null)
        {
            prefabCreationStatus.text = message;
            prefabCreationStatus.color = failedPrefabs == 0 ? Color.green : Color.yellow;
        }
#else
        ShowNotification("❌ Создание префабов доступно только в редакторе Unity!", Color.red);
#endif
    }

    private string GetPrefabNameForCreation(GameObject obj)
    {
        if (obj == null) return "";
        
        string name = obj.name.Replace("(Clone)", "").Trim();
        
        foreach (GameObject prefab in objectPrefabs)
        {
            if (prefab != null && prefab.name == name)
            {
                return name;
            }
        }
        
        return name;
    }

    private GameObject CreatePrefabGameObject(GameObject source, string prefabName)
    {
        GameObject prefabObject = new GameObject(prefabName + "_Prefab");
        
        CopyComponentsForPrefab(source, prefabObject);
        
        return prefabObject;
    }

    private void CopyComponentsForPrefab(GameObject source, GameObject target)
    {
        if (source == null || target == null) return;
        
        target.transform.position = Vector3.zero;
        target.transform.rotation = Quaternion.identity;
        target.transform.localScale = source.transform.localScale;
        
        MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
        MeshRenderer sourceMeshRenderer = source.GetComponent<MeshRenderer>();
        
        if (sourceMeshFilter != null && sourceMeshRenderer != null)
        {
            MeshFilter targetMeshFilter = target.AddComponent<MeshFilter>();
            MeshRenderer targetMeshRenderer = target.AddComponent<MeshRenderer>();
            
            targetMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
            
            if (sourceMeshRenderer.sharedMaterial != null)
            {
                Material newMaterial = new Material(sourceMeshRenderer.sharedMaterial);
                newMaterial.color = GetObjectColor(source);
                targetMeshRenderer.sharedMaterial = newMaterial;
            }
        }
        
        SpriteRenderer sourceSpriteRenderer = source.GetComponent<SpriteRenderer>();
        if (sourceSpriteRenderer != null)
        {
            SpriteRenderer targetSpriteRenderer = target.AddComponent<SpriteRenderer>();
            targetSpriteRenderer.sprite = sourceSpriteRenderer.sprite;
            targetSpriteRenderer.color = GetObjectColor(source);
            targetSpriteRenderer.sortingOrder = sourceSpriteRenderer.sortingOrder;
            targetSpriteRenderer.sortingLayerID = sourceSpriteRenderer.sortingLayerID;
        }
        
        Collider sourceCollider = source.GetComponent<Collider>();
        if (sourceCollider != null)
        {
            if (sourceCollider is BoxCollider)
            {
                BoxCollider targetCollider = target.AddComponent<BoxCollider>();
                BoxCollider sourceBox = (BoxCollider)sourceCollider;
                targetCollider.center = sourceBox.center;
                targetCollider.size = sourceBox.size;
                targetCollider.isTrigger = sourceBox.isTrigger;
            }
            else if (sourceCollider is SphereCollider)
            {
                SphereCollider targetCollider = target.AddComponent<SphereCollider>();
                SphereCollider sourceSphere = (SphereCollider)sourceCollider;
                targetCollider.center = sourceSphere.center;
                targetCollider.radius = sourceSphere.radius;
                targetCollider.isTrigger = sourceSphere.isTrigger;
            }
            else if (sourceCollider is CapsuleCollider)
            {
                CapsuleCollider targetCollider = target.AddComponent<CapsuleCollider>();
                CapsuleCollider sourceCapsule = (CapsuleCollider)sourceCollider;
                targetCollider.center = sourceCapsule.center;
                targetCollider.radius = sourceCapsule.radius;
                targetCollider.height = sourceCapsule.height;
                targetCollider.direction = sourceCapsule.direction;
                targetCollider.isTrigger = sourceCapsule.isTrigger;
            }
            else if (sourceCollider is MeshCollider)
            {
                MeshCollider targetCollider = target.AddComponent<MeshCollider>();
                MeshCollider sourceMesh = (MeshCollider)sourceCollider;
                targetCollider.sharedMesh = sourceMesh.sharedMesh;
                targetCollider.convex = sourceMesh.convex;
                targetCollider.isTrigger = sourceMesh.isTrigger;
            }
        }
        
        Collider2D sourceCollider2D = source.GetComponent<Collider2D>();
        if (sourceCollider2D != null)
        {
            if (sourceCollider2D is BoxCollider2D)
            {
                BoxCollider2D targetCollider = target.AddComponent<BoxCollider2D>();
                BoxCollider2D sourceBox = (BoxCollider2D)sourceCollider2D;
                targetCollider.offset = sourceBox.offset;
                targetCollider.size = sourceBox.size;
                targetCollider.isTrigger = sourceBox.isTrigger;
            }
            else if (sourceCollider2D is CircleCollider2D)
            {
                CircleCollider2D targetCollider = target.AddComponent<CircleCollider2D>();
                CircleCollider2D sourceCircle = (CircleCollider2D)sourceCollider2D;
                targetCollider.offset = sourceCircle.offset;
                targetCollider.radius = sourceCircle.radius;
                targetCollider.isTrigger = sourceCircle.isTrigger;
            }
            else if (sourceCollider2D is PolygonCollider2D)
            {
                PolygonCollider2D targetCollider = target.AddComponent<PolygonCollider2D>();
                PolygonCollider2D sourcePoly = (PolygonCollider2D)sourceCollider2D;
                targetCollider.offset = sourcePoly.offset;
                targetCollider.points = sourcePoly.points;
                targetCollider.isTrigger = sourcePoly.isTrigger;
            }
        }
        
        ObjectGroup sourceObjectGroup = source.GetComponent<ObjectGroup>();
        if (sourceObjectGroup != null)
        {
            ObjectGroup targetObjectGroup = target.AddComponent<ObjectGroup>();
            targetObjectGroup.groupId = sourceObjectGroup.groupId;
        }
        
        target.tag = source.tag;
        target.layer = source.layer;
        
        CopyChildObjectsForPrefab(source, target);
    }

    private void CopyChildObjectsForPrefab(GameObject source, GameObject target)
    {
        if (source.transform.childCount == 0) return;
        
        for (int i = 0; i < source.transform.childCount; i++)
        {
            Transform child = source.transform.GetChild(i);
            GameObject childCopy = new GameObject(child.name);
            childCopy.transform.SetParent(target.transform);
            childCopy.transform.localPosition = child.localPosition;
            childCopy.transform.localRotation = child.localRotation;
            childCopy.transform.localScale = child.localScale;
            
            CopyComponentsForPrefab(child.gameObject, childCopy);
        }
    }

    private Color GetObjectColor(GameObject obj)
    {
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.color;
        }
        
        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            return meshRenderer.sharedMaterial.color;
        }
        
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            return renderer.material.color;
        }
        
        return Color.white;
    }

    public void FixAllPurpleMaterials()
    {
#if UNITY_EDITOR
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                FixPurpleMaterials(obj);
            }
        }
        
        ShowNotification("✅ Материалы исправлены", Color.green);
#else
        ShowNotification("❌ Исправление материалов доступно только в редакторе Unity!", Color.red);
#endif
    }

    private void FixPurpleMaterials(GameObject obj)
    {
        if (obj == null) return;
        
        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            if (meshRenderer.sharedMaterial.shader.name.Contains("Error") || 
                meshRenderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                Material standardMaterial = new Material(Shader.Find("Standard"));
                Color objectColor = GetObjectColor(obj);
                standardMaterial.color = objectColor;
                meshRenderer.sharedMaterial = standardMaterial;
            }
        }
        
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            if (renderer.sharedMaterial.shader.name.Contains("Error") || 
                renderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                Material standardMaterial = new Material(Shader.Find("Standard"));
                Color objectColor = GetObjectColor(obj);
                standardMaterial.color = objectColor;
                renderer.sharedMaterial = standardMaterial;
            }
        }
        
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            FixPurpleMaterials(obj.transform.GetChild(i).gameObject);
        }
    }

    public void ExportCurrentLevelToUnityPackage()
    {
#if UNITY_EDITOR
        if (placedObjects.Count == 0)
        {
            ShowNotification("❌ Нет объектов для экспорта!", Color.red);
            return;
        }

        string levelName = "Level_Export";
        if (levelNameInput != null && !string.IsNullOrEmpty(levelNameInput.text))
        {
            levelName = levelNameInput.text.Replace(" ", "_");
        }

        string packagePath = EditorUtility.SaveFilePanel("Экспорт Unity Package", "", levelName, "unitypackage");
        
        if (string.IsNullOrEmpty(packagePath)) return;

        List<string> assetPaths = new List<string>();
        
        string prefabsFolder = "Assets/Prefabs/LevelObjects/";
        if (!AssetDatabase.IsValidFolder(prefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "LevelObjects");
        }

        foreach (GameObject obj in placedObjects)
        {
            if (obj == null) continue;
            
            if (obj.GetComponent<MoveTrigger>() != null || 
                obj.GetComponent<ColorTrigger>() != null ||
                obj.GetComponent<PulseTrigger>() != null ||
                obj.GetComponent<AlphaTrigger>() != null) continue;
            
            string prefabName = GetPrefabNameForCreation(obj);
            if (string.IsNullOrEmpty(prefabName)) continue;
            
            GameObject prefabObject = CreatePrefabGameObject(obj, prefabName);
            string prefabPath = prefabsFolder + prefabName + ".prefab";
            
            PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
            assetPaths.Add(prefabPath);
            
            DestroyImmediate(prefabObject);
        }

        if (assetPaths.Count > 0)
        {
            AssetDatabase.ExportPackage(assetPaths.ToArray(), packagePath, ExportPackageOptions.Default);
            ShowNotification($"✅ Unity Package экспортирован: {Path.GetFileName(packagePath)}", Color.green);
        }
        else
        {
            ShowNotification("❌ Не удалось создать ресурсы для экспорта", Color.red);
        }
#else
        ShowNotification("❌ Экспорт Unity Package доступен только в редакторе Unity!", Color.red);
#endif
    }

    public void CreatePrefabFromSelectedObject()
    {
#if UNITY_EDITOR
        if (selectedIndex >= 0 && selectedIndex < objectPrefabs.Length && objectPrefabs[selectedIndex] != null)
        {
            string prefabName = objectPrefabs[selectedIndex].name;
            GameObject prefabObject = CreatePrefabGameObject(objectPrefabs[selectedIndex], prefabName);
            
            string folderPath = "Assets/Prefabs/LevelObjects/";
            if (prefabsFolderInput != null && !string.IsNullOrEmpty(prefabsFolderInput.text))
            {
                folderPath = prefabsFolderInput.text;
                if (!folderPath.EndsWith("/")) folderPath += "/";
            }
            
            string prefabPath = folderPath + prefabName + ".prefab";
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "LevelObjects");
            }
            
            PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
            DestroyImmediate(prefabObject);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            ShowNotification($"✅ Префаб создан: {prefabName}", Color.green);
        }
        else
        {
            ShowNotification("❌ Сначала выберите объект для создания префаба", Color.red);
        }
#else
        ShowNotification("❌ Создание префабов доступно только в редакторе Unity!", Color.red);
#endif
    }
}

// ============= ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ =============

public class TriggerActivator : MonoBehaviour
{
    public LevelCreator levelCreator;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"🎯 Игрок активировал триггер: {gameObject.name}");
            levelCreator.ActivateTrigger(gameObject);
        }
    }
}

[System.Serializable]
public class ObjectGroup : MonoBehaviour
{
    public int groupId = 1;
}

[System.Serializable]
public class MoveTrigger : MonoBehaviour
{
    public int targetGroupId = 1;
    public float moveDuration = 1.0f;
    public Vector3 targetPosition = Vector3.zero;
    public EaseType moveEaseType = EaseType.EaseInOut;
}

[System.Serializable]
public class ColorTrigger : MonoBehaviour
{
    public int targetGroupId = 1;
    public float colorDuration = 1.0f;
    public Color targetColor = Color.white;
    public EaseType colorEaseType = EaseType.EaseInOut;
}

[System.Serializable]
public class PulseTrigger : MonoBehaviour
{
    public int targetGroupId = 1;
    public float pulseDuration = 1.0f;
    public PulseType pulseType = PulseType.GrowAndShrink;
    public float pulseScale = 1.5f;
}

[System.Serializable]
public class AlphaTrigger : MonoBehaviour
{
    public int targetGroupId = 1;
    public float alphaDuration = 1.0f;
    public float targetAlpha = 0.5f;
    public EaseType alphaEaseType = EaseType.EaseInOut;
}

[System.Serializable]
public class MagnetSettingsData
{
    public bool useMagnet = false;
    public float magnetRadius = 3.0f;
    public bool showCentres = true;
}

public enum EaseType
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut
}

public enum PulseType
{
    Grow,
    Shrink,
    GrowAndShrink
}

[System.Serializable]
public class GroupData
{
    public int groupId;
    public int objectCount;
}

[System.Serializable]
public class TriggerData
{
    public string triggerType;
    public int targetGroupId;
    public float duration;
    public float x, y, z;
    
    public float targetX, targetY;
    
    public float colorR, colorG, colorB, colorA;
    
    public float pulseScale;
    
    public float targetAlpha;
}

[System.Serializable]
public class LevelDataWrapper
{
    public List<LevelObjectData> objects;
    public List<GroupData> groups;
    public List<TriggerData> triggers;
    public MagnetSettingsData magnetSettings;
}

[System.Serializable]
public class LevelObjectData
{
    public int objectType;
    public float x, y, z;
    public float rotation = 0f;
    public float colorR = 1f;
    public float colorG = 1f;
    public float colorB = 1f;
    public float colorA = 1f;
    public int groupId;
    public bool isCentre = false;
}

[System.Serializable]
public class EditorLevelData
{
    public string levelName;
    public List<EditorObjectData> objects;
}

[System.Serializable]
public class EditorObjectData
{
    public int objectType;
    public float posX;
    public float posY;
    public float posZ;
}

public class ColorPicker : MonoBehaviour
{
    public Color CurrentColor { get; set; } = Color.red;
}

[System.Serializable]
public class LocalLevelData
{
    public string level_name;
    public string author;
    public string description;
    public string level_data;
    public string save_date;
    public int version;
}