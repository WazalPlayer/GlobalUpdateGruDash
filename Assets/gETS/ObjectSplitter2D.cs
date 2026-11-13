using UnityEngine;

public class ObjectSplitter2D : MonoBehaviour
{
    [Header("Настройки разлёта")]
    [SerializeField] private GameObject fragmentPrefab; // Префаб фрагмента
    [SerializeField] private int fragmentCount = 10; // Количество копий
    [SerializeField] private float explosionForce = 10f; // Сила разлёта
    [SerializeField] private float minSize = 0.3f; // Минимальный размер фрагмента
    [SerializeField] private float maxSize = 0.7f; // Максимальный размер
    [SerializeField] private Color[] fragmentColors; // Возможные цвета (опционально)

    [Header("Автоактивация")]
    [SerializeField] private bool activateOnStart = false; // Активировать при старте
    [SerializeField] private float activationDelay = 0f; // Задержка активации

    [Header("Временные параметры")]
    [SerializeField] private bool destroyAfterSplit = true; // Уничтожить исходник
    [SerializeField] private float fragmentLifetime = 3f; // Время жизни фрагментов
    [SerializeField] private bool fadeOut = true; // Постепенное исчезновение

    void Start()
    {
        if (activateOnStart)
        {
            Invoke("ActivateSplit", activationDelay);
        }
    }

    // Метод для активации из других скриптов
    public void ActivateSplit()
    {
        if (fragmentPrefab == null)
        {
            Debug.LogWarning("Не назначен префаб фрагмента!");
            return;
        }

        for (int i = 0; i < fragmentCount; i++)
        {
            CreateFragment();
        }

        if (destroyAfterSplit)
        {
            Destroy(gameObject);
        }
    }

    private void CreateFragment()
    {
        // Создаём фрагмент
        GameObject fragment = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
        
        // Задаём случайный размер
        float randomSize = Random.Range(minSize, maxSize);
        fragment.transform.localScale = new Vector3(randomSize, randomSize, 1f);

        // Случайный цвет, если заданы цвета
        if (fragmentColors != null && fragmentColors.Length > 0)
        {
            SpriteRenderer sr = fragment.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = fragmentColors[Random.Range(0, fragmentColors.Length)];
            }
        }

        // Добавляем физику
        Rigidbody2D rb = fragment.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = fragment.AddComponent<Rigidbody2D>();
        }

        // Случайное направление с силой
        Vector2 randomDirection = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
        
        float randomForce = explosionForce * Random.Range(0.8f, 1.2f);
        rb.AddForce(randomDirection * randomForce, ForceMode2D.Impulse);

        // Случайное вращение
        float randomTorque = Random.Range(-100f, 100f);
        rb.AddTorque(randomTorque);

        // Уничтожение фрагмента через время
        if (fragmentLifetime > 0)
        {
            StartCoroutine(DestroyFragment(fragment));
        }
    }

    private System.Collections.IEnumerator DestroyFragment(GameObject fragment)
    {
        yield return new WaitForSeconds(fragmentLifetime);

        if (fadeOut && fragment != null)
        {
            SpriteRenderer sr = fragment.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Плавное исчезновение
                float fadeTime = 0.5f;
                float elapsedTime = 0f;
                Color originalColor = sr.color;

                while (elapsedTime < fadeTime)
                {
                    elapsedTime += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                    sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    yield return null;
                }
            }
        }

        if (fragment != null)
        {
            Destroy(fragment);
        }
    }

    // Для тестирования - активация по нажатию пробела
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateSplit();
        }
    }
}