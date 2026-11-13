using UnityEngine;

// Keeps a UI panel inside the phone's safe area (avoids notches, rounded corners
// and the gesture bar). Attach to a full-screen panel or to a UI root object.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Apply(true);
    }

    private void Update()
    {
        // Re-apply when the safe area, resolution or orientation changes (rotation, fold, etc.)
        Apply(false);
    }

    private void Apply(bool force)
    {
        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        if (!force && safe == lastSafeArea && size == lastScreenSize && orientation == lastOrientation)
            return;

        lastSafeArea = safe;
        lastScreenSize = size;
        lastOrientation = orientation;

        if (size.x <= 0 || size.y <= 0)
            return;

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;
        anchorMin.x /= size.x;
        anchorMin.y /= size.y;
        anchorMax.x /= size.x;
        anchorMax.y /= size.y;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
