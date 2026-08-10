using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    [SerializeField] private AudioClip bgMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.15f;

    [Header("SFX")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public bool ClickEnabled { get; private set; } = true;
    public bool GameSoundsEnabled { get; private set; } = true;
    public bool MusicEnabled { get; private set; } = true;
    public float SfxVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.clip = bgMusic;
        musicSource.volume = musicVolume * SfxVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = SfxVolume;

        ApplyMusicState();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);

        if (sfxSource != null)
            sfxSource.volume = SfxVolume;

        if (musicSource != null)
            musicSource.volume = musicVolume * SfxVolume;
    }

    public void SetClickEnabled(bool enabled)
    {
        ClickEnabled = enabled;
    }

    public void SetGameSoundsEnabled(bool enabled)
    {
        GameSoundsEnabled = enabled;
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        ApplyMusicState();
    }

    public void PlayClick()
    {
        if (!ClickEnabled)
            return;

        PlayOneShot(clickSound);
    }

    public void PlayVictory()
    {
        if (!GameSoundsEnabled)
            return;

        PlayOneShot(victorySound);
    }

    public void PlayDefeat()
    {
        if (!GameSoundsEnabled)
            return;

        PlayOneShot(defeatSound);
    }

    private void ApplyMusicState()
    {
        if (musicSource == null)
            return;

        if (MusicEnabled)
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    private void LoadSettings()
    {
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("SfxVolume", 1f));
        ClickEnabled = PlayerPrefs.GetInt("ClickSound", 1) == 1;
        GameSoundsEnabled = PlayerPrefs.GetInt("GameSounds", 1) == 1;
        MusicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
    }
}