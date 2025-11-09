using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;

public class CountDownManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI countdownText;

    [Header("Countdown Settings")]
    public int countdownTime = 3;

    [Header("Audio Settings")]
    public AudioSource countdownAudio; // assign in Inspector

    private Coroutine countdownCoroutine;
    private bool countdownActive = false;
    private bool countdownFinished = false;
    private bool isPaused = false; 

    private IGameStarter gameStarter;

    void Awake()
    {
        gameStarter = FindObjectsOfType<MonoBehaviour>()
              .OfType<IGameStarter>()
              .FirstOrDefault();

        if (countdownText != null)
            countdownText.raycastTarget = false;
    }

    void OnEnable()
    {
        SidePanelController.OnPauseStateChanged += HandlePause; 
    }

    void OnDisable()
    {
        SidePanelController.OnPauseStateChanged -= HandlePause; 
    }

    private void HandlePause(bool paused) 
    {
        isPaused = paused;
    }

    public void StartCountdown()
    {
        if (countdownActive || countdownFinished)
            return;

        // ✅ Set volume based on saved SFX setting
        if (countdownAudio != null)
        {
            countdownAudio.volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            countdownAudio.Play(); // play your single countdown clip
        }

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        countdownActive = true;
        int timeLeft = countdownTime;
        countdownText.text = "";
        countdownText.raycastTarget = true;

        while (timeLeft > 0)
        {
            countdownText.text = timeLeft.ToString();

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                if (!isPaused) elapsed += Time.deltaTime; 
                yield return null;
            }

            timeLeft--;
        }

        countdownText.text = "GO!";

        float goElapsed = 0f;
        while (goElapsed < 1f)
        {
            if (!isPaused) goElapsed += Time.deltaTime; 
            yield return null;
        }

        countdownText.text = "";
        countdownText.raycastTarget = false;

        if (gameStarter == null)
            gameStarter = FindObjectOfType<GoBagGameManager>();

        if (gameStarter != null)
            gameStarter.StartGame();

        countdownCoroutine = null;
        countdownActive = false;
        countdownFinished = true;
    }

    public void ResetCountdown()
    {
        countdownFinished = false;
        countdownText.text = "";
        countdownText.raycastTarget = false;
    }
}
