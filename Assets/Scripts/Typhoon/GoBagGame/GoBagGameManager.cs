using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using SQLite4Unity3d;

public class GoBagGameManager : MonoBehaviour, IGameStarter
{
    [Header("UI References")]
    public ItemUI[] itemSlots;
    public RectTransform frontBackPack;
    public RectTransform backBackPack;
    public Canvas mainCanvas; 
    private RectTransform canvasRect;

    [Header("Scaling")]
    [Range(0f, 200f)]
    public float backpackScaleFactor = 100f; // % of screen height

    [Header("Backpack Position Offsets")]
    [SerializeField] private float frontBackpackYOffset = 0f;
    [SerializeField] private float backBackpackYOffset = -20f;

    [Header("Item Data")]
    public List<GoBagItemSO> allItems;
    private List<GoBagItemSO> roundPool;

    [Header("Timer Settings")]
    [SerializeField] private float roundTimerDuration = 10f; // 10 seconds per round
    private TimerLogic timer; // Timer instance

    [Header("Feedback System")]
    public FeedbackManager feedbackManager;

    // --- PAUSE FLAG ---
    private bool isPaused = false;

    private int correctItems = 0;
    private int totalAnswered = 0;
    private int itemsThisRound = 0;
    private int currentRound = 0;

    [SerializeField] private int maxRounds = 5;
    [SerializeField] private int itemsPerRound = 2;

    // Scoring
    private int score = 0;
    private const int pointsPerCorrect = 10;  
    private int maxScore = 100;      
    private int passingScore;

    private void Awake()
    {
        
        if (feedbackManager == null)
        {
            feedbackManager = FindObjectOfType<FeedbackManager>();
            if (feedbackManager != null)
                Debug.Log("[GoBagGameManager] FeedbackManager found automatically!");
            else
                Debug.LogError("[GoBagGameManager] FeedbackManager not found in scene!");
        }

        if (mainCanvas != null)
            canvasRect = mainCanvas.GetComponent<RectTransform>();
        else
        {
            Canvas foundCanvas = GetComponentInParent<Canvas>();
            if (foundCanvas != null)
            {
                mainCanvas = foundCanvas;
                canvasRect = mainCanvas.GetComponent<RectTransform>();
            }
        }

        // 🔥 Hide slots immediately (before first frame render)
        if (itemSlots != null)
        {
            foreach (var slot in itemSlots)
            {
                Debug.Log($"[GoBag] {slot.name} active at Awake? {slot.gameObject.activeSelf}");

                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        SidePanelController.OnPauseStateChanged += HandlePause; 
    }

    private void OnDisable()
    {
        SidePanelController.OnPauseStateChanged -= HandlePause; 
    }

    private void HandlePause(bool paused) 
    {
        isPaused = paused;
    }

    private void Start()
    {
        DBManager.Init();

        if (timer == null)
        {
            timer = FindObjectOfType<TimerLogic>();
            if (timer != null)
                Debug.Log("[GoBag] Found TimerLogic in scene automatically.");
            else
                Debug.LogError("[GoBag] Could not find TimerLogic in scene!");
        }

        if (itemSlots != null)
        {
            foreach (var slot in itemSlots)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
        }

        passingScore = 70;

        if (canvasRect != null)
            PositionBackPack();

        // DEBUG: confirm timer status
        if (timer == null)
            Debug.LogError("[GoBag] Timer reference not set in Inspector!");
        else
            Debug.Log("[GoBag] Timer reference found.");
    }

   public void StartRounds()
    {
        Debug.Log("[GoBag] Starting rounds...");

        currentRound = 0;
        correctItems = 0;
        totalAnswered = 0;
        score = 0;

        itemsPerRound = Mathf.Clamp(itemSlots.Length, 1, itemSlots.Length);

        // 🔥 Compute scoring thresholds
        maxScore = itemsPerRound * maxRounds * pointsPerCorrect;
        passingScore = Mathf.RoundToInt(maxScore * 0.7f); // 70%

        if (allItems != null && allItems.Count > 0)
            roundPool = allItems.OrderBy(x => Random.value).ToList();
        else
            roundPool = new List<GoBagItemSO>();

        Debug.Log($"[GoBag] Items available: {roundPool.Count}");

        SetupNextItems();

        if (timer != null)
        {
            timer.StartTimer(roundTimerDuration);
            timer.OnTimerFinished += OnTimerEnded;
        }
    }

    public void StartGame()
    {
        StartRounds();
    }

    private void PositionBackPack()
    {
        if (canvasRect == null) return;

        float canvasHeight = canvasRect.rect.height;
        float targetHeight = canvasHeight * (backpackScaleFactor / 100f);

        if (frontBackPack != null)
        {
            frontBackPack.anchorMin = new Vector2(0.5f, 0f);
            frontBackPack.anchorMax = new Vector2(0.5f, 0f);
            frontBackPack.pivot = new Vector2(0.5f, 0.5f);
            float scale = targetHeight / frontBackPack.rect.height;
            frontBackPack.localScale = Vector3.one * scale;
            float halfHeight = (frontBackPack.rect.height * scale) / 2f;
            frontBackPack.anchoredPosition = new Vector2(0f, halfHeight + frontBackpackYOffset);
        }

        if (backBackPack != null)
        {
            backBackPack.anchorMin = new Vector2(0.5f, 0f);
            backBackPack.anchorMax = new Vector2(0.5f, 0f);
            backBackPack.pivot = new Vector2(0.5f, 0f);
            float scale = targetHeight / backBackPack.rect.height;
            backBackPack.localScale = Vector3.one * scale;
            backBackPack.anchoredPosition = new Vector2(0f, backBackpackYOffset);
        }
    }

    private void SetupNextItems()
    {
        if (currentRound >= maxRounds)
        {
            EndGame();
            return;
        }

        if (itemSlots == null || itemSlots.Length == 0 || allItems == null || allItems.Count == 0)
            return;

        currentRound++;
        itemsThisRound = 0;

        foreach (var slot in itemSlots)
        {
            if (slot != null) slot.gameObject.SetActive(false);
        }

        if (roundPool.Count < itemsPerRound)
        {
            var refill = allItems.OrderBy(x => Random.value).ToList();
            roundPool.AddRange(refill);
        }

        float centerY = 10f;
        float spacing = 30f;
        float slotWidth = itemSlots[0].GetComponent<RectTransform>().rect.width;
        float totalWidth = (itemsPerRound * slotWidth) + ((itemsPerRound - 1) * spacing);
        float firstSlotX = -(totalWidth / 2f) + (slotWidth / 2f);

        for (int i = 0; i < itemsPerRound; i++)
        {
            GoBagItemSO nextItem = roundPool[0];
            roundPool.RemoveAt(0);

            ItemUI slot = itemSlots[i];
            if (slot == null) continue;

            slot.gameObject.SetActive(true);
            slot.Setup(nextItem, this);

            RectTransform rt = slot.GetComponent<RectTransform>();
            Vector2 pos = rt.anchoredPosition;
            pos.x = firstSlotX + i * (slotWidth + spacing);
            pos.y = centerY;
            rt.anchoredPosition = pos;

            itemsThisRound++;
        }
    }

    public void HandleItemResult(ItemUI slotUI, GoBagItemSO item, bool swipedUp)
    {
        if (item == null)
            return;

        totalAnswered++;

        bool isCorrect = (swipedUp && !item.isNecessary) || (!swipedUp && item.isNecessary);

        Debug.Log($"[GoBag] HandleItemResult -> Item: {item.name}, isNecessary: {item.isNecessary}, swipedUp: {swipedUp}, isCorrect: {isCorrect}, Slot: {(slotUI != null ? slotUI.name : "null")}");

        if (slotUI != null)
        {
            if (feedbackManager != null)
            {
                if (isCorrect) feedbackManager.ShowPositive();
                else feedbackManager.ShowNegative();
            }

            slotUI.gameObject.SetActive(false);
        }
        else
        {
            if (feedbackManager != null)
            {
                if (isCorrect) feedbackManager.ShowPositive();
                else feedbackManager.ShowNegative();
            }
        }

        if (isCorrect)
        {
            correctItems++;
            score += pointsPerCorrect;
        }

        itemsThisRound--;

        if (itemsThisRound <= 0)
            SetupNextItems();
    }

    public void OnItemSwiped(ItemUI slotUI, bool swipedUp)
    {
        if (slotUI == null)
        {
            Debug.LogWarning("[GoBag] OnItemSwiped called with null slotUI!");
            return;
        }

        var item = slotUI.ItemData;
        Debug.Log($"[GoBag] OnItemSwiped -> slot: {slotUI.name}, item: {(item != null ? item.name : "null")}, isNecessary: {(item != null ? item.isNecessary.ToString() : "?")}, swipedUp: {swipedUp}");

        HandleItemResult(slotUI, item, swipedUp);
    }

    public void OnItemSwiped(GoBagItemSO item, bool swipedUp)
    {
        if (item == null)
        {
            Debug.LogWarning("[GoBag] OnItemSwiped called with null item!");
            return;
        }

        ItemUI slotUI = itemSlots.FirstOrDefault(s => s.ItemData == item && s.gameObject.activeSelf);
        if (slotUI != null)
        {
            OnItemSwiped(slotUI, swipedUp);
        }
        else
        {
            Debug.LogWarning($"[GoBag] OnItemSwiped(GoBagItemSO) fallback: couldn't find active slot for item {item.name}. Proceeding with item only.");
            HandleItemResult(null, item, swipedUp);
        }
    }

    private void OnTimerEnded()
    {
        Debug.Log("[GoBag] Timer ended. Transitioning to next scene...");
        EndGame(); 
    }

    private void OnDestroy()
    {
        if (timer != null)
            timer.OnTimerFinished -= OnTimerEnded;
    }

    private void EndGame()
    {
        score = Mathf.Clamp(score, 0, maxScore);

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon"; // default
        string difficulty = "Easy";  // default
        int miniGameIndex = 1;       // Go Bag is always MiniGame 1

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

}
    // Passing score is now 60% of maxScore
