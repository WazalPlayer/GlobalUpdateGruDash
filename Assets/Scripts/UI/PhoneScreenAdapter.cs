using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runs automatically in every scene (no scene edits needed).
// - Picks the right CanvasScaler "match" for portrait phones (match width)
//   and landscape screens (match height), so UI is not cropped on odd aspect ratios.
// - Caps the frame rate at 60 and stops the screen from sleeping during play.
public static class PhoneScreenAdapter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SceneManager.sceneLoaded += (scene, mode) => AdaptCanvases();
        AdaptCanvases();
    }

    private static void AdaptCanvases()
    {
        // На ПК и в редакторе оставляем настройки сцены как есть,
        // иначе UI становится мельче (это и было причиной уменьшения интерфейса).
        if (!Application.isMobilePlatform)
            return;

        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
        // Референс квадратный (1000x1000): масштабируем по меньшей стороне экрана, чтобы UI не вылезал за край
        float match = aspect >= 1f ? 1f : 0f; // горизонтально — по высоте, вертикально — по ширине

        CanvasScaler[] scalers = Object.FindObjectsOfType<CanvasScaler>(true);
        foreach (CanvasScaler scaler in scalers)
        {
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                continue;

            scaler.matchWidthOrHeight = match;
        }
    }
}
