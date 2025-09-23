using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

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
        PlayMusicForScene(currentScene);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip clipToPlay = null;

        // Main menu / non-game scenes
        if (sceneName == "MainMenu" || sceneName == "Settings" || sceneName == "Map")
            clipToPlay = mainMusic;

        // Quizzes
        else if (sceneName.EndsWith("Quiz"))
            clipToPlay = quizMusic;

        // Transition scenes (you call PlayTransitionMusic manually from your script)
        else if (sceneName == "Transition")
            return;

        // Game levels
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

    // Play one-shot SFX
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
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
}
