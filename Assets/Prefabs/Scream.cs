using UnityEngine;
using System.Collections;

public class Scream : MonoBehaviour
{
    // Ссылка на аудиоклип, который будет воспроизводиться
    public AudioClip soundEffect;

    // Ссылка на объект, который будет активирован
    public GameObject objectToActivate;

    // Длительность активации объекта в секундах
    public float activationDuration = 5f;

    private AudioSource audioSource;

    void Start()
    {
        // Получаем компонент AudioSource или добавляем его, если его нет
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Отключаем объект для активации, если он изначально включен
        if (objectToActivate != null && objectToActivate.activeSelf)
        {
            objectToActivate.SetActive(false);
        }
    }

    // Функция, вызываемая при столкновении с другим коллайдером
    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что столкновение произошло с объектом, который должен вызывать активацию
        // Можно добавить дополнительные условия для проверки тега или слоя объекта
        if (other.gameObject.CompareTag("Player")) // Пример проверки тега
        {
            // Воспроизводим звук, если он задан
            if (soundEffect != null)
            {
                audioSource.PlayOneShot(soundEffect);
            }

            // Активируем объект на заданное время
            if (objectToActivate != null)
            {
                StartCoroutine(ActivateObject());
            }
        }
    }

    // Корутина для активации объекта на определенное время
    IEnumerator ActivateObject()
    {
        objectToActivate.SetActive(true);
        yield return new WaitForSeconds(activationDuration);
        objectToActivate.SetActive(false);
    }
}