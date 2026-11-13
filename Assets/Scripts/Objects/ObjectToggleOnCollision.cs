using UnityEngine;

public class ObjectToggleOnCollision : MonoBehaviour
{
    [Header("Objects to Disable")]
    [Tooltip("Objects that will be disabled on collision with Player")]
    public GameObject[] objectsToDisable;

    [Header("Objects to Enable")]
    [Tooltip("Objects that will be enabled on collision with Player")]
    public GameObject[] objectsToEnable;

    [Header("Collision Settings")]
    [Tooltip("Should the collision work only once?")]
    public bool triggerOnce = true;
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && (!triggerOnce || !hasTriggered))
        {
            ToggleObjects();
            hasTriggered = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && (!triggerOnce || !hasTriggered))
        {
            ToggleObjects();
            hasTriggered = true;
        }
    }

    private void ToggleObjects()
    {
        // Disable specified objects
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
            else
            {
                Debug.LogWarning("One of the objects to disable is not assigned!", this);
            }
        }

        // Enable specified objects
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
            else
            {
                Debug.LogWarning("One of the objects to enable is not assigned!", this);
            }
        }
    }

    // Optional: Draw gizmos to visualize the trigger area
    private void OnDrawGizmos()
    {
        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(transform.position, GetComponent<Collider2D>().bounds.size);
        }
    }
}