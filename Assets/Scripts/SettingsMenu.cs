using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel; 
    public Button settingsButton;    
    public Button closeButton;       

    public Slider musicSlider;
    public Slider narrationSlider;
    public Slider sfxSlider; // ✅ new slider for sound effects

    [Header("Blur Background")]
    public GameObject blurBackground;

    [Header("Settings Button Padding")]
    public Vector2 buttonPadding = new Vector2(20f, 20f);

    void Awake()
    {
        if (settingsButton != null)
        {
            RectTransform rect = settingsButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(buttonPadding.x, buttonPadding.y);
            }
        }
    }

    void Start()
    {
        // ✅ Load saved volume values
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        narrationSlider.value = PlayerPrefs.GetFloat("NarrationVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // ✅ Add slider listeners
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        narrationSlider.onValueChanged.AddListener(SetNarrationVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (blurBackground != null)
            blurBackground.SetActive(false);
    }

    private void OpenSettings()
    {
        settingsPanel.SetActive(true);
        if (blurBackground != null)
            blurBackground.SetActive(true);

        Time.timeScale = 0f;
    }

    private void CloseSettings()
    {
        settingsPanel.SetActive(false);
        if (blurBackground != null)
            blurBackground.SetActive(false);

        Time.timeScale = 1f;
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);

        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetNarrationVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetNarrationVolume(value);

        PlayerPrefs.SetFloat("NarrationVolume", value);
    }

    public void SetSFXVolume(float value) // ✅ new function
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);

        PlayerPrefs.SetFloat("SFXVolume", value);
    }
}
