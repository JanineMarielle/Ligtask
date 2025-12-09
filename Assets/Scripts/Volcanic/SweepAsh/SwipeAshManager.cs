using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SwipeAshManager : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public RectTransform canvas;
    public RectTransform customCanvas;
    public GameObject walisPrefab;
    [SerializeField] private TimerLogic timer;
    public UnityEngine.UI.Image backgroundImage;
    public Sprite nextRoundBackground;

    [Header("Settings")]
    [SerializeField] private float roundTimerDuration = 10f;
    public float swipeRadius = 100f;
    public float ashPushDistance = 100f;
    public float walisAnimDuration = 0.3f;
    public float walisReturnDuration = 0.25f;
    public float swingAngle = 45f;

    [Header("Spawn Settings")]
    public GameObject ashPrefab;
    public int ashCountThisRound = 100;

    private Vector2 startPos;
    private bool isSwiping = false;
    private bool gameStarted = false;
    private RectTransform walis;
    private RectTransform activeCanvas;

    [Header("Swipe Settings")]
    public float maxSwipeLength = 400f;

    [Header("Ash Spawner Reference")]
    public AshSpawner ashSpawner;

    [Header("Dustpan Reference")]
    public RectTransform dustpanRect;
    public float dustpanPaddingLeft = 10f;
    public float dustpanPaddingRight = 10f;
    public float dustpanPaddingTop = 10f;
    public float dustpanPaddingBottom = 10f;

    private int totalAsh = 0;
    private int clearedAsh = 0;

    [Header("Game Scoring")]
    public int maxScore = 100;
    public int passingScore = 60;
    private int score = 0;
    private int currentRound = 0;           
    private int totalScore = 0;           
    private int maxRounds = 2;              

    private int cumulativeTotalAsh = 0;   
    private int cumulativeClearedAsh = 0;

    private Coroutine walisCoroutine; 

    [Header("UI")]
    public TextMeshProUGUI lastRoundText;
    public float lastRoundFadeDuration = 2f;

    private void Start()
    {
        if (timer == null)
            timer = FindObjectOfType<TimerLogic>();

        activeCanvas = customCanvas != null ? customCanvas : canvas;

        if (walisPrefab != null && canvas != null)
        {
            GameObject obj = Instantiate(walisPrefab, canvas);
            walis = obj.GetComponent<RectTransform>();
            walis.gameObject.SetActive(false);
        }

        if (lastRoundText != null)
            lastRoundText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameStarted) return;

        // Touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) { startPos = touch.position; isSwiping = true; }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                ProcessSwipe(startPos, touch.position);
                isSwiping = false;
            }
        }

        // Mouse input fallback
        if (Input.GetMouseButtonDown(0)) { startPos = Input.mousePosition; isSwiping = true; }
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            ProcessSwipe(startPos, Input.mousePosition);
            isSwiping = false;
        }
    }

    public void StartGame()
    {
        gameStarted = true;
        AshController[] ashes = FindObjectsOfType<AshController>();
        totalAsh = ashes.Length;
        clearedAsh = 0;

        if (timer != null)
        {
            timer.StartTimer(roundTimerDuration);   // start timer once
            timer.OnTimerFinished += HandleTimerEnd;
        }

        SpawnAshForRound();  // spawn initial ashes
    }

    public void StopGame()
    {
        gameStarted = false;
        isSwiping = false;

        if (timer != null)
            timer.OnTimerFinished -= HandleTimerEnd;
    }

    private void HandleTimerEnd()
    {
        EndRound(timerExpired: true);
    }


    private void OnDestroy()
    {
        if (timer != null)
            timer.OnTimerFinished -= HandleTimerEnd;
    }

    private void ProcessSwipe(Vector2 start, Vector2 end)
    {
        Vector2 swipeVector = end - start;
        if (swipeVector.magnitude < 0.1f) return;
        if (swipeVector.magnitude > maxSwipeLength)
        {
            swipeVector = swipeVector.normalized * maxSwipeLength;
            end = start + swipeVector;
        }
        TriggerSwipe(start, end, swipeVector.normalized);
    }

    void TriggerSwipe(Vector2 start, Vector2 end, Vector2 dir)
    {
        if (walis == null) return;

        if (walisCoroutine != null)
        {
            StopCoroutine(walisCoroutine);
        }

        walisCoroutine = StartCoroutine(PlayWalisAnimation(walis, start, end, dir));


        AshController[] ashes = FindObjectsOfType<AshController>();
        foreach (var ash in ashes)
        {
            if (ash.IsSettled) continue;

            RectTransform ashRT = ash.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            ashRT.GetWorldCorners(corners);

            bool shouldMove = false;
            float lineLength = (end - start).magnitude;

            foreach (var corner in corners)
            {
                Vector2 cornerScreenPos = RectTransformUtility.WorldToScreenPoint(null, corner);
                float dist = DistancePointToLineSegment(cornerScreenPos, start, end);
                float projLength = Vector2.Dot((cornerScreenPos - start), (end - start).normalized);
                if (dist < swipeRadius && projLength >= 0 && projLength <= lineLength)
                {
                    shouldMove = true;
                    break;
                }
            }

            if (shouldMove)
            {
                if (dustpanRect != null && ash.IsOverDustpan(dustpanRect))
                {
                    ash.Settle(dustpanRect);
                    continue;
                }

                ash.Push(dir, ashPushDistance);
            }
        }
    }

    float DistancePointToLineSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = point - a;
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
        return Vector2.Distance(point, a + t * ab);
    }

    IEnumerator PlayWalisAnimation(RectTransform walis, Vector2 start, Vector2 end, Vector2 dir)
    {
        walis.gameObject.SetActive(true);
        walis.position = start;

        walis.localScale = (end.x > Screen.width / 2f) ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion baseRot = Quaternion.Euler(0, 0, angle - 90f);
        walis.localRotation = baseRot;

        float swingDir = (dir.x > 0) ? -swingAngle : swingAngle;
        Quaternion swingRot = baseRot * Quaternion.Euler(0, 0, swingDir);

        float elapsed = 0f;
        while (elapsed < walisAnimDuration)
        {
            elapsed += Time.deltaTime;
            walis.localRotation = Quaternion.Lerp(baseRot, swingRot, elapsed / walisAnimDuration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < walisReturnDuration)
        {
            elapsed += Time.deltaTime;
            walis.localRotation = Quaternion.Lerp(swingRot, baseRot, elapsed / walisReturnDuration);
            yield return null;
        }
    }

    public void NotifyAshCleared()
    {   
        clearedAsh++;
        cumulativeClearedAsh++; // track total cleared across rounds
        Debug.Log($"Ash cleared this round: {clearedAsh}/{totalAsh}, total cleared: {cumulativeClearedAsh}/{cumulativeTotalAsh}");

        if (clearedAsh >= totalAsh && gameStarted)
        {
            Debug.Log("All ashes cleared! Ending round...");
            EndRound();
        }
    }

    private void EndRound(bool timerExpired = false)
    {
        gameStarted = false;

        CalculateScore();
        totalScore += score;

        AshController[] ashes = FindObjectsOfType<AshController>();
        foreach (var ash in ashes)
            Destroy(ash.gameObject);

        if (timerExpired || currentRound + 1 >= maxRounds)
        {
            EndGame();
            return;
        }

        // Otherwise continue to next round
        currentRound++;
        clearedAsh = 0;

        if (backgroundImage != null && nextRoundBackground != null)
            backgroundImage.sprite = nextRoundBackground;

        SpawnAshForRound();
        gameStarted = true;

        if (lastRoundText != null)
            StartCoroutine(ShowLastRoundText());
    }

    private IEnumerator ShowLastRoundText()
    {
        lastRoundText.gameObject.SetActive(true);
        lastRoundText.color = new Color(lastRoundText.color.r, lastRoundText.color.g, lastRoundText.color.b, 1f);

        float elapsed = 0f;
        while (elapsed < lastRoundFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / lastRoundFadeDuration);
            lastRoundText.color = new Color(lastRoundText.color.r, lastRoundText.color.g, lastRoundText.color.b, alpha);
            yield return null;
        }

        lastRoundText.gameObject.SetActive(false);
    }

    private void SpawnAshForRound()
    {
        if (ashSpawner != null)
        {
            ashSpawner.SpawnAshes(ashCountThisRound);
            totalAsh = ashCountThisRound;
            clearedAsh = 0;

            cumulativeTotalAsh += ashCountThisRound; // track total across all rounds
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 canvasSize = activeCanvas.rect.size;

        Vector3[] dpCorners = new Vector3[4];
        dustpanRect.GetWorldCorners(dpCorners);
        Vector2 dpMin = activeCanvas.InverseTransformPoint(dpCorners[0]);
        Vector2 dpMax = activeCanvas.InverseTransformPoint(dpCorners[2]);

        dpMin += new Vector2(dustpanPaddingLeft, dustpanPaddingBottom);
        dpMax -= new Vector2(dustpanPaddingRight, dustpanPaddingTop);

        Vector2 pos;
        int attempts = 0;
        do
        {
            pos = new Vector2(
                Random.Range(-canvasSize.x / 2f + 50f, canvasSize.x / 2f - 50f),
                Random.Range(-canvasSize.y / 2f + 50f, canvasSize.y / 2f - 50f)
            );
            attempts++;
        }
        while (attempts < 100 && pos.x >= dpMin.x && pos.x <= dpMax.x && pos.y >= dpMin.y && pos.y <= dpMax.y);

        return pos;
    }

    private void CalculateScore()
    {
        float percent = (totalAsh > 0) ? ((float)clearedAsh / totalAsh) * 100f : 0f;
        score = Mathf.RoundToInt(percent);
        score = Mathf.Clamp(score, 0, maxScore);
    }

    private void EndGame()
    {
        bool passed = cumulativeClearedAsh >= Mathf.CeilToInt(0.6f * cumulativeTotalAsh);

        GameResults.Score = cumulativeClearedAsh;
        GameResults.MaxScore = cumulativeTotalAsh;
        GameResults.Passed = passed;
        GameResults.DisasterName = "Volcanic";
        GameResults.MiniGameIndex = 3;
        GameResults.Difficulty = "Easy";

        DBManager.SaveProgress(GameResults.DisasterName, GameResults.Difficulty, GameResults.MiniGameIndex, passed);
        SceneTracker.SetCurrentMiniGame(GameResults.DisasterName, GameResults.Difficulty, SceneManager.GetActiveScene().name);

        SceneManager.LoadScene("TransitionScene");
    }
}
