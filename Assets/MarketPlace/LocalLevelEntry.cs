using UnityEngine;
using UnityEngine.UI;

public class LocalLevelEntry : MonoBehaviour
{
    public Text nameText;         // Название уровня
    public Text infoText;         // Автор, дата
    public Button loadButton;     // Кнопка "Загрузить"
    public Button deleteButton;   // Кнопка "Удалить"

    private string filePath;
    private LevelMenu menu;

    public void Setup(LocalLevelMeta meta, string path, LevelMenu parentMenu)
    {
        nameText.text = meta.level_name;
        infoText.text = $"{meta.author} | {meta.save_date}";
        filePath = path;
        menu = parentMenu;

        // Назначаем обработчики
        loadButton.onClick.AddListener(OnLoadClick);
        deleteButton.onClick.AddListener(OnDeleteClick);
    }

    void OnLoadClick()
    {
        if (menu != null)
            menu.LoadLevel(filePath);
    }

    void OnDeleteClick()
    {
        if (menu != null)
            menu.DeleteLevel(filePath);
    }
}