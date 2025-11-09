using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class FireExtController : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public RectTransform extinguisher;
    public RectTransform pin;
    public RectTransform hose;
    public RectTransform fireParent;
    public GameObject firePrefab;
    public HoseAnimation hoseAnimation;
    public TimerLogic timerLogic;
    public Sprite squeezed; // ✅ pressed sprite

    [Header("Fire Settings")]
    public Sprite[] fireSprites;
    public int minFires = 2;
    public int maxFires = 5;
    public Vector2 minFireScale = new Vector2(0.8f, 0.8f);
    public Vector2 maxFireScale = new Vector2(1.5f, 1.5f);
    [Range(0f, 0.4f)] public float bottomPaddingPercent = 0.05f;
    public float holdTimeToExtinguish = 3f;

    [Header("Hose Settings")]
    public float hoseFollowSpeed = 10f;

    [Header("Visual Offsets")]
    public float extinguishYOffset = 100f;

    [Header("Extinguish Settings")]
    public float extinguishRadius = 200f;
    [Range(0.1f, 1f)] public float regrowSpeedMultiplier = 0.5f;

    [Header("Pin Animation Settings")]
    public float pinSwipeThreshold = 100f;
    public float pinSlideDistance = 150f;
    public float pinSlideDuration = 0.3f;
    public float extinguisherSlideDuration = 0.5f;

    [Header("Game Timer Settings")]
    public float gameDuration = 20f;

    private bool pinRemoved = false;
    private bool isHoldingExtinguisher = false;
    private bool isSwipingPin = false;
    private Vector2 startTouchPos;
    private List<FireData> fires = new List<FireData>();
    private bool gameEnded = false;
    private bool gameStarted = false;
    private bool canSwipePin = false;
    private Sprite originalExtinguisherSprite;

    [System.Serializable]
    private class FireData
    {
        public FireAnimation anim;
        public float holdTimer;
        public bool extinguished;
        public float baseScale;
    }

    void Start()
    {
        hose.gameObject.SetActive(false);
        originalExtinguisherSprite = extinguisher.GetComponent<UnityEngine.UI.Image>().sprite;
        SpawnFires();

        if (timerLogic != null)
            timerLogic.OnTimerFinished += HandleTimerFinished;
    }

    public void StartGame()
    {
        gameStarted = true;
        canSwipePin = false;
    }

    void OnDestroy()
    {
        if (timerLogic != null)
            timerLogic.OnTimerFinished -= HandleTimerFinished;
    }

    void Update()
    {
        if (gameEnded) return;

        if (!gameStarted && canSwipePin)
        {
            HandlePinSwipe();
            return;
        }

        if (gameStarted)
        {
            if (!pinRemoved)
                HandlePinSwipe();
            else
            {
                UpdateMultitouch();
                UpdateFiresExtinguish();
            }
        }
    }

    #region Fire Logic
    private void SpawnFires()
    {
        int fireCount = Random.Range(minFires, maxFires + 1);
        Vector2 parentSize = fireParent.rect.size;
        float safeZoneRadius = parentSize.x * 0.3f; // Safe zone bottom-left

        for (int i = 0; i < fireCount; i++)
        {
            GameObject newFire = Instantiate(firePrefab, fireParent);
            RectTransform rt = newFire.GetComponent<RectTransform>();
            FireAnimation anim = newFire.GetComponent<FireAnimation>();

            if (fireSprites.Length > 0)
                anim.fireFrames = fireSprites;

            float scale = Random.Range(minFireScale.x, maxFireScale.x);
            rt.localScale = Vector3.one * scale;

            Vector2 fireHalfSize = new Vector2(rt.rect.width, rt.rect.height) * 0.5f * scale;
            float minX = -parentSize.x / 2f + fireHalfSize.x;
            float maxX = parentSize.x / 2f - fireHalfSize.x;
            float minY = -parentSize.y / 2f + fireHalfSize.y + parentSize.y * bottomPaddingPercent;
            float maxY = parentSize.y / 2f - fireHalfSize.y;

            Vector2 pos;
            int attempts = 0;
            do
            {
                pos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                attempts++;

                // Define bottom-left exclusion zone (where extinguisher rests)
                float excludeWidth = parentSize.x * 0.35f;   // 35% of width
                float excludeHeight = parentSize.y * 0.5f;  // 50% of height

                bool inBottomLeftZone = pos.x < -parentSize.x / 2f + excludeWidth &&
                                        pos.y < -parentSize.y / 2f + excludeHeight;

                if (!inBottomLeftZone)
                    break;

            } while (attempts < 50);

            rt.localPosition = pos;
            fires.Add(new FireData { anim = anim, baseScale = scale });
        }
    }

    private void UpdateFiresExtinguish()
    {
        Vector2 hosePos = (Vector2)hose.localPosition + new Vector2(0, extinguishYOffset);

        foreach (var fire in fires)
        {
            if (fire.extinguished) continue;

            Vector2 firePos = ((RectTransform)fire.anim.transform).localPosition;
            float distance = Vector2.Distance(firePos, hosePos);
            bool isUnderSpray = isHoldingExtinguisher && distance <= extinguishRadius;

            if (isUnderSpray)
                fire.holdTimer += Time.deltaTime;
            else
                fire.holdTimer -= Time.deltaTime * regrowSpeedMultiplier;

            fire.holdTimer = Mathf.Clamp(fire.holdTimer, 0f, holdTimeToExtinguish);
            float progress = fire.holdTimer / holdTimeToExtinguish;
            float newScale = Mathf.Lerp(fire.baseScale, 0f, progress);
            fire.anim.transform.localScale = Vector3.one * newScale;

            if (progress >= 1f)
            {
                fire.extinguished = true;
                fire.anim.gameObject.SetActive(false);
            }
        }

        if (fires.TrueForAll(f => f.extinguished) && !gameEnded)
        {
            timerLogic?.StopTimer();
            HandleTimerFinished();
        }
    }
    #endregion

    #region Pin Swipe
    public void EnablePinSwipeDuringCountdown() => canSwipePin = true;

    private void HandlePinSwipe()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(pin, Input.mousePosition))
        {
            isSwipingPin = true;
            startTouchPos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && isSwipingPin)
        {
            float swipeDist = Input.mousePosition.x - startTouchPos.x;
            if (swipeDist > pinSwipeThreshold)
            {
                StartCoroutine(AnimatePinRemoval());
                isSwipingPin = false;
            }
        }
        else if (Input.GetMouseButtonUp(0)) isSwipingPin = false;
#else
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began && RectTransformUtility.RectangleContainsScreenPoint(pin, touch.position))
        {
            isSwipingPin = true;
            startTouchPos = touch.position;
        }
        else if (touch.phase == TouchPhase.Moved && isSwipingPin)
        {
            float swipeDist = touch.position.x - startTouchPos.x;
            if (swipeDist > pinSwipeThreshold)
            {
                StartCoroutine(AnimatePinRemoval());
                isSwipingPin = false;
            }
        }
        else if (touch.phase == TouchPhase.Ended) isSwipingPin = false;
#endif
    }

    private IEnumerator AnimatePinRemoval()
{
    pinRemoved = true;

    // Animate pin removal first
    Vector2 pinStart = pin.anchoredPosition;
    Vector2 pinEnd = pinStart + new Vector2(pinSlideDistance, 0);
    float t = 0f;

    while (t < pinSlideDuration)
    {
        t += Time.deltaTime;
        pin.anchoredPosition = Vector2.Lerp(pinStart, pinEnd, t / pinSlideDuration);
        yield return null;
    }

    yield return new WaitForSeconds(0.2f);

    // Reference main canvas
    Canvas mainCanvas = extinguisher.GetComponentInParent<Canvas>();
    RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();

    // Set anchor and pivot for bottom-left
    extinguisher.anchorMin = new Vector2(0f, 0f);
    extinguisher.anchorMax = new Vector2(0f, 0f);
    extinguisher.pivot = new Vector2(0.5f, 0.5f); // center pivot

    // Start from center of canvas
    Vector2 startPos = new Vector2(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f);

    // Target position: bottom-left, half offscreen vertically, flush left
    float targetX = extinguisher.rect.width * 0.2f; 
    float targetY = extinguisher.rect.height * 0.05f;
    Vector2 endPos = new Vector2(targetX, targetY);

    t = 0f;
    Vector3 startScale = extinguisher.localScale;
    Vector3 endScale = startScale * 0.5f;

    while (t < extinguisherSlideDuration)
    {
        t += Time.deltaTime;
        float progress = Mathf.SmoothStep(0, 1, t / extinguisherSlideDuration);
        extinguisher.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
        extinguisher.localScale = Vector3.Lerp(startScale, endScale, progress);
        yield return null;
    }

    extinguisher.anchoredPosition = endPos;
    extinguisher.localScale = endScale;

    // Hide pin and activate hose
    pin.gameObject.SetActive(false);
    hose.gameObject.SetActive(true);

    if (timerLogic != null)
        timerLogic.StartTimer(gameDuration);
}

    #endregion

    #region Multitouch & Spray
    private void UpdateMultitouch()
    {
        if (Input.touchCount == 0)
        {
            if (isHoldingExtinguisher)
                StopSpraying();
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            bool onExt = RectTransformUtility.RectangleContainsScreenPoint(extinguisher, touch.position);

            if (onExt && touch.phase == TouchPhase.Began)
                StartSpraying();
            else if (onExt && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                StopSpraying();
            else if (!onExt && touch.phase == TouchPhase.Moved)
                MoveHose(touch.position);
        }
    }

    private void StartSpraying()
    {
        if (isHoldingExtinguisher) return;
        isHoldingExtinguisher = true;
        extinguisher.GetComponent<UnityEngine.UI.Image>().sprite = squeezed;
        hoseAnimation.StartSpraying();
    }

    private void StopSpraying()
    {
        isHoldingExtinguisher = false;
        extinguisher.GetComponent<UnityEngine.UI.Image>().sprite = originalExtinguisherSprite;
        hoseAnimation.StopSpraying();
    }

    private void MoveHose(Vector2 screenPos)
    {
        RectTransform parentRect = hose.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, null, out Vector2 localPoint))
        {
            Vector2 clampedPos = new Vector2(
                Mathf.Clamp(localPoint.x, -parentRect.rect.width / 2f, parentRect.rect.width / 2f),
                Mathf.Clamp(localPoint.y, -parentRect.rect.height / 2f, parentRect.rect.height / 2f)
            );
            hose.localPosition = Vector3.Lerp(hose.localPosition, clampedPos, Time.deltaTime * hoseFollowSpeed);
        }
    }
    #endregion

    #region Scoring
    private void HandleTimerFinished()
    {
        if (gameEnded) return;
        gameEnded = true;

        int total = fires.Count;
        int extinguished = fires.FindAll(f => f.extinguished).Count;
        int score = Mathf.RoundToInt((extinguished / (float)total) * 100f);
        bool passed = score >= 60;

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Earthquake";
        string difficulty = currentScene.Contains("Hard") ? "Hard" : "Easy";
        int miniGameIndex = 2;

        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = miniGameIndex;
        GameResults.Difficulty = difficulty;

        DBManager.SaveProgress(disaster, difficulty, miniGameIndex, passed);
        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

        SceneManager.LoadScene("TransitionScene");
    }
    #endregion
}
