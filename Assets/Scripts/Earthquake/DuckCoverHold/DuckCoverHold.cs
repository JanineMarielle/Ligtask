using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class DuckCoverHold : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public DCHAnimator animator;
    public GameObject tablePrefab;
    public RectTransform tableSlot; 
    public BackgroundManager backgroundManager;

    public float slideSpeed = 5f;
    public int pointsLost = 10;
    public int pointsPerAction = 20;

    [Header("Player Settings")]
    public RectTransform playerRect;
    public float playerLeftPadding = 20f;

    [Header("Round Settings")]
    [SerializeField] private int totalRounds = 3;
    private int currentRound = 0;

    [Header("Managers")]
    public FeedbackManager feedbackManager;

    [Header("UI References")]
    public GameObject menuButton;

    private int score = 0;
    private bool isShaking = false;
    private bool canSwipeRight = false;

    private GameObject currentTable;
    private GameObject nextTableInstance;

    private bool hasDucked = false;
    private bool hasCovered = false;
    private bool isHolding = false;

    private float currentShakeDuration = 0f;

    private float touchStartX = 0f;
    private bool touchStartedAfterCanSwipe = false;

    // --- PAUSE SUPPORT ---
    private bool isPaused = false;
    private Coroutine activeShakeRoutine;

    void OnEnable()
    {
        SidePanelController.OnPauseStateChanged += HandlePauseStateChanged;
    }

    void OnDisable()
    {
        SidePanelController.OnPauseStateChanged -= HandlePauseStateChanged;
    }

    private void HandlePauseStateChanged(bool pause)
    {
        isPaused = pause;

        if (animator != null)
            animator.enabled = !pause; // stop/resume animations
    }

    void Start()
    {
        if (playerRect != null)
        {
            playerRect.anchorMin = new Vector2(0f, 0f);
            playerRect.anchorMax = new Vector2(0f, 0f);
            playerRect.pivot = new Vector2(0f, 0f);
            playerRect.anchoredPosition = new Vector2(playerLeftPadding, 0f);
        }

        if (currentTable == null)
        {
            currentTable = Instantiate(tablePrefab, tableSlot);
            RectTransform tableRect = currentTable.GetComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0f, 0f);
            tableRect.anchorMax = new Vector2(0f, 0f);
            tableRect.pivot = new Vector2(0f, 0f);
            tableRect.anchoredPosition = new Vector2(0f, 0f);
        }
    }

    public void StartGame()
    {
        score = 100;
        isShaking = false;
        canSwipeRight = false;
        hasDucked = false;
        hasCovered = false;
        isHolding = false;

        currentRound = 0;

        touchStartedAfterCanSwipe = false;
        touchStartX = 0f;

        animator.ResetDuckCover();

        if (nextTableInstance != null) Destroy(nextTableInstance);
        SpawnNextTable();

        StartCoroutine(StartNextRound());
    }

    private IEnumerator StartNextRound()
    {
        currentRound++;

        if (currentRound > totalRounds)
        {
            EndGame();
            yield break;
        }

        yield return StartCoroutine(ShakeScreenRoutine());
        canSwipeRight = true;
    }

    private bool ignoreNextRelease = false; // 👈 add this

    void Update()
    {
        if (isPaused) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (menuButton != null && RectTransformUtility.RectangleContainsScreenPoint(
                menuButton.GetComponent<RectTransform>(),
                Input.mousePosition,
                null))
            {
                ignoreNextRelease = true; // 👈 ignore this whole touch
                return;
            }

            touchStartX = Input.mousePosition.x;
            animator.startX = touchStartX;
            touchStartedAfterCanSwipe = canSwipeRight;
            ignoreNextRelease = false;
        }

        if (isShaking)
        {
            HandleShakingInput();
        }
        else if (canSwipeRight)
        {
            if (Input.GetMouseButtonUp(0) && touchStartedAfterCanSwipe && !ignoreNextRelease)
            {
                float swipeDelta = Input.mousePosition.x - touchStartX;
                if (swipeDelta > 50f)
                {
                    StartCoroutine(SwipeRightToNextTable());
                }
                touchStartedAfterCanSwipe = false;
            }
        }
    }

    void HandleShakingInput()
    {
        if (!animator.IsRunning && Input.GetMouseButtonDown(0))
        {
            if (!hasDucked)
            {
                animator.Duck();
                hasDucked = true;
                GainPoints("Ducked on time!");
            }
            else if (!hasCovered)
            {
                animator.Cover();
                hasCovered = true;
                GainPoints("Covered on time!");
            }
        }

        // ✅ Only allow Hold if already ducked & covered
        if (!animator.IsRunning && hasDucked && hasCovered)
        {
            bool holding = Input.GetMouseButton(0);

            if (holding && !isHolding)
            {
                animator.Hold(true);
                isHolding = true;
            }
            else if (!holding && isHolding)
            {
                animator.Hold(false);
                isHolding = false;
            }
        }

        if (Input.GetMouseButtonUp(0) && !ignoreNextRelease)
        {
            float swipeDelta = Input.mousePosition.x - touchStartX;
            if (swipeDelta > 50f) LosePoints("Swiped right during shaking!");
            if (swipeDelta < -50f) LosePoints("Swiped left during shaking!");
        }
    }

    IEnumerator ShakeScreenRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        isShaking = true;
        canSwipeRight = false;
        hasDucked = false;
        hasCovered = false;
        isHolding = false;

        float initialCheckTime = 5f;
        currentShakeDuration = Mathf.Max(Random.Range(10f, 15f), initialCheckTime + 1f);

        // Shake all UI elements in sync
        RectTransform[] shakeTargets = {
            playerRect,
            currentTable?.GetComponent<RectTransform>(),
            backgroundManager.backgroundA,
            backgroundManager.backgroundB
        };

        activeShakeRoutine = StartCoroutine(ShakeMultipleUI(shakeTargets, currentShakeDuration, 5f));

        // wait first phase...
        float timer = 0f;
        while (timer < initialCheckTime)
        {
            if (hasDucked && hasCovered && isHolding) break;
            timer += Time.deltaTime;
            yield return null;
        }

        if (!hasDucked) LosePoints("❌ Failed to duck in time!");
        if (!hasCovered) LosePoints("❌ Failed to cover in time!");
        if (!isHolding) LosePoints("❌ Failed to hold in time!");

        // sustain phase...
        yield return new WaitForSeconds(currentShakeDuration - initialCheckTime);

        yield return activeShakeRoutine;

        isShaking = false;
        animator.Hold(false);
        isHolding = false;
        canSwipeRight = true;
    }


    IEnumerator ShakeMultipleUI(RectTransform[] targets, float duration, float magnitude = 5f)
    {
        float elapsed = 0f;
        Dictionary<RectTransform, Vector3> originals = new Dictionary<RectTransform, Vector3>();

        foreach (var t in targets)
        {
            if (t != null) originals[t] = t.localPosition;
        }

        while (elapsed < duration)
        {
            if (!isPaused) // ✅ only update when not paused
            {
                foreach (var kv in originals)
                {
                    float offsetX = Random.Range(-magnitude, magnitude);
                    float offsetY = Random.Range(-magnitude, magnitude);
                    kv.Key.localPosition = kv.Value + new Vector3(offsetX, offsetY, 0);
                }

                elapsed += Time.deltaTime; // ✅ normal deltaTime now
            }

            yield return null;
        }

        foreach (var kv in originals)
        {
            kv.Key.localPosition = kv.Value;
        }
    }

    // Helper to check if player is currently holding (works PC + Mobile)
    private bool IsPlayerHolding()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButton(0);
    #else
        return Input.touchCount > 0 && 
            (Input.GetTouch(0).phase == TouchPhase.Began || 
                Input.GetTouch(0).phase == TouchPhase.Moved || 
                Input.GetTouch(0).phase == TouchPhase.Stationary);
    #endif
    }


    IEnumerator SwipeRightToNextTable()
    {
        canSwipeRight = false;

        SpawnNextTable();

        RectTransform nextRect = nextTableInstance.GetComponent<RectTransform>();
        RectTransform currentRect = currentTable.GetComponent<RectTransform>();
        nextRect.anchoredPosition = new Vector2(tableSlot.rect.width + 100f, 0f);

        Vector2 tableStart = nextRect.anchoredPosition;
        Vector2 tableEnd = Vector2.zero;
        Vector2 tableCurrentEnd = new Vector2(-currentRect.rect.width - 100f, 0f);

        float t = 0f;
        float duration = 1.2f;
        animator.Run();

        Coroutine bgSlide = StartCoroutine(backgroundManager.SlideBackground());

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            nextRect.anchoredPosition = Vector2.Lerp(tableStart, tableEnd, t);
            currentRect.anchoredPosition = Vector2.Lerp(currentRect.anchoredPosition, tableCurrentEnd, t);
            yield return null;
        }

        animator.StopRun();

        Destroy(currentTable);
        currentTable = nextTableInstance;

        yield return bgSlide;

        StartCoroutine(StartNextRound());
    }

    void SpawnNextTable()
    {
        nextTableInstance = Instantiate(tablePrefab, tableSlot);
        RectTransform rt = nextTableInstance.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);

        if (currentTable == null)
        {
            rt.anchoredPosition = new Vector2(0f, 0f);
            currentTable = nextTableInstance;
        }
        else
        {
            rt.anchoredPosition = new Vector2(tableSlot.rect.width + 100f, 0f);
        }
    }

    // --- ENDGAME LOGIC ---
    private void EndGame()
    {
        int maxScore = totalRounds * (pointsPerAction * 3); // duck, cover, hold per round
        int passingScore = Mathf.RoundToInt(maxScore * 0.6f);

        GameResults.Score = score;
        GameResults.Passed = score >= passingScore;
        GameResults.DisasterName = "Earthquake";
        GameResults.MiniGameIndex = 3;
        GameResults.Difficulty = "Easy";

        DBManager.SaveProgress(GameResults.DisasterName, GameResults.Difficulty, GameResults.MiniGameIndex, GameResults.Passed);
        SceneTracker.SetCurrentMiniGame(GameResults.DisasterName, GameResults.Difficulty, SceneManager.GetActiveScene().name);

        Debug.Log($"🏁 Game Ended! Score: {score}, Passed: {GameResults.Passed}");
        SceneManager.LoadScene("TransitionScene");
    }

    // --- SCORING ---
    private void GainPoints(string reason)
    {
        score += pointsPerAction;
        Debug.Log(reason + " + " + pointsPerAction + " points. Total: " + score);

        if (feedbackManager != null)
            feedbackManager.ShowPositive();
    }

    private void LosePoints(string reason)
    {
        score -= pointsLost;
        Debug.Log(reason + " - " + pointsLost + " points. Total: " + score);

        if (feedbackManager != null)
            feedbackManager.ShowNegative();
    }
}
