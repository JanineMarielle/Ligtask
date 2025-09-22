using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DuckCoverHold : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public DCHAnimator animator;
    public GameObject tablePrefab;
    public RectTransform tableSlot;
    public RectTransform gameCanvas;   
    public BackgroundManager backgroundManager; // NEW

    public float slideSpeed = 5f;
    public int pointsLost = 10;
    public int pointsPerAction = 10;

    [Header("Player Settings")]
    public RectTransform playerRect;
    public float playerLeftPadding = 20f;

    [Header("Round Settings")]
    [SerializeField] private int totalRounds = 3;  // editable
    private int currentRound = 0;

    private int score = 100;
    private bool isShaking = false;
    private bool canSwipeRight = false;

    private GameObject currentTable;
    private GameObject nextTableInstance;

    private bool hasDucked = false;
    private bool hasCovered = false;
    private bool isHolding = false;

    private float currentShakeDuration = 0f; // duration of current shake

    private float touchStartX = 0f;
    private bool touchStartedAfterCanSwipe = false;

    void Start()
    {
        if (playerRect != null)
        {
            playerRect.anchorMin = new Vector2(0f, 0f);
            playerRect.anchorMax = new Vector2(0f, 0f);
            playerRect.pivot = new Vector2(0f, 0f);
            playerRect.anchoredPosition = new Vector2(playerLeftPadding, 0f);
        }

        if (gameCanvas != null)
        {
            gameCanvas.anchorMin = new Vector2(0f, 0f);
            gameCanvas.anchorMax = new Vector2(1f, 1f);
            gameCanvas.pivot = new Vector2(0.5f, 0.5f);
            gameCanvas.anchoredPosition = Vector2.zero;
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

    currentRound = 0; // reset rounds

    touchStartedAfterCanSwipe = false;
    touchStartX = 0f;

    animator.ResetDuckCover();

    if (nextTableInstance != null) Destroy(nextTableInstance);
    SpawnNextTable();

    // ✅ start first round properly
    StartCoroutine(StartNextRound());
}

    private IEnumerator StartNextRound()
{
    currentRound++;

    if (currentRound > totalRounds)
    {
        Debug.Log("✅ All rounds finished!");
        yield break; // stop game
    }

    Debug.Log("▶️ Starting Round " + currentRound);

    // Start shaking for this round
    yield return StartCoroutine(ShakeScreenRoutine());

    // After shake, wait for swipe to trigger table+background slide
    canSwipeRight = true;
}

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStartX = Input.mousePosition.x;
            animator.startX = touchStartX;
            touchStartedAfterCanSwipe = canSwipeRight;
        }

        if (isShaking)
        {
            HandleShakingInput();
        }
        else if (canSwipeRight)
        {
            if (Input.GetMouseButtonUp(0) && touchStartedAfterCanSwipe)
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
        // duck / cover taps
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

        // hold after cover — do NOT immediately penalize on release here.
        // penalty for releasing is handled inside ShakeScreenRoutine (only in 2nd half).
        if (!animator.IsRunning && hasCovered)
        {
            bool holding = Input.GetMouseButton(0);
            animator.Hold(holding);

            if (!holding && isHolding)
            {
                // mark that the player released; do not deduct points here.
                isHolding = false;
            }
            else if (holding)
            {
                isHolding = true;
            }
        }

        // swipes during shake are still penalized
        if (Input.GetMouseButtonUp(0))
        {
            float swipeDelta = Input.mousePosition.x - touchStartX;
            if (swipeDelta > 50f) LosePoints("Swiped right during shaking!");
            if (swipeDelta < -50f) LosePoints("Swiped left during shaking!");
        }
    }

    IEnumerator ShakeScreenRoutine()
{
    yield return new WaitForSeconds(Random.Range(1f, 3f));

    // initialize shake state
    isShaking = true;
    canSwipeRight = false;
    hasDucked = false;
    hasCovered = false;
    isHolding = false;

    // ensure duration > initialCheckTime
    float initialCheckTime = 5f;
    currentShakeDuration = Mathf.Max(Random.Range(10f, 15f), initialCheckTime + 1f);

    Debug.Log("🌍 Round " + currentRound + " shaking for " + currentShakeDuration + " seconds!");

    if (gameCanvas != null)
        StartCoroutine(ShakeCanvas(gameCanvas, currentShakeDuration, 5f));

    // --- Phase 1: initial actions ---
    float timer = 0f;
    while (timer < initialCheckTime)
    {
        if (hasDucked && hasCovered && isHolding)
            break;

        timer += Time.deltaTime;
        yield return null;
    }

    if (!hasDucked) LosePoints("❌ Failed to duck in time!");
    if (!hasCovered) LosePoints("❌ Failed to cover in time!");
    if (!isHolding) LosePoints("❌ Failed to hold in time!");

    // --- Phase 2: sustain shaking ---
    timer = 0f;
    isHolding = Input.GetMouseButton(0); // sync state before phase 2

    while (timer < currentShakeDuration - initialCheckTime)
    {
        float absoluteTime = initialCheckTime + timer;

        if (!Input.GetMouseButton(0) && isHolding && absoluteTime >= currentShakeDuration / 2f)
        {
            LosePoints("❌ Released hold too early during second half of shake!");
            break;
        }

        timer += Time.deltaTime;
        yield return null;
    }

    // --- Exit shake ---
    isShaking = false;
    animator.Hold(false); 
    canSwipeRight = true;

    Debug.Log("✅ Shake ended. Swipe right to move to next table.");
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

        // Run both background and table slides simultaneously
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

        // Make sure background slide finishes before next round starts
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

    IEnumerator ShakeCanvas(RectTransform canvasRect, float duration, float magnitude = 5f)
    {
        float elapsed = 0f;
        Vector3 originalPos = canvasRect.localPosition;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-magnitude, magnitude);
            float offsetY = Random.Range(-magnitude, magnitude);
            canvasRect.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset to original
        canvasRect.localPosition = originalPos;
    }

    private void GainPoints(string reason)
    {
        score += pointsPerAction;
        Debug.Log(reason + " + " + pointsPerAction + " points. Total: " + score);
    }

    private void LosePoints(string reason)
    {
        score -= pointsLost;
        Debug.Log(reason + " - " + pointsLost + " points. Total: " + score);
    }
}
