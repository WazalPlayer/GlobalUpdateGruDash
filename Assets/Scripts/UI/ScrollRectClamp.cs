using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Во всех сценах делает все ScrollRect "зажатыми": нельзя прокрутить дальше содержимого
// и нет пружинящего отскока за край. Работает без правок сцен.
public static class ScrollRectClamp
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        Apply();
    }

    // Можно вызвать вручную после создания списков динамически (например, LevelMenu.RefreshList)
    public static void Apply()
    {
        ScrollRect[] rects = Object.FindObjectsOfType<ScrollRect>(true);
        foreach (ScrollRect rect in rects)
        {
            ClampRect(rect);
        }
    }

    public static void ClampRect(ScrollRect rect)
    {
        if (rect == null) return;
        rect.movementType = ScrollRect.MovementType.Clamped;
        rect.elasticity = 0f;
        rect.inertia = true;
    }
}
