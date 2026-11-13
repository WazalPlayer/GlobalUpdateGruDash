using UnityEngine;

public class CameraEdit : MonoBehaviour
{
    public float dragSpeed = 0.01f;
    public float zoomSpeed = 0.5f;
    public float minZoom = 2f;
    public float maxZoom = 20f;
    
    private Camera cam;
    private Vector3 lastPanPosition;
    private bool isDragging = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Обработка тач-ввода (мобильные устройства)
        if (Input.touchCount > 0)
        {
            HandleTouch();
        }
        // Обработка ввода с мыши (ПК)
        else
        {
            HandleMouse();
        }
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                lastPanPosition = cam.ScreenToWorldPoint(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector3 currentPos = cam.ScreenToWorldPoint(touch.position);
                Vector3 delta = lastPanPosition - currentPos;
                transform.position += delta;
                lastPanPosition = cam.ScreenToWorldPoint(touch.position);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;

            float prevDistance = Vector2.Distance(touch1PrevPos, touch2PrevPos);
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);
            float deltaDistance = prevDistance - currentDistance;

            cam.orthographicSize += deltaDistance * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandleMouse()
    {
        // Перетаскивание камеры (ПК)
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            lastPanPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }
        else if ((Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) && isDragging)
        {
            Vector3 currentPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = lastPanPosition - currentPos;
            transform.position += delta;
            lastPanPosition = cam.ScreenToWorldPoint(Input.mousePosition);
        }
        
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
        }

        // Зум колесом мыши (ПК)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            cam.orthographicSize -= scroll * zoomSpeed * 10f; // Умножение на 10 для более быстрого зума
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    public void ZoomIn()
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - 1f, minZoom, maxZoom);
    }

    public void ZoomOut()
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + 1f, minZoom, maxZoom);
    }
}