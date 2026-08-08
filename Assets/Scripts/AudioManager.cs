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

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        musicSource.clip = bgMusic;
        musicSource.Play();

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    public void PlayClick()
    {
        PlayOneShot(clickSound);
    }

    public void PlayVictory()
    {
        PlayOneShot(victorySound);
    }

    public void PlayDefeat()
    {
        PlayOneShot(defeatSound);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }
}