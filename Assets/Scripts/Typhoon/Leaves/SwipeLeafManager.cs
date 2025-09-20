using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SwipeLeafManager : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public RectTransform canvas;                  // Main canvas (fallback)
    public RectTransform customCanvas;            // Assign leaf canvas here
    public GameObject walisPrefab;
    [SerializeField] private TimerLogic timer;

    [Header("Settings")]
    [SerializeField] private float roundTimerDuration = 10f; 
    public float swipeRadius = 100f;
    public float leafPushDistance = 100f;
    public float walisAnimDuration = 0.3f;
    public float walisReturnDuration = 0.25f;
    public float swingAngle = 45f;

    private Vector2 startPos;
    private bool isSwiping = false;
    private bool gameStarted = false;

    private RectTransform walis;
    private RectTransform activeCanvas;           // The canvas used for bounds

    [Header("Swipe Settings")]
    public float maxSwipeLength = 400f;

    // --- Scoring ---
    private int totalLeaves = 0;
    private int clearedLeaves = 0;

    [Header("Game Scoring")]
    public int maxScore = 100;
    public int passingScore = 60;
    private int score = 0;

    private void Start()
    {
        if (timer == null)
        {
            timer = FindObjectOfType<TimerLogic>();
            if (timer != null)
                Debug.Log("[SwipeLeaf] Found TimerLogic automatically.");
            else
                Debug.LogError("[SwipeLeaf] TimerLogic not found in scene!");
        }

        // pick custom canvas for bounds if assigned, otherwise main canvas
        activeCanvas = customCanvas != null ? customCanvas : canvas;

        // ✅ Always spawn walis on main canvas
        if (walisPrefab != null && canvas != null)
        {
            GameObject obj = Instantiate(walisPrefab, canvas); // use main canvas
            walis = obj.GetComponent<RectTransform>();
            walis.gameObject.SetActive(false);
        }
    }

   private void Update()
    {
        if (!gameStarted) return;

        // Mobile touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                startPos = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                Vector2 endPos = touch.position;
                ProcessSwipe(startPos, endPos);
                isSwiping = false;
            }
        }

        // Mouse input fallback (desktop)
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            isSwiping = true;
        }
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            Vector2 endPos = Input.mousePosition;
            ProcessSwipe(startPos, endPos);
            isSwiping = false;
        }
    }

    // ---------------- IGameStarter ----------------
    public void StartGame()
    {
        gameStarted = true;

        // count leaves at start
        LeafController[] leaves = FindObjectsOfType<LeafController>();
        totalLeaves = leaves.Length;
        clearedLeaves = 0;

        if (timer != null)
        {
            timer.StartTimer(roundTimerDuration);
            timer.OnTimerFinished += HandleTimerEnd;
        }
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
        Debug.Log("[SwipeLeaf] Timer ended, calculating completion...");
        CalculateCompletion();
    }

    private void OnDestroy()
    {
        if (timer != null)
            timer.OnTimerFinished -= HandleTimerEnd;
    }

    // ---------------- Swipe + Animation ----------------
    private void ProcessSwipe(Vector2 start, Vector2 end)
    {
        Vector2 swipeVector = end - start;

        if (swipeVector.magnitude < 0.1f) return;

        // Clamp swipe to max length
        if (swipeVector.magnitude > maxSwipeLength)
        {
            swipeVector = swipeVector.normalized * maxSwipeLength;
            end = start + swipeVector;
        }

        Vector2 swipeDir = swipeVector.normalized;
        TriggerSwipe(start, end, swipeDir);
    }

    void TriggerSwipe(Vector2 start, Vector2 end, Vector2 dir)
    {
        if (walis == null) return;

        StopAllCoroutines();
        StartCoroutine(PlayWalisAnimation(walis, start, end, dir));

        LeafController[] leaves = FindObjectsOfType<LeafController>();
        foreach (var leaf in leaves)
        {
            // Use the main canvas camera for ScreenPoint conversion
            Vector2 leafPos = RectTransformUtility.WorldToScreenPoint(null, leaf.transform.position);

            float dist = DistancePointToLineSegment(leafPos, start, end);

            // Only affect leaves within swipe radius AND within the clamped line segment
            float lineLength = (end - start).magnitude;
            float projLength = Vector2.Dot((leafPos - start), (end - start).normalized);
            if (dist < swipeRadius && projLength >= 0 && projLength <= lineLength)
            {
                leaf.Push(dir, leafPushDistance);
            }
        }
    }

    float DistancePointToLineSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = point - a;
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(point, closest);
    }

    IEnumerator PlayWalisAnimation(RectTransform walis, Vector2 start, Vector2 end, Vector2 dir)
    {
        walis.gameObject.SetActive(true);
        walis.position = start;

        walis.localScale = (end.x > Screen.width / 2f)
            ? new Vector3(-1, 1, 1)
            : new Vector3(1, 1, 1);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion baseRot = Quaternion.Euler(0, 0, angle - 90f);
        walis.localRotation = baseRot;

        float swingDir = ((dir.x > 0 && dir.y > 0) || (dir.x > 0 && dir.y < 0)) ? -swingAngle : swingAngle;
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

    // ---------------- Scoring ----------------
    public void NotifyLeafCleared()
    {
        clearedLeaves++;
        Debug.Log($"[SwipeLeaf] Leaf cleared! {clearedLeaves}/{totalLeaves}");

        if (clearedLeaves >= totalLeaves && gameStarted)
        {
            Debug.Log("[SwipeLeaf] All leaves cleared before timer ended!");
            CalculateCompletion();
        }
    }

    private void CalculateCompletion()
    {
        gameStarted = false;

        float percent = (totalLeaves > 0) ? 
            ((float)clearedLeaves / totalLeaves) * 100f : 0f;

        score = Mathf.RoundToInt(percent);
        score = Mathf.Clamp(score, 0, maxScore);

        Debug.Log($"[SwipeLeaf] Completion: {percent:F1}%, Score: {score}");

        EndGame();
    }

    // ---------------- EndGame Logic ----------------
    private void EndGame()
    {
        score = Mathf.Clamp(score, 0, maxScore);

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon"; // default
        string difficulty = "Easy";  // default
        int miniGameIndex = 2;       // assign correct index for this mini-game

        if (currentScene.StartsWith("TyphoonEasy"))
        {
            disaster = "Typhoon";
            difficulty = "Easy";
        }
        else if (currentScene.StartsWith("TyphoonHard"))
        {
            disaster = "Typhoon";
            difficulty = "Hard";
        }

        bool passed = score >= passingScore;

        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = miniGameIndex;
        GameResults.Difficulty = difficulty;

        DBManager.SaveProgress(disaster, difficulty, miniGameIndex, passed);

        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

        SceneManager.LoadScene("TransitionScene");
    }

    // ---------------- Canvas Bounds Helper ----------------
    public bool IsOutOfBounds(Vector3 worldPos)
    {
        if (activeCanvas == null) return false;

        Vector3 localPos = activeCanvas.InverseTransformPoint(worldPos);
        Rect rect = activeCanvas.rect;

        return !rect.Contains(localPos);
    }
}
