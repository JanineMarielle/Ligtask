using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel; // The overlay panel
    public Button settingsButton;    // The gear/settings button
    public Button closeButton;       // Optional close button

    public Slider musicSlider;
    //public Slider sfxSlider;

    [Header("Blur Background")]
    public GameObject blurBackground;

    [Header("Settings Button Padding")]
    public Vector2 buttonPadding = new Vector2(20f, 20f);

    void Awake()
    {
        // Apply padding to settings button if assigned
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
        // Load saved volume values
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        //sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Add slider listeners for live updates
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        //sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Add button listener to open settings panel (overlay)
        if (settingsButton != null)
            settingsButton.onClick.AddListener(() =>
            {
                OpenSettings();
            });

        // Add button listener to close settings panel
        if (closeButton != null)
            closeButton.onClick.AddListener(() =>
            {
                CloseSettings();
            });

        // Ensure settings panel is hidden at start
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Ensure blur background is hidden at start
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
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }
}
