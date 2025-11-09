using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource narrationSource; // ✅ New for voice/narration

    [Header("Music Clips")]
    public AudioClip mainMusic;
    public AudioClip gameMusic;
    public AudioClip quizMusic;
    public AudioClip passMusic;
    public AudioClip failMusic;

    private string currentScene;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved volumes (default = 1f)
            musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            if (narrationSource != null)
                narrationSource.volume = PlayerPrefs.GetFloat("NarrationVolume", 1f); // ✅ load narration volume
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentScene = scene.name;
        StopNarration();
        PlayMusicForScene(currentScene);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip clipToPlay = null;

        if (sceneName == "MainMenu" || sceneName == "DisasterSelection")
            clipToPlay = mainMusic;
        else if (sceneName.EndsWith("Quiz"))
            clipToPlay = quizMusic;
        else if (sceneName == "Transition")
            return;
        else
            clipToPlay = gameMusic;

        if (clipToPlay != null && musicSource.clip != clipToPlay)
        {
            musicSource.clip = clipToPlay;
            musicSource.Play();
        }
    }

    // For Transition scene
    public void PlayTransitionMusic(bool passed)
    {
        musicSource.clip = passed ? passMusic : failMusic;
        musicSource.Play();
    }

    // === Sound Effects ===
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    // === Narration ===
    public void PlayNarration(AudioClip clip)
    {
        if (narrationSource == null || clip == null) return;

        narrationSource.Stop(); // stop previous narration
        narrationSource.clip = clip;
        narrationSource.Play();
    }

    public void StopNarration()
    {
        if (narrationSource != null)
            narrationSource.Stop();
    }

    // === Volume Controls ===
    public void SetMusicVolume(float value)
    {
        musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        sfxSource.volume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void SetNarrationVolume(float value)
    {
        if (narrationSource != null)
            narrationSource.volume = value;

        PlayerPrefs.SetFloat("NarrationVolume", value);
    }
}
