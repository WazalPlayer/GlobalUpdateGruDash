using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StarManager : MonoBehaviour
{
    public static StarManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Image[] starIcons; // Иконки звёзд в UI
    [SerializeField] private Sprite starFull;
    [SerializeField] private Sprite starEmpty;

    private int totalStars = 0;
    private const string SAVE_KEY = "PlayerStars";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadStars();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddStars(int amount)
    {
        totalStars += amount;
        SaveStars();
        UpdateUI();
    }

    public int GetTotalStars()
    {
        return totalStars;
    }

    private void UpdateUI()
    {
        for (int i = 0; i < starIcons.Length; i++)
        {
            starIcons[i].sprite = i < totalStars ? starFull : starEmpty;
        }
    }

    private void SaveStars()
    {
        PlayerPrefs.SetInt(SAVE_KEY, totalStars);
        PlayerPrefs.Save();
    }

    private void LoadStars()
    {
        totalStars = PlayerPrefs.GetInt(SAVE_KEY, 0);
    }

    public void ResetStars()
    {
        totalStars = 0;
        SaveStars();
        UpdateUI();
    }
}