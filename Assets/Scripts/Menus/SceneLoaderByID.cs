using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderByID : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ID of the scene to load (check in Build Settings)")]
    public int sceneIDToLoad = 0;

    [Header("Collision Settings")]
    [Tooltip("Should the object have collision detection?")]
    public bool useCollisionDetection = true;
    [Tooltip("Should it load on trigger enter?")]
    public bool loadOnTriggerEnter = true;
    [Tooltip("Should it load on collision enter?")]
    public bool loadOnCollisionEnter = true;
    [Tooltip("Should it work only once?")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    // Optional: Load scene immediately without collision
    public void LoadSceneNow()
    {
        LoadTargetScene();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (useCollisionDetection && loadOnTriggerEnter && 
            collision.CompareTag("Player") && (!triggerOnce || !hasTriggered))
        {
            LoadTargetScene();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (useCollisionDetection && loadOnCollisionEnter && 
            collision.gameObject.CompareTag("Player") && (!triggerOnce || !hasTriggered))
        {
            LoadTargetScene();
        }
    }

    private void LoadTargetScene()
    {
        if (sceneIDToLoad < 0 || sceneIDToLoad >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Invalid scene ID: {sceneIDToLoad}. Please check Build Settings.");
            return;
        }

        hasTriggered = true;
        SceneManager.LoadScene(sceneIDToLoad);
    }

    // Editor validation
    private void OnValidate()
    {
        if (sceneIDToLoad < 0)
        {
            sceneIDToLoad = 0;
            Debug.LogWarning("Scene ID cannot be negative. Reset to 0.");
        }
    }
}