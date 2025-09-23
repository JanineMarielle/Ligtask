using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class CabinetItemHandler : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public GoBagDataTest cabinetData;
    public Transform cabinetGrid;
    public RectTransform basketDropZone;
    public FeedbackManager feedbackManager;
    public RectTransform itemContainer; // Container inside basket for items

    [Header("Prefab")]
    public GameObject itemPrefab;

    [Header("Timer Settings")]
    public TimerLogic timer;
    [SerializeField] private float gameDuration = 30f;

    private int currentRound = 1;
    [SerializeField] private int totalRounds = 2;

    [Header("Basket Padding (Editable)")]
    [SerializeField] private float basketPaddingLeft = 20f;
    [SerializeField] private float basketPaddingRight = 20f;
    [SerializeField] private float basketPaddingTop = 10f;
    [SerializeField] private float basketPaddingBottom = 10f;

    private int score = 0;
    private List<Transform> slots = new List<Transform>();
    private bool gameRunning = false;

    private HashSet<GoBagItem> collectedNecessaryItems = new HashSet<GoBagItem>();
    private List<GoBagItem> spawnedNecessaryItems = new List<GoBagItem>();

    private void Start()
    {
        foreach (Transform child in cabinetGrid)
        {
            slots.Add(child);
            child.gameObject.SetActive(false);
        }

        SpawnItems();
        EnsureBasketSpriteOnTop();
    }

    private void SpawnItems()
    {
        List<GoBagItem> itemsToSpawn = new List<GoBagItem>(cabinetData.allItems);
        Shuffle(itemsToSpawn);

        int spawnCount = Mathf.Min(itemsToSpawn.Count, slots.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            GoBagItem item = itemsToSpawn[i];
            GameObject obj = Instantiate(itemPrefab, slots[i]);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.SetParent(slots[i], false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            Image spriteImage = obj.transform.Find("SpriteHolder")?.GetComponent<Image>();
            if (spriteImage != null)
            {
                spriteImage.sprite = item.itemSprite;
                spriteImage.preserveAspect = true;
            }

            CabinetDraggableItem drag = obj.AddComponent<CabinetDraggableItem>();
            drag.manager = this;
            drag.itemData = item;
            drag.originalParent = slots[i];
            drag.enabled = true; // Items can still be dragged

            if (item.isNecessary) spawnedNecessaryItems.Add(item);
        }
    }

    public void HandleDrop(GoBagItem item, Vector2 dropPosition, CabinetDraggableItem draggable)
    {
        if (!gameRunning) return;

        Vector2 localPoint;
        Canvas canvas = basketDropZone.GetComponentInParent<Canvas>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            basketDropZone,
            dropPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint))
        {
            if (basketDropZone.rect.Contains(localPoint))
            {
                if (item.isNecessary && !collectedNecessaryItems.Contains(item))
                {
                    collectedNecessaryItems.Add(item);
                    score += 20;
                    feedbackManager?.ShowPositive();
                    StartCoroutine(SoftFallIntoBasket(draggable.gameObject));

                    if (collectedNecessaryItems.Count >= spawnedNecessaryItems.Count)
                        EndRound();
                }
                else
                {
                    score = Mathf.Max(0, score - 10);
                    feedbackManager?.ShowNegative();
                    Destroy(draggable.gameObject);
                }
            }
            else
            {
                draggable.transform.SetParent(draggable.originalParent);
                draggable.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            draggable.transform.SetParent(draggable.originalParent);
            draggable.transform.localPosition = Vector3.zero;
        }
    }

    private void EnsureBasketSpriteOnTop()
    {
        if (basketDropZone == null) return;

        var basketSprite = basketDropZone.Find("BasketSprite");
        if (basketSprite != null && itemContainer != null)
        {
            int basketIndex = basketSprite.GetSiblingIndex();
            itemContainer.SetSiblingIndex(Mathf.Max(0, basketIndex - 1));
            basketSprite.SetAsLastSibling(); // Basket always on top
        }
    }

    private IEnumerator AnimateBasketToCenter(System.Action onComplete)
{
    if (basketDropZone == null)
    {
        onComplete?.Invoke();
        yield break;
    }

    Vector3 startPos = basketDropZone.position;
    Vector3 targetPos = new Vector3(Screen.width / 2f, Screen.height / 2f, startPos.z);

    // Record initial offsets of children relative to basket
    Dictionary<Transform, Vector3> childOffsets = new Dictionary<Transform, Vector3>();
    if (itemContainer != null)
    {
        foreach (Transform child in itemContainer)
        {
            childOffsets[child] = child.position - startPos; // world offset
        }
    }

    float duration = 1f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float t = elapsed / duration;

        // Move basket
        basketDropZone.position = Vector3.Lerp(startPos, targetPos, t);

        // Move children maintaining offsets
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
            {
                if (childOffsets.ContainsKey(child))
                    child.position = basketDropZone.position + childOffsets[child];
            }
        }

        elapsed += Time.deltaTime;
        yield return null;
    }

    // Ensure final position
    basketDropZone.position = targetPos;
    if (itemContainer != null)
    {
        foreach (Transform child in itemContainer)
        {
            if (childOffsets.ContainsKey(child))
                child.position = basketDropZone.position + childOffsets[child];
        }
    }

    // Destroy or hide basket and children
    if (itemContainer != null)
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);
    }

    basketDropZone.gameObject.SetActive(false);

    onComplete?.Invoke();
}

    private IEnumerator SoftFallIntoBasket(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        Vector3 worldPos = rect.position;
        Vector3 worldScale = rect.lossyScale;

        // Reparent into itemContainer
        if (itemContainer != null)
            obj.transform.SetParent(itemContainer, true);

        rect.position = worldPos;
        rect.localScale = Vector3.one;
        rect.localScale = new Vector3(
            worldScale.x / rect.lossyScale.x,
            worldScale.y / rect.lossyScale.y,
            worldScale.z / rect.lossyScale.z
        );

        Vector3 startLocalPos = new Vector3(rect.localPosition.x, rect.localPosition.y + 200f, 0f);

        float xMin = basketDropZone.rect.xMin + basketPaddingLeft + rect.rect.width * 0.5f;
        float xMax = basketDropZone.rect.xMax - basketPaddingRight - rect.rect.width * 0.5f;
        float yMin = basketDropZone.rect.yMin + basketPaddingBottom + rect.rect.height * 0.5f;
        float yMax = basketDropZone.rect.yMax - basketPaddingTop - rect.rect.height * 0.5f;

        Vector3 endLocalPos = new Vector3(
            Random.Range(xMin, xMax),
            Random.Range(yMin, yMax),
            0f
        );

        rect.localPosition = startLocalPos;

        float duration = 1.2f + Random.Range(0f, 0.3f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            rect.localPosition = Vector3.Lerp(startLocalPos, endLocalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.localPosition = endLocalPos;

        var drag = obj.GetComponent<CabinetDraggableItem>();
        if (drag != null) drag.enabled = false;

        EnsureBasketSpriteOnTop();
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
    foreach (Transform slot in slots)
        foreach (Transform child in slot)
            Destroy(child.gameObject);

    collectedNecessaryItems.Clear();
    spawnedNecessaryItems.Clear();
    currentRound++;

    if (currentRound <= totalRounds)
    {
        SpawnItems();
    }
    else
    {
        if (timer != null) timer.StopTimer();

        // Swap cabinet first, then animate basket
        var cabinetManager = FindObjectOfType<CabinetManager>();
        if (cabinetManager != null)
        {
            cabinetManager.PlayCabinetSwap(() =>
            {
                // After swap finishes, animate basket
                StartCoroutine(AnimateBasketToCenter(() =>
                {
                    EndGame();
                }));
            });
        }
        else
        {
            // fallback: animate basket directly
            StartCoroutine(AnimateBasketToCenter(() =>
            {
                EndGame();
            }));
        }
    }
}

    public void StartGame()
    {
        if (gameRunning) return;

        score = 0;
        collectedNecessaryItems.Clear();
        gameRunning = true;

        foreach (Transform slot in slots)
        {
            slot.gameObject.SetActive(true);
            var drag = slot.GetComponentInChildren<CabinetDraggableItem>();
            if (drag != null) drag.enabled = true;
        }

        var navigator = FindObjectOfType<RowNavigator>();
        navigator?.StartZoom();

        if (timer != null)
        {
            timer.OnTimerFinished -= ForceSwap;
            timer.OnTimerFinished += ForceSwap;
            timer.StartTimer(gameDuration);
        }
    }

    private void ForceSwap()
{
    if (!gameRunning) return;
    gameRunning = false;

    foreach (Transform slot in slots)
        foreach (Transform child in slot)
            Destroy(child.gameObject);

    collectedNecessaryItems.Clear();
    spawnedNecessaryItems.Clear();

    var cabinetManager = FindObjectOfType<CabinetManager>();
    if (cabinetManager != null)
    {
        cabinetManager.PlayCabinetSwap(() =>
        {
            StartCoroutine(AnimateBasketToCenter(() =>
            {
                EndGame();
            }));
        });
    }
    else
    {
        StartCoroutine(AnimateBasketToCenter(() =>
        {
            EndGame();
        }));
    }
}

    private void EndGame()
    {
        gameRunning = false;

        int maxScore = spawnedNecessaryItems.Count * 20;
        int passingScore = Mathf.RoundToInt(maxScore * 0.6f);

        GameResults.Score = score;
        GameResults.Passed = score >= passingScore;
        GameResults.DisasterName = "Earthquake";
        GameResults.MiniGameIndex = 1;
        GameResults.Difficulty = "Easy";

        DBManager.SaveProgress(GameResults.DisasterName, GameResults.Difficulty, GameResults.MiniGameIndex, GameResults.Passed);
        SceneTracker.SetCurrentMiniGame(GameResults.DisasterName, GameResults.Difficulty, SceneManager.GetActiveScene().name);

        SceneManager.LoadScene("TransitionScene");
    }

    private void OnDestroy()
    {
        if (timer != null) timer.OnTimerFinished -= ForceSwap;
    }
}

public class CabinetDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public CabinetItemHandler manager;
    [HideInInspector] public GoBagItem itemData;
    [HideInInspector] public Transform originalParent;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(manager.cabinetGrid.parent, true);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        rectTransform.position += (Vector3)eventData.delta;
    }

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        manager.HandleDrop(itemData, rectTransform.position, this);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }
}
