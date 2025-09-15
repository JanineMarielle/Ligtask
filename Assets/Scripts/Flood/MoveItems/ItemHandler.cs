using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine.UI;
using System.Collections;

public class ItemHandler : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public GoBagDataTest data;
    public Transform gridParent;
    public RectTransform bagDropZone;

    [Header("Prefab")]
    public GameObject itemPrefab;

    [Header("Timer Settings")]
    public TimerLogic timer;                
    [SerializeField] private float gameDuration = 30f;

    private int currentRound = 1;
    [SerializeField] private int totalRounds = 2; // currently 2 rounds

    private int score = 0;
    private List<Transform> slots = new List<Transform>();
    private bool gameRunning = false;

    // Track collected necessary items to prevent counting duplicates
    private HashSet<GoBagItem> collectedNecessaryItems = new HashSet<GoBagItem>();
    private List<GoBagItem> spawnedNecessaryItems = new List<GoBagItem>();

    private void Start()
    {
        foreach (Transform child in gridParent)
        {
            slots.Add(child);
            child.gameObject.SetActive(false); // Hide all slots initially
        }

        SpawnItems();
    }

    private void SpawnItems()
    {
        List<GoBagItem> itemsToSpawn = new List<GoBagItem>(data.allItems);
        Shuffle(itemsToSpawn);

        int spawnCount = Mathf.Min(itemsToSpawn.Count, slots.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            GoBagItem item = itemsToSpawn[i];
            GameObject obj = Instantiate(itemPrefab, slots[i]);

            // 🔹 Force reset transform so it aligns properly in grid
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.SetParent(slots[i], false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            Image spriteImage = obj.transform.Find("SpriteHolder").GetComponent<Image>();
            if (spriteImage != null)
            {
                spriteImage.sprite = item.itemSprite;
                spriteImage.preserveAspect = true;
            }

            DraggableItem drag = obj.AddComponent<DraggableItem>();
            drag.manager = this;
            drag.itemData = item;
            drag.originalParent = slots[i];
            drag.enabled = false;

            if (item.isNecessary) spawnedNecessaryItems.Add(item);
        }

    }

    public void HandleDrop(GoBagItem item, Vector2 dropPosition, DraggableItem draggable)
    {
        if (!gameRunning) return;

        Vector2 localPoint;
        Canvas canvas = bagDropZone.GetComponentInParent<Canvas>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bagDropZone,
                dropPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint))
        {
            if (bagDropZone.rect.Contains(localPoint))
            {
                if (item.isNecessary && !collectedNecessaryItems.Contains(item))
                {
                    collectedNecessaryItems.Add(item);
                    score += 20;

                    // Run fall animation into the bag
                    StartCoroutine(FallIntoBag(draggable.gameObject));

                    Debug.Log($"Collected {item.itemName} ({collectedNecessaryItems.Count}/{spawnedNecessaryItems.Count})");

                    if (collectedNecessaryItems.Count >= spawnedNecessaryItems.Count)
                    {
                        Debug.Log("[ItemHandler] All necessary items collected! Ending round...");
                        EndRound();
                        return;
                    }
                }
                else
                {
                    // Reset item if not necessary or already collected
                    draggable.transform.SetParent(draggable.originalParent);
                    draggable.transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                // Reset item if dropped outside bag
                draggable.transform.SetParent(draggable.originalParent);
                draggable.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // Safety fallback
            draggable.transform.SetParent(draggable.originalParent);
            draggable.transform.localPosition = Vector3.zero;
        }

    }

    private IEnumerator FallIntoBag(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();

        // 🔹 Re-parent to the bag’s parent so it can be layered correctly
        obj.transform.SetParent(bagDropZone.parent, true);

        // 🔹 Ensure the item is drawn *behind* the bag
        obj.transform.SetSiblingIndex(bagDropZone.GetSiblingIndex());

        // Start higher above the bag
        Vector3 startPos = bagDropZone.position + new Vector3(0, 500f, 0); // higher offset

        // Padding inside bag
        float paddingX = rect.rect.width * 0.6f;
        float paddingY = rect.rect.height * 0.6f;

        float xMin = bagDropZone.rect.xMin + paddingX;
        float xMax = bagDropZone.rect.xMax - paddingX;
        float yMin = bagDropZone.rect.yMin + paddingY;
        float yMax = bagDropZone.rect.yMax - paddingY;

        Vector2 randomLocal = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
        Vector3 endPos = bagDropZone.TransformPoint(randomLocal);

        float duration = 0.6f + Random.Range(0f, 0.4f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rect.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.position = endPos;

        // Disable dragging once inside the bag
        DraggableItem drag = obj.GetComponent<DraggableItem>();
        if (drag != null) drag.enabled = false;

    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randIndex = Random.Range(i, list.Count);
            list[i] = list[randIndex];
            list[randIndex] = temp;
        }
    }

    public int GetScore() => score;

    private void EndRound()
    {
        // Destroy all current items in slots
        foreach (Transform slot in slots)
        {
            foreach (Transform child in slot)
            {
                Destroy(child.gameObject);
            }
        }

        // Clear tracking
        collectedNecessaryItems.Clear();
        spawnedNecessaryItems.Clear();

        currentRound++;

        if (currentRound <= totalRounds)
        {
            Debug.Log($"Starting round {currentRound}...");
            SpawnItems(); // spawn new items

            // Ensure each slot's child draggable is enabled and has correct parent
            foreach (Transform slot in slots)
            {
                foreach (Transform child in slot)
                {
                    DraggableItem drag = child.GetComponent<DraggableItem>();
                    if (drag != null)
                    {
                        drag.manager = this;
                        drag.originalParent = slot;
                        drag.enabled = true;
                    }
                }
            }
        }
        else
        {
            Debug.Log("All rounds completed! Ending game...");
            EndGame();
        }

    }

    // ------------------- IGameStarter Implementation -------------------
    public void StartGame()
    {
        if (gameRunning) return;

        Debug.Log("[ItemHandler] Starting game...");

        score = 0;
        collectedNecessaryItems.Clear();
        gameRunning = true;

        foreach (Transform slot in slots)
        {
            slot.gameObject.SetActive(true);
            var draggable = slot.GetComponentInChildren<DraggableItem>();
            if (draggable != null) draggable.enabled = true;
        }

        if (timer == null)
        {
            timer = FindObjectOfType<TimerLogic>();
            if (timer == null)
                Debug.LogError("[ItemHandler] TimerLogic not found in scene!");
        }

        if (timer != null)
        {
            timer.OnTimerFinished -= EndGame;
            timer.OnTimerFinished += EndGame;
            timer.StartTimer(gameDuration);
        }
    }

    private void EndGame()
    {
        if (!gameRunning) return;
        gameRunning = false;

        // Disable draggables
        foreach (Transform slot in slots)
        {
            var draggable = slot.GetComponentInChildren<DraggableItem>();
            if (draggable != null) draggable.enabled = false;
        }

        int maxScore = spawnedNecessaryItems.Count * 20; 
        int passingScore = Mathf.RoundToInt(maxScore * 0.7f);

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon";
        string difficulty = "Easy";

        if (currentScene.Contains("Hard")) difficulty = "Hard";
        if (currentScene.StartsWith("Typhoon")) disaster = "Typhoon";
        else if (currentScene.StartsWith("Flood")) disaster = "Flood";

        GameResults.Score = score;
        GameResults.Passed = score >= passingScore;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = 1;
        GameResults.Difficulty = difficulty;

        // 🔹 Save only pass/fail, not scores
        DBManager.SaveProgress(disaster, difficulty, GameResults.MiniGameIndex, GameResults.Passed);

        // 🔹 Keep scene tracking for transition
        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

        Debug.Log($"[ItemHandler] Game ended. Score: {score}, Passed: {GameResults.Passed}");

        SceneManager.LoadScene("TransitionScene");
    }

    private void OnDestroy()
    {
        if (timer != null)
            timer.OnTimerFinished -= EndGame;
    }
}

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public ItemHandler manager;
    [HideInInspector] public GoBagItem itemData;
    [HideInInspector] public Transform originalParent;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(manager.gridParent.parent, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position += (Vector3)eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        manager.HandleDrop(itemData, rectTransform.position, this);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
}
