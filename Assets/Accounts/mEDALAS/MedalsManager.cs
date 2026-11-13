// MedalsManager.cs
using UnityEngine;
using System.Collections.Generic;

public class MedalsManager : MonoBehaviour
{
    public static MedalsManager Instance { get; private set; }

    [Header("Медали")]
    public Sprite[] medalSprites;      // назначаем в инспекторе (индекс = ID медали)
    public string[] medalNames;        // опционально, названия медалей

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Возвращает список ID медалей, которые есть у игрока
    public List<int> GetOwnedMedals()
    {
        List<int> owned = new List<int>();
        int mask = AccountsManager.Instance.GetPlayerMedals();
        for (int i = 0; i < 32; i++) // максимум 32 медали
        {
            if ((mask & (1 << i)) != 0)
                owned.Add(i);
        }
        return owned;
    }

    // Получить спрайт медали по ID
    public Sprite GetMedalSprite(int medalId)
    {
        if (medalId >= 0 && medalId < medalSprites.Length)
            return medalSprites[medalId];
        return null;
    }

    // Получить название медали (опционально)
    public string GetMedalName(int medalId)
    {
        if (medalId >= 0 && medalId < medalNames.Length)
            return medalNames[medalId];
        return $"Медаль {medalId}";
    }
}