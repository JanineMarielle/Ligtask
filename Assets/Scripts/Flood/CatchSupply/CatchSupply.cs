using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class CatchSupply : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public GoBagDataTest goBagData;
    public RectTransform catchArea;        
    public GameObject foodPrefab;          
    public Canvas canvas;
    public FeedbackManager feedbackManager; 
    public TimerLogic timerLogic;           

    [Header("Settings")]
    public float spawnInterval = 1f;
    public float fallSpeed = 300f;
    public int pointsPerCatch = 10;
    public Vector2 basketHitboxScale = new Vector2(0.8f, 0.5f);
    public float feedbackOffsetY = 100f;
    public float gameDuration = 30f;

    // ✅ Score & Progress
    private int score;
    private int maxScore;
    private int passingScore;
    private bool gameEnded;

    // ✅ Counters
    private int necessarySpawned;
    private int necessaryCaught;
    private int necessaryMissed;

    private RectTransform canvasRect;
    private bool gameActive = false;

    // 🔹 Pause flag
    private bool isPaused = false;

    void Start()
    {
        canvasRect = canvas.GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        // 🔹 Subscribe to pause events
        SidePanelController.OnPauseStateChanged += OnPauseStateChanged;
    }

    void OnDisable()
    {
        // 🔹 Unsubscribe from pause events
        SidePanelController.OnPauseStateChanged -= OnPauseStateChanged;

        if (timerLogic != null)
            timerLogic.OnTimerFinished -= EndGame;
    }

    private void OnPauseStateChanged(bool paused) // 🔹 NEW
    {
        isPaused = paused;
    }

    public void StartGame()
    {
        if (goBagData == null || goBagData.allItems.Count == 0)
        {
            Debug.LogError("[CatchSupply] No items assigned!");
            return;
        }

        score = 0;
        necessarySpawned = 0;
        necessaryCaught = 0;
        necessaryMissed = 0;
        gameEnded = false;
        gameActive = true;

        if (timerLogic != null)
        {
            timerLogic.StartTimer(gameDuration);
            timerLogic.OnTimerFinished += EndGame;
        }

        StartCoroutine(SpawnFood());

        Debug.Log("[CatchSupply] Game started. Tracking necessary items for passing...");
    }

    void OnDestroy()
    {
        if (timerLogic != null)
            timerLogic.OnTimerFinished -= EndGame;
    }

    void Update()
    {
        if (gameActive && !isPaused)
            HandleBasketTouch();
    }

    void HandleBasketTouch()
    {
        Vector3 newPos = catchArea.localPosition;
        Vector2 localPoint;

#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, canvas.worldCamera, out localPoint);
            newPos.x = Mathf.Clamp(localPoint.x,
                -canvasRect.rect.width / 2 + catchArea.rect.width / 2,
                 canvasRect.rect.width / 2 - catchArea.rect.width / 2);
        }
#else
        if (Input.touchCount > 0)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.GetTouch(0).position, canvas.worldCamera, out localPoint);
            newPos.x = Mathf.Clamp(localPoint.x,
                -canvasRect.rect.width / 2 + catchArea.rect.width / 2,
                 canvasRect.rect.width / 2 - catchArea.rect.width / 2);
        }
#endif

        newPos.y = catchArea.localPosition.y;
        catchArea.localPosition = newPos;
    }

    IEnumerator SpawnFood()
    {
        while (gameActive)
        {
            if (!isPaused) // 🔹 Skip spawning while paused
                SpawnSingleFood();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnSingleFood()
    {
        if (!gameActive || isPaused) return; // 🔹 Block spawns during pause

        int index = Random.Range(0, goBagData.allItems.Count);
        GoBagItem item = goBagData.allItems[index];

        if (item.isNecessary)
            necessarySpawned++;

        GameObject foodGO = Instantiate(foodPrefab, canvas.transform);
        Image img = foodGO.GetComponent<Image>();
        img.sprite = item.itemSprite;

        RectTransform foodRect = foodGO.GetComponent<RectTransform>();
        float halfWidth = canvasRect.rect.width / 2 - 20;
        float xPos = Random.Range(-halfWidth, halfWidth);
        float yPos = canvasRect.rect.height / 2 + 100;

        foodRect.localPosition = new Vector3(xPos, yPos, 0);

        StartCoroutine(FallFood(foodGO, item));
    }

    IEnumerator FallFood(GameObject foodGO, GoBagItem item)
    {
        RectTransform rect = foodGO.GetComponent<RectTransform>();

        while (gameActive && rect.localPosition.y > -(canvasRect.rect.height / 2 + 100))
        {
            if (!isPaused) // 🔹 Freeze in place when paused
            {
                rect.localPosition += Vector3.down * fallSpeed * Time.deltaTime;

                if (RectOverlapsScaled(rect, catchArea, basketHitboxScale))
                {
                    if (item.isNecessary)
                    {
                        score += pointsPerCatch;
                        necessaryCaught++;
                        Debug.Log($"Caught NECESSARY: {item.itemName} | Score={score}");
                        ShowFeedbackAboveBasket(true);
                    }
                    else
                    {
                        score -= pointsPerCatch;
                        Debug.Log($"Caught UNNECESSARY: {item.itemName} | Score={score}");
                        ShowFeedbackAboveBasket(false);
                    }

                    Destroy(foodGO);
                    yield break;
                }
            }

            yield return null;
        }

        // Fell past basket
        if (item.isNecessary)
            necessaryMissed++;

        Destroy(foodGO);
    }

    bool RectOverlapsScaled(RectTransform food, RectTransform basket, Vector2 scale)
    {
        Vector3[] foodCorners = new Vector3[4];
        Vector3[] basketCorners = new Vector3[4];
        food.GetWorldCorners(foodCorners);
        basket.GetWorldCorners(basketCorners);

        Rect foodRect = new Rect(foodCorners[0], foodCorners[2] - foodCorners[0]);
        Rect basketRect = new Rect(basketCorners[0], basketCorners[2] - basketCorners[0]);

        Vector2 size = basketRect.size;
        Vector2 newSize = new Vector2(size.x * scale.x, size.y * scale.y);
        Vector2 offset = (size - newSize) / 2f;
        basketRect = new Rect(basketRect.position + offset, newSize);

        return foodRect.Overlaps(basketRect);
    }

    void ShowFeedbackAboveBasket(bool positive)
    {
        if (feedbackManager == null) return;

        RectTransform basketRect = catchArea;
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Camera.main.WorldToScreenPoint(basketRect.position + Vector3.up * feedbackOffsetY),
            canvas.worldCamera,
            out anchoredPos
        );

        feedbackManager.feedbackPrefab.rectTransform.anchoredPosition = anchoredPos;

        if (positive) feedbackManager.ShowPositive();
        else feedbackManager.ShowNegative();
    }

    private void EndGame()
    {
        if (gameEnded) return;
        gameEnded = true;
        gameActive = false;

        // ✅ Clamp score to prevent negatives
        score = Mathf.Clamp(score, 0, int.MaxValue);

        // ✅ Define a passing score: 70% of the maximum possible
        int maxPossibleScore = necessarySpawned * pointsPerCatch;
        int threshold = Mathf.RoundToInt(maxPossibleScore * 0.7f); // 70%

        bool passed = score >= threshold;

        Debug.Log($"[CatchSupply] EndGame: necessarySpawned={necessarySpawned}, caught={necessaryCaught}, missed={necessaryMissed}, " +
                  $"score={score}, maxPossibleScore={maxPossibleScore}, threshold={threshold}, passed={passed}");

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Flood";
        string difficulty = "Easy";
        int miniGameIndex = 2; // Assuming CatchSupply is MiniGame 2 for Flood

        // Difficulty check
        if (currentScene.Equals("CatchSupplyHard"))
            difficulty = "Hard";
        else if (currentScene.Equals("CatchSupply"))
            difficulty = "Easy";

        // Disaster check (always Flood for this mini-game)
        if (currentScene.StartsWith("CatchSupply"))
            disaster = "Flood";

        // Save results
        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = miniGameIndex;
        GameResults.Difficulty = difficulty;

        DBManager.SaveProgress(disaster, difficulty, miniGameIndex, passed);
        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

        Debug.Log($"[CatchSupply] Game ended. Score: {score}, Passed: {passed}, Difficulty: {difficulty}, Scene: {currentScene}");


        SceneManager.LoadScene("TransitionScene");
    }

}
