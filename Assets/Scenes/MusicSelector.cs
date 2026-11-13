using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MusicSelector : MonoBehaviour
{
    [Header("Список музыки")]
    public List<AudioClip> musicTracks = new List<AudioClip>();
    
    [Header("Настройки аудио")]
    public AudioSource audioSource;
    public bool playImmediately = true;
    public bool loopMusic = true;
    public float volume = 1.0f;
    
    [Header("Настройки кнопок")]
    public Button previousButton;
    public Button nextButton;
    public Button playButton;
    public Button stopButton;
    public Text trackNameText;
    
    [Header("Визуальные настройки")]
    public bool showTrackNumbers = true;
    public string defaultTrackName = "No Track";
    
    [Header("Синхронизация музыки")]
    public float musicSyncInterval = 1.0f; // Интервал синхронизации в секундах
    public Text syncIndicatorText;
    public Image syncIndicatorImage;
    private float syncTimer = 0f;
    private bool isSyncing = false;
    
    [Header("Сохранение настроек")]
    public bool saveSettings = true;
    private const string TRACK_INDEX_KEY = "SavedMusicTrackIndex";
    private const string VOLUME_KEY = "SavedMusicVolume";
    private const string LOOP_KEY = "SavedMusicLoop";
    private const string SYNC_KEY = "SavedMusicSync";
    
    [HideInInspector] public int currentTrackIndex = 0;
    private bool isPlaying = false;
    
    void Start()
    {
        InitializeAudioSource();
        
        // Загружаем сохраненные настройки
        if (saveSettings)
        {
            LoadSavedSettings();
        }
        
        SetupButtons();
        UpdateUI();
        
        // Воспроизводим сразу если нужно
        if (playImmediately && musicTracks.Count > 0)
        {
            PlayMusic();
        }
    }
    
    void Update()
    {
        // Синхронизация музыки
        if (isSyncing && isPlaying && audioSource.isPlaying)
        {
            syncTimer += Time.deltaTime;
            if (syncTimer >= musicSyncInterval)
            {
                syncTimer = 0f;
                TriggerSyncEvent();
            }
        }
        
        // Обновление UI синхронизации
        UpdateSyncUI();
    }
    
    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        audioSource.loop = loopMusic;
        audioSource.volume = volume;
    }
    
    private void LoadSavedSettings()
    {
        if (PlayerPrefs.HasKey(TRACK_INDEX_KEY))
        {
            currentTrackIndex = PlayerPrefs.GetInt(TRACK_INDEX_KEY);
        }
        
        if (PlayerPrefs.HasKey(VOLUME_KEY))
        {
            volume = PlayerPrefs.GetFloat(VOLUME_KEY);
            if (audioSource != null)
                audioSource.volume = volume;
        }
        
        if (PlayerPrefs.HasKey(LOOP_KEY))
        {
            loopMusic = PlayerPrefs.GetInt(LOOP_KEY) == 1;
            if (audioSource != null)
                audioSource.loop = loopMusic;
        }
        
        if (PlayerPrefs.HasKey(SYNC_KEY))
        {
            isSyncing = PlayerPrefs.GetInt(SYNC_KEY) == 1;
        }
    }
    
    private void SaveSettings()
    {
        if (!saveSettings) return;
        
        PlayerPrefs.SetInt(TRACK_INDEX_KEY, currentTrackIndex);
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.SetInt(LOOP_KEY, loopMusic ? 1 : 0);
        PlayerPrefs.SetInt(SYNC_KEY, isSyncing ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    private void SetupButtons()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(PreviousTrack);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextTrack);
        }
        
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(TogglePlay);
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.RemoveAllListeners();
            stopButton.onClick.AddListener(StopMusic);
        }
    }
    
    public void SelectTrack(int trackIndex)
    {
        if (musicTracks.Count == 0)
        {
            Debug.LogWarning("⚠️ Список музыки пуст!", this);
            return;
        }
        
        if (trackIndex < 0 || trackIndex >= musicTracks.Count)
        {
            Debug.LogError($"❌ Неверный индекс трека: {trackIndex}. Доступно треков: {musicTracks.Count}");
            return;
        }
        
        currentTrackIndex = trackIndex;
        AudioClip selectedTrack = musicTracks[currentTrackIndex];
        
        if (selectedTrack != null)
        {
            audioSource.clip = selectedTrack;
            
            if (playImmediately && !audioSource.isPlaying)
            {
                PlayMusic();
            }
            
            // Сохраняем настройки
            if (saveSettings)
            {
                SaveSettings();
            }
            
            Debug.Log($"🎵 Выбран трек: {GetTrackName(currentTrackIndex)}");
        }
        else
        {
            Debug.LogError($"❌ Трек с индексом {trackIndex} равен null!");
        }
        
        UpdateUI();
    }
    
    public void SelectTrackByButton(int trackIndex)
    {
        SelectTrack(trackIndex);
    }
    
    public void NextTrack()
    {
        if (musicTracks.Count == 0) return;
        
        currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Count;
        SelectTrack(currentTrackIndex);
    }
    
    public void PreviousTrack()
    {
        if (musicTracks.Count == 0) return;
        
        currentTrackIndex--;
        if (currentTrackIndex < 0)
        {
            currentTrackIndex = musicTracks.Count - 1;
        }
        SelectTrack(currentTrackIndex);
    }
    
    public void PlayMusic()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            isPlaying = true;
            Debug.Log($"▶️ Воспроизведение: {GetTrackName(currentTrackIndex)}");
        }
        else if (musicTracks.Count > 0)
        {
            SelectTrack(currentTrackIndex);
        }
        
        UpdateUI();
    }
    
    public void StopMusic()
    {
        audioSource.Stop();
        isPlaying = false;
        Debug.Log("⏹️ Музыка остановлена");
        UpdateUI();
    }
    
    public void TogglePlay()
    {
        if (isPlaying)
        {
            StopMusic();
        }
        else
        {
            PlayMusic();
        }
    }
    
    // СИНХРОНИЗАЦИЯ МУЗЫКИ
    public void ToggleSync()
    {
        isSyncing = !isSyncing;
        syncTimer = 0f;
        
        if (isSyncing && isPlaying && audioSource.isPlaying)
        {
            Debug.Log("🎵 Синхронизация музыки: ВКЛ");
            TriggerSyncEvent();
        }
        else if (!isSyncing)
        {
            Debug.Log("🎵 Синхронизация музыки: ВЫКЛ");
        }
        
        if (saveSettings)
        {
            PlayerPrefs.SetInt(SYNC_KEY, isSyncing ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        UpdateSyncUI();
    }
    
    private void TriggerSyncEvent()
    {
        Debug.Log($"🎵 Синхрон музыки (интервал: {musicSyncInterval}сек)");
        
        // Здесь можно добавить события синхронизации
        // Например, пульсацию объектов, изменение цветов и т.д.
        SyncPulseEffect();
    }
    
    private void SyncPulseEffect()
    {
        // Создаем визуальный эффект пульсации
        if (syncIndicatorImage != null)
        {
            StartCoroutine(PulseEffect(syncIndicatorImage));
        }
        
        // Отправляем событие синхронизации
        SendMessage("OnMusicSync", SendMessageOptions.DontRequireReceiver);
    }
    
    private System.Collections.IEnumerator PulseEffect(Image image)
    {
        if (image == null) yield break;
        
        Color originalColor = image.color;
        image.color = Color.yellow;
        
        yield return new WaitForSeconds(0.2f);
        
        if (image != null)
            image.color = originalColor;
    }
    
    private void UpdateSyncUI()
    {
        if (syncIndicatorText != null)
        {
            syncIndicatorText.text = isSyncing ? "СИНХРОН: ВКЛ" : "СИНХРОН: ВЫКЛ";
            syncIndicatorText.color = isSyncing ? Color.green : Color.white;
        }
        
        if (syncIndicatorImage != null)
        {
            syncIndicatorImage.enabled = isSyncing;
        }
    }
    
    public void SetSyncInterval(float interval)
    {
        musicSyncInterval = Mathf.Clamp(interval, 0.1f, 10f);
        Debug.Log($"🎵 Интервал синхронизации изменен: {musicSyncInterval:F1} сек");
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
            audioSource.volume = volume;
        
        if (saveSettings)
        {
            PlayerPrefs.SetFloat(VOLUME_KEY, volume);
            PlayerPrefs.Save();
        }
    }
    
    public void SetLoop(bool loop)
    {
        loopMusic = loop;
        if (audioSource != null)
            audioSource.loop = loopMusic;
        
        if (saveSettings)
        {
            PlayerPrefs.SetInt(LOOP_KEY, loopMusic ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    
    private void UpdateUI()
    {
        if (trackNameText != null)
        {
            trackNameText.text = GetTrackName(currentTrackIndex);
        }
        
        UpdateButtonsState();
        UpdateSyncUI();
    }
    
    private void UpdateButtonsState()
    {
        if (playButton != null)
        {
            playButton.interactable = musicTracks.Count > 0;
            Text playButtonText = playButton.GetComponentInChildren<Text>();
            if (playButtonText != null)
            {
                playButtonText.text = isPlaying ? "II" : "▶";
            }
        }
        
        if (stopButton != null)
        {
            stopButton.interactable = isPlaying;
        }
        
        if (previousButton != null)
        {
            previousButton.interactable = musicTracks.Count > 1;
        }
        
        if (nextButton != null)
        {
            nextButton.interactable = musicTracks.Count > 1;
        }
    }
    
    public string GetTrackName(int index)
    {
        if (musicTracks.Count == 0) return defaultTrackName;
        
        if (index < 0 || index >= musicTracks.Count) return "Invalid Track";
        
        AudioClip track = musicTracks[index];
        if (track == null) return "Null Track";
        
        string trackNumber = showTrackNumbers ? $"{index + 1}. " : "";
        return $"{trackNumber}{track.name}";
    }
    
    void OnDestroy()
    {
        if (saveSettings)
        {
            SaveSettings();
        }
    }
}