using UnityEngine;

public class EditorCameraController : MonoBehaviour
{
    [Header("Настройки камеры")]
    public float moveSpeed = 10f;
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 20f;
    public float dragSpeed = 0.1f;
    
    private Camera cam;
    private Vector3 lastTouchPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleTouch();
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector3 delta = cam.ScreenToWorldPoint(touch.position) - cam.ScreenToWorldPoint(lastTouchPosition);
                transform.position -= delta;
                lastTouchPosition = touch.position;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;

            float prevMagnitude = (touch1PrevPos - touch2PrevPos).magnitude;
            float currentMagnitude = (touch1.position - touch2.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            Zoom(difference * 0.01f);
        }
    }

    void Zoom(float increment)
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - increment * zoomSpeed, minZoom, maxZoom);
    }

    public void ZoomIn()
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - zoomSpeed, minZoom, maxZoom);
    }

    public void ZoomOut()
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + zoomSpeed, minZoom, maxZoom);
    }
}