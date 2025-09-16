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

    private Coroutine countdownCoroutine;
    private bool countdownActive = false;
    private bool countdownFinished = false;

    // --- PAUSE FLAG ---
    private bool isPaused = false; 

    private IGameStarter gameStarter; // dynamic reference

    void Awake()
    {
        gameStarter = FindObjectsOfType<MonoBehaviour>()
              .OfType<IGameStarter>()
              .FirstOrDefault();

        // make sure raycast is off initially
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

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        countdownActive = true;
        int timeLeft = countdownTime;
        countdownText.text = "";

        // Enable raycast while countdown is active
        countdownText.raycastTarget = true;

        while (timeLeft > 0)
        {
            countdownText.text = timeLeft.ToString();

            // Wait for 1 second but pause if needed
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

        // Disable raycast after countdown ends
        countdownText.raycastTarget = false;

        // Start the game
        if (gameStarter == null)
        {
            gameStarter = FindObjectOfType<GoBagGameManager>();
        }

        if (gameStarter != null)
        {
            gameStarter.StartGame();
        }

        countdownCoroutine = null;
        countdownActive = false;
        countdownFinished = true;
    }

    public void ResetCountdown()
    {
        countdownFinished = false;
        countdownText.text = "";

        // also ensure raycast is off when reset
        countdownText.raycastTarget = false;
    }
}
