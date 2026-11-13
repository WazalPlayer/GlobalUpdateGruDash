using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GroupShowHideController : MonoBehaviour
{
    [System.Serializable]
    public class ObjectGroup
    {
        public string groupName;
        public List<GameObject> objects;
        public bool isVisible = true;
    }

    [Header("Группы объектов")]
    public List<ObjectGroup> objectGroups = new List<ObjectGroup>();

    [Header("Настройки кнопки")]
    public Button toggleButton;
    public Text buttonText;

    private int currentGroupIndex = 0;

private void Start()
{
    // Находим компоненты если не установлены
    if (toggleButton == null)
        toggleButton = GetComponent<Button>();
    if (buttonText == null && toggleButton != null)
        buttonText = toggleButton.GetComponentInChildren<Text>();

    // Отключаем навигацию (клавиши Space, Enter и стрелки не будут воздействовать)
    if (toggleButton != null)
    {
        Navigation nav = toggleButton.navigation;
        nav.mode = Navigation.Mode.None;
        toggleButton.navigation = nav;
    }

    // Инициализируем видимость
    UpdateAllGroupsVisibility();

    // Подписываемся на событие
    if (toggleButton != null)
    {
        toggleButton.onClick.AddListener(OnToggleButtonClick);
    }

    UpdateButtonUI();
}

    /// <summary>
    /// Обработчик нажатия кнопки переключения
    /// </summary>
    public void OnToggleButtonClick()
    {
        if (objectGroups.Count == 0) return;

        // Переключаем текущую группу
        ToggleCurrentGroup();
        
        // Обновляем UI
        UpdateButtonUI();
    }

    /// <summary>
    /// Переключить видимость текущей группы
    /// </summary>
    private void ToggleCurrentGroup()
    {
        if (currentGroupIndex < objectGroups.Count)
        {
            objectGroups[currentGroupIndex].isVisible = !objectGroups[currentGroupIndex].isVisible;
            SetGroupVisibility(objectGroups[currentGroupIndex]);
        }
    }

    /// <summary>
    /// Установить видимость для группы
    /// </summary>
    private void SetGroupVisibility(ObjectGroup group)
    {
        foreach (GameObject obj in group.objects)
        {
            if (obj != null)
            {
                obj.SetActive(group.isVisible);
            }
        }
    }

    /// <summary>
    /// Обновить видимость всех групп
    /// </summary>
    private void UpdateAllGroupsVisibility()
    {
        foreach (ObjectGroup group in objectGroups)
        {
            SetGroupVisibility(group);
        }
    }

    /// <summary>
    /// Обновить интерфейс кнопки
    /// </summary>
    private void UpdateButtonUI()
    {
        if (buttonText != null && objectGroups.Count > 0)
        {
            var currentGroup = objectGroups[currentGroupIndex];
            buttonText.text = $"{currentGroup.groupName}";
        }
    }

    /// <summary>
    /// Переключить на следующую группу
    /// </summary>
    public void NextGroup()
    {
        if (objectGroups.Count > 0)
        {
            currentGroupIndex = (currentGroupIndex + 1) % objectGroups.Count;
            UpdateButtonUI();
        }
    }

    /// <summary>
    /// Переключить на предыдущую группу
    /// </summary>
    public void PreviousGroup()
    {
        if (objectGroups.Count > 0)
        {
            currentGroupIndex = (currentGroupIndex - 1 + objectGroups.Count) % objectGroups.Count;
            UpdateButtonUI();
        }
    }

    void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(OnToggleButtonClick);
        }
    }
}