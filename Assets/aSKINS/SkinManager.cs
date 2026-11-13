using UnityEngine;
using System.Collections.Generic;

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance { get; private set; }

    [System.Serializable]
    public class SkinCollection
    {
        public string name;
        public List<Sprite> sprites;
        public List<string> skinNames; // Названия скинов для отображения
    }

    [Header("Коллекции скинов")]
    public SkinCollection cubeSkins;
    public SkinCollection shipSkins;
    public SkinCollection waveSkins;
    public SkinCollection ufoSkins;
    public SkinCollection spiderSkins;
    public SkinCollection ballSkins;

    private Dictionary<string, int> selectedSkins;
    private string playerName = "";
    private string playerID = "";

    // Событие для обновления UI при изменении скинов
    public System.Action OnSkinsUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayer();
            LoadSelectedSkins();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePlayer()
    {
        // Загружаем или создаем ID игрока
        playerID = PlayerPrefs.GetString("PlayerID", "");
        if (string.IsNullOrEmpty(playerID))
        {
            playerID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("PlayerID", playerID);
            PlayerPrefs.Save();
        }

        // Загружаем ник игрока
        playerName = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Player_" + Random.Range(1000, 9999);
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
    }

    private void LoadSelectedSkins()
    {
        selectedSkins = new Dictionary<string, int>
        {
            { "Cube", PlayerPrefs.GetInt("SelectedCubeSkin", 0) },
            { "Ship", PlayerPrefs.GetInt("SelectedShipSkin", 0) },
            { "Wave", PlayerPrefs.GetInt("SelectedWaveSkin", 0) },
            { "UFO", PlayerPrefs.GetInt("SelectedUFOSkin", 0) },
            { "Spider", PlayerPrefs.GetInt("SelectedSpiderSkin", 0) },
            { "Ball", PlayerPrefs.GetInt("SelectedBallSkin", 0) }
        };
    }

    // Получение скинов
    public Sprite GetCubeSkin() => GetSkin("Cube", cubeSkins);
    public Sprite GetShipSkin() => GetSkin("Ship", shipSkins);
    public Sprite GetWaveSkin() => GetSkin("Wave", waveSkins);
    public Sprite GetUFOSkin() => GetSkin("UFO", ufoSkins);
    public Sprite GetSpiderSkin() => GetSkin("Spider", spiderSkins);
    public Sprite GetBallSkin() => GetSkin("Ball", ballSkins);

    private Sprite GetSkin(string skinType, SkinCollection collection)
    {
        if (selectedSkins.ContainsKey(skinType))
        {
            int index = selectedSkins[skinType];
            if (index < collection.sprites.Count && collection.sprites[index] != null)
                return collection.sprites[index];
        }
        return null;
    }

    // Получение имени скина
    public string GetCubeSkinName() => GetSkinName("Cube", cubeSkins);
    public string GetShipSkinName() => GetSkinName("Ship", shipSkins);
    public string GetWaveSkinName() => GetSkinName("Wave", waveSkins);
    public string GetUFOSkinName() => GetSkinName("UFO", ufoSkins);
    public string GetSpiderSkinName() => GetSkinName("Spider", spiderSkins);
    public string GetBallSkinName() => GetSkinName("Ball", ballSkins);

    private string GetSkinName(string skinType, SkinCollection collection)
    {
        if (selectedSkins.ContainsKey(skinType))
        {
            int index = selectedSkins[skinType];
            if (index < collection.skinNames.Count)
                return collection.skinNames[index];
        }
        return "Default";
    }

    // Выбор скина
    public void SelectSkin(string skinType, int skinIndex)
    {
        if (selectedSkins.ContainsKey(skinType))
        {
            selectedSkins[skinType] = skinIndex;
            PlayerPrefs.SetInt("Selected" + skinType + "Skin", skinIndex);
            PlayerPrefs.Save();

            // Уведомляем подписчиков об изменении
            OnSkinsUpdated?.Invoke();
        }
    }

    // Установка ника игрока
    public void SetPlayerName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            playerName = name;
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
    }

    // Получение данных игрока
    public string GetPlayerName() => playerName;
    public string GetPlayerID() => playerID;

    // Получение всех выбранных скинов для отправки в Supabase
    public Dictionary<string, int> GetAllSelectedSkins()
    {
        return new Dictionary<string, int>(selectedSkins);
    }

    // Загрузка скинов из данных аккаунта
    public void LoadSkinsFromAccount(AccountData accountData)
    {
        if (accountData != null)
        {
            // Обновляем скины
            SelectSkin("Cube", accountData.cube_skin);
            SelectSkin("Ship", accountData.ship_skin);
            SelectSkin("Wave", accountData.wave_skin);
            SelectSkin("UFO", accountData.ufo_skin);
            SelectSkin("Spider", accountData.spider_skin);
            
            // Добавляем скин шара, если он есть в данных
            if (accountData.ball_skin >= 0)
            {
                SelectSkin("Ball", accountData.ball_skin);
            }

            // Обновляем ник
            if (!string.IsNullOrEmpty(accountData.player_name))
            {
                SetPlayerName(accountData.player_name);
            }

            // Сохраняем в локальные настройки
            PlayerPrefs.Save();

            // Уведомляем UI
            OnSkinsUpdated?.Invoke();
        }
    }

    // Сброс к стандартным настройкам
    public void ResetToDefault()
    {
        selectedSkins["Cube"] = 0;
        selectedSkins["Ship"] = 0;
        selectedSkins["Wave"] = 0;
        selectedSkins["UFO"] = 0;
        selectedSkins["Spider"] = 0;
        selectedSkins["Ball"] = 0;

        PlayerPrefs.SetInt("SelectedCubeSkin", 0);
        PlayerPrefs.SetInt("SelectedShipSkin", 0);
        PlayerPrefs.SetInt("SelectedWaveSkin", 0);
        PlayerPrefs.SetInt("SelectedUFOSkin", 0);
        PlayerPrefs.SetInt("SelectedSpiderSkin", 0);
        PlayerPrefs.SetInt("SelectedBallSkin", 0);
        PlayerPrefs.Save();

        OnSkinsUpdated?.Invoke();
    }

    // Получение всех доступных имен скинов
    public List<string> GetAvailableSkinNames(string skinType)
    {
        switch (skinType)
        {
            case "Cube": return cubeSkins.skinNames;
            case "Ship": return shipSkins.skinNames;
            case "Wave": return waveSkins.skinNames;
            case "UFO": return ufoSkins.skinNames;
            case "Spider": return spiderSkins.skinNames;
            case "Ball": return ballSkins.skinNames;
            default: return new List<string>();
        }
    }
    
    // Получение спрайта скина по типу и индексу
    public Sprite GetSkinSpriteByIndex(string skinType, int index)
    {
        switch (skinType)
        {
            case "Cube":
                if (index >= 0 && index < cubeSkins.sprites.Count)
                    return cubeSkins.sprites[index];
                break;
            case "Ship":
                if (index >= 0 && index < shipSkins.sprites.Count)
                    return shipSkins.sprites[index];
                break;
            case "Wave":
                if (index >= 0 && index < waveSkins.sprites.Count)
                    return waveSkins.sprites[index];
                break;
            case "UFO":
                if (index >= 0 && index < ufoSkins.sprites.Count)
                    return ufoSkins.sprites[index];
                break;
            case "Spider":
                if (index >= 0 && index < spiderSkins.sprites.Count)
                    return spiderSkins.sprites[index];
                break;
            case "Ball":
                if (index >= 0 && index < ballSkins.sprites.Count)
                    return ballSkins.sprites[index];
                break;
        }
        return null;
    }

    // Или отдельные методы для каждого типа:
    public Sprite GetCubeSkinByIndex(int index)
    {
        if (index >= 0 && index < cubeSkins.sprites.Count)
            return cubeSkins.sprites[index];
        return null;
    }

    public Sprite GetShipSkinByIndex(int index)
    {
        if (index >= 0 && index < shipSkins.sprites.Count)
            return shipSkins.sprites[index];
        return null;
    }

    public Sprite GetWaveSkinByIndex(int index)
    {
        if (index >= 0 && index < waveSkins.sprites.Count)
            return waveSkins.sprites[index];
        return null;
    }

    public Sprite GetUFOSkinByIndex(int index)
    {
        if (index >= 0 && index < ufoSkins.sprites.Count)
            return ufoSkins.sprites[index];
        return null;
    }

    public Sprite GetSpiderSkinByIndex(int index)
    {
        if (index >= 0 && index < spiderSkins.sprites.Count)
            return spiderSkins.sprites[index];
        return null;
    }

    public Sprite GetBallSkinByIndex(int index)
    {
        if (index >= 0 && index < ballSkins.sprites.Count)
            return ballSkins.sprites[index];
        return null;
    }

    // Проверка, разблокирован ли скин (можно добавить систему прогресса)
    public bool IsSkinUnlocked(string skinType, int skinIndex)
    {
        // Базовая реализация - все скины разблокированы
        // Можно добавить проверку на достижения или покупки
        return true;
    }
}