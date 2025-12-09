using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LaneDodgerController : MonoBehaviour, IGameStarter
{
    [Header("Road Setup")]
    public RectTransform road1;
    public RectTransform road2;
    public float roadScrollSpeed = 200f;
    private float roadHeight;

    [Header("Car Setup")]
    public RectTransform car;
    public float laneSwitchDelay = 0.5f;
    public float laneMoveSpeed = 8f;

    private Vector2[] lanePositions = new Vector2[3];
    private int currentLane = 1;

    [Header("Swipe Settings")]
    public float minSwipeDistance = 20f;
    private Vector2 startTouchPos;

    [Header("Collision Settings")]
    public float collisionPadding = 20f;

    [Header("Ash Setup")]
    public GameObject ashPrefab;
    public Sprite[] ashSprites;
    public float minAshSizePercent = 0.08f;
    public float maxAshSizePercent = 0.12f;
    public float spawnInterval = 1.2f;
    public float verticalSafeMultiplier = 1.5f;

    private List<RectTransform> activeAshes = new List<RectTransform>();
    private float[] lastAshYPerLane = new float[3];

    [Header("Feedback")]
    public FeedbackManager feedbackManager;

    #region Game / Scoring / Pause
    [Header("Game Settings")]
    public float gameDuration = 30f;
    private float timer;
    private bool isRunning = false;
    private bool isPaused = false;

    [Header("Scoring")]
    public int pointsPerAsh = 10;
    private int score = 0;
    private int totalSpawned = 0;
    private int totalCollided = 0;
    private int maxScore = 0;
    private int passingScore = 0;

    private Coroutine[] laneSpawnerCoroutines = new Coroutine[3];
    #endregion

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

    void Start()
    {
        roadHeight = road1.rect.height;

        road1.anchoredPosition = Vector2.zero;
        road2.anchoredPosition = new Vector2(0, roadHeight);

        ComputeLanePositions();

        for (int i = 0; i < 3; i++)
            lastAshYPerLane[i] = roadHeight;
    }

    void Update()
    {
        if (!isRunning || isPaused) return;

        ScrollRoad();

        DetectSwipe();

        CheckCollision();

        CheckAshPassed();

        timer -= Time.deltaTime;
        if (timer <= 0f)
            EndGame();
    }

    #region Road Scrolling
    void ScrollRoad()
    {
        float move = roadScrollSpeed * Time.deltaTime;
        road1.anchoredPosition -= new Vector2(0, move);
        road2.anchoredPosition -= new Vector2(0, move);

        if (road1.anchoredPosition.y <= -roadHeight)
            road1.anchoredPosition = new Vector2(0, road2.anchoredPosition.y + roadHeight);

        if (road2.anchoredPosition.y <= -roadHeight)
            road2.anchoredPosition = new Vector2(0, road1.anchoredPosition.y + roadHeight);
    }
    #endregion

    #region Lanes & Input
    void ComputeLanePositions()
    {
        float roadWidth = road1.rect.width;
        float laneWidth = roadWidth / 3f;

        lanePositions[0] = new Vector2(-laneWidth, car.anchoredPosition.y);
        lanePositions[1] = new Vector2(0, car.anchoredPosition.y);
        lanePositions[2] = new Vector2(laneWidth, car.anchoredPosition.y);
    }

    void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
            startTouchPos = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            float diffX = Input.mousePosition.x - startTouchPos.x;

            if (Mathf.Abs(diffX) >= minSwipeDistance)
            {
                if (diffX < 0) ChangeLane(-1);
                else ChangeLane(1);
            }
        }
    }

    void ChangeLane(int direction)
    {
        int nextLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (nextLane != currentLane)
        {
            currentLane = nextLane;
            StopCoroutine("AnimateCarToLane");
            StartCoroutine(AnimateCarToLane(currentLane));
        }
    }

    IEnumerator AnimateCarToLane(int lane)
    {
        Vector2 start = car.anchoredPosition;
        Vector2 target = lanePositions[lane];
        float t = 0f;

        if (laneSwitchDelay <= 0.0001f)
            laneSwitchDelay = 0.001f;

        while (t < 1f)
        {
            t += Time.deltaTime / laneSwitchDelay;
            car.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        car.anchoredPosition = target;
    }
    #endregion

    #region Spawning & Safety (Guaranteed reachable safe lane)
    public void BeginSpawning()
    {
        for (int i = 0; i < 3; i++)
        {
            if (laneSpawnerCoroutines[i] == null)
                laneSpawnerCoroutines[i] = StartCoroutine(LaneSpawner(i));
        }
    }

    public void StopSpawning()
    {
        for (int i = 0; i < 3; i++)
        {
            if (laneSpawnerCoroutines[i] != null)
            {
                StopCoroutine(laneSpawnerCoroutines[i]);
                laneSpawnerCoroutines[i] = null;
            }
        }
    }

    IEnumerator LaneSpawner(int laneIndex)
    {
        float minVertical = Mathf.Max(1f, car.rect.height * verticalSafeMultiplier);

        while (true)
        {
            while (isPaused)
                yield return null;

            float interval = Random.Range(spawnInterval * 0.7f, spawnInterval * 1.4f);
            float elapsed = 0f;
            while (elapsed < interval)
            {
                if (!isPaused)
                    elapsed += Time.deltaTime;
                yield return null;
            }

            float lastY = lastAshYPerLane[laneIndex];
            float randomSpace = Random.Range(minVertical, minVertical * 2f);
            float spawnY = Mathf.Max(roadHeight, lastY + randomSpace);

            List<int> reachable = new List<int> { currentLane };
            if (currentLane > 0) reachable.Add(currentLane - 1);
            if (currentLane < 2) reachable.Add(currentLane + 1);

            int safeLane = reachable[Random.Range(0, reachable.Count)];

            if (laneIndex == safeLane)
            {
                lastAshYPerLane[laneIndex] = spawnY;
                continue;
            }

            bool safeToSpawn = false;
            int attempts = 0;
            while (!safeToSpawn && attempts < 10)
            {
                safeToSpawn = true;

                bool anyReachableClear = false;
                foreach (int r in reachable)
                {
                    float otherY = lastAshYPerLane[r];
                    if (Mathf.Abs(spawnY - otherY) >= minVertical)
                    {
                        anyReachableClear = true;
                        break;
                    }
                }

                if (!anyReachableClear)
                {
                    safeToSpawn = false;
                    spawnY += minVertical;
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (i == laneIndex) continue;
                        float otherY = lastAshYPerLane[i];
                        if (Mathf.Abs(spawnY - otherY) < minVertical)
                        {
                            safeToSpawn = false;
                            spawnY += minVertical;
                            break;
                        }
                    }
                }

                attempts++;
            }

            if (!safeToSpawn)
            {
                lastAshYPerLane[laneIndex] = spawnY;
                continue;
            }

            float sizePercent = Random.Range(minAshSizePercent, maxAshSizePercent);
            SpawnAsh(laneIndex, sizePercent, spawnY);
        }
    }

    void SpawnAsh(int laneIndex, float sizePercent, float startY)
    {
        GameObject ash = Instantiate(ashPrefab, road1.parent);
        RectTransform ashRT = ash.GetComponent<RectTransform>();

        if (ashSprites.Length > 0 && ash.GetComponent<Image>() != null)
            ash.GetComponent<Image>().sprite = ashSprites[Random.Range(0, ashSprites.Length)];

        float size = roadHeight * sizePercent;
        ashRT.sizeDelta = new Vector2(size, size);

        ashRT.anchoredPosition = new Vector2(lanePositions[laneIndex].x, startY);

        lastAshYPerLane[laneIndex] = startY;

        AshMover mover = ash.AddComponent<AshMover>();
        mover.fallSpeed = roadScrollSpeed;
        mover.controller = this;
        mover.rect = ashRT;

        activeAshes.Add(ashRT);

        RegisterAshSpawned();
    }
    #endregion

    #region Collision & Ash Lifecycle
    void CheckCollision()
    {
        Rect carRect = GetPaddedWorldRect(car);

        foreach (RectTransform ash in activeAshes.ToArray())
        {
            if (ash == null) continue;

            Rect ashRect = GetPaddedWorldRect(ash);

            if (carRect.Overlaps(ashRect, true))
            {
                totalCollided++;
                feedbackManager.ShowNegative();

                Destroy(ash.gameObject);
                activeAshes.Remove(ash);
            }
        }
    }

    void CheckAshPassed()
    {
        for (int i = activeAshes.Count - 1; i >= 0; i--)
        {
            RectTransform ash = activeAshes[i];
            if (ash == null) { activeAshes.RemoveAt(i); continue; }

            if (ash.anchoredPosition.y <= -roadHeight)
            {
                OnAshPassed();
                Destroy(ash.gameObject);
                activeAshes.RemoveAt(i);
            }
        }
    }

    Rect GetPaddedWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Rect rect = new Rect(
            corners[0].x,
            corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y
        );

        rect.xMin += collisionPadding;
        rect.xMax -= collisionPadding;
        rect.yMin += collisionPadding;
        rect.yMax -= collisionPadding;

        return rect;
    }

    public void RemoveAsh(RectTransform rt)
    {
        if (activeAshes.Contains(rt))
            activeAshes.Remove(rt);
    }
    #endregion

    #region Scoring Helpers
    public void RegisterAshSpawned()
    {
        totalSpawned++;
        maxScore = totalSpawned * pointsPerAsh;
        passingScore = Mathf.CeilToInt(maxScore * 0.6f);
    }

    public void OnAshPassed()
    {
        score += pointsPerAsh;
    }
    #endregion

    #region IGameStarter / EndGame / Save
    public void StartGame()
    {
        Debug.Log("[LaneDodger] StartGame called");

        timer = gameDuration;
        isRunning = true;
        isPaused = false;

        score = 0;
        totalSpawned = 0;
        totalCollided = 0;
        maxScore = 0;
        passingScore = 0;

        if (road1 != null && road2 != null)
        {
            road1.anchoredPosition = Vector2.zero;
            road2.anchoredPosition = new Vector2(0, roadHeight);
        }

        for (int i = 0; i < 3; i++)
            lastAshYPerLane[i] = roadHeight;

        BeginSpawning();
    }

    public void EndGame()
    {
        if (!isRunning) return;
        isRunning = false;

        StopSpawning();

        foreach (var ash in activeAshes)
            if (ash != null) Destroy(ash.gameObject);
        activeAshes.Clear();

        int finalScore = pointsPerAsh * (totalSpawned - totalCollided);
        Debug.Log($"[LaneDodger] Game Over. Final Score = {finalScore} (spawned {totalSpawned}, hit {totalCollided})");

        GameResults.Score = finalScore;
        GameResults.MaxScore = maxScore;
        GameResults.Passed = finalScore >= passingScore;
        GameResults.DisasterName = "Volcanic";
        GameResults.MiniGameIndex = 2;
        GameResults.Difficulty = "Easy";

        DBManager.SaveProgress(GameResults.DisasterName, GameResults.Difficulty, GameResults.MiniGameIndex, GameResults.Passed);
        SceneTracker.SetCurrentMiniGame(GameResults.DisasterName, GameResults.Difficulty, SceneManager.GetActiveScene().name);

        SceneManager.LoadScene("TransitionScene");
    }
    #endregion
}
