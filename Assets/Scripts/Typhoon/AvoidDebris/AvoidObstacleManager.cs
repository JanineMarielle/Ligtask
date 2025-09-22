using UnityEngine;
using UnityEngine.SceneManagement;
using SQLite4Unity3d;

public class AvoidObstacleManager : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public DebrisSpawner spawner;
    public RectTransform player;

    [Header("Background Settings")]
    public RectTransform background1;
    public RectTransform background2;
    public float backgroundSpeed = 50f; // pixels per second
    private float backgroundHeight;

    [Header("Gameplay Settings")]
    public float gameDuration = 20f; // seconds
    private float timer;
    private bool isRunning = false;

    [Header("Collision Settings")]
    public float collisionBufferX = 20f;
    public float collisionBufferY = 20f;

    [Header("Debris Spawning")]
    [Tooltip("Extra spacing offset between debris. Smaller = more crowded, Larger = more spaced.")]
    public float debrisSpawnOffset = 0f; // editable in inspector

    private string difficulty = "Easy"; // default

    [Header("Scoring")]
    public int pointsPerDebris = 10; // can also edit in inspector
    private int score = 0;
    private int totalDebrisSpawned = 0;
    private int totalDebrisHit = 0;

    private int maxScore = 0;
    private int passingScore = 0; // 70% of max score

    private bool isPaused = false;

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

    public string GetDifficulty()
    {
        return difficulty;
    }


    public void StartGame()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "AvoidDebris")
            difficulty = "Easy";
        else if (currentScene == "AvoidDebrisHard")
            difficulty = "Hard";

        Debug.Log($"[AvoidObstacleManager] Difficulty set to {difficulty}");

        // Setup background stacking
        if (background1 != null && background2 != null)
        {
            backgroundHeight = background1.rect.height;

            background1.anchoredPosition = Vector2.zero;
            background2.anchoredPosition = new Vector2(
                background1.anchoredPosition.x,
                background1.anchoredPosition.y + backgroundHeight
            );
        }

        timer = gameDuration;
        isRunning = true;
        score = 0;
        totalDebrisSpawned = 0;
        totalDebrisHit = 0;

        if (spawner != null)
        {
            if (difficulty == "Easy")
            {
                spawner.spawnInterval = 1.8f;
                spawner.laneDrift = 40f;
                spawner.safeLaneExtraWidth = 300f;
                spawner.fillRatioMin = 0.4f;
                spawner.fillRatioMax = 0.5f;
            }
            else if (difficulty == "Hard")
            {
                spawner.spawnInterval = 1.0f;
                spawner.laneDrift = 150f;
                spawner.safeLaneExtraWidth = 60f;
                spawner.fillRatioMin = 0.7f;
                spawner.fillRatioMax = 0.85f;
            }

            spawner.debrisOffset = debrisSpawnOffset;
            spawner.BeginSpawning();
        }
    }

    private void Update()
    {
        if (!isRunning || isPaused) return;

        // Background scrolling
        if (background1 != null && background2 != null)
        {
            Vector2 move = new Vector2(0, -backgroundSpeed * Time.deltaTime);
            background1.anchoredPosition += move;
            background2.anchoredPosition += move;

            if (background1.anchoredPosition.y <= -backgroundHeight)
            {
                background1.anchoredPosition = new Vector2(
                    background1.anchoredPosition.x,
                    background2.anchoredPosition.y + backgroundHeight
                );
            }
            else if (background2.anchoredPosition.y <= -backgroundHeight)
            {
                background2.anchoredPosition = new Vector2(
                    background2.anchoredPosition.x,
                    background1.anchoredPosition.y + backgroundHeight
                );
            }
        }

        // Game timer
        timer -= Time.deltaTime;
        if (timer <= 0f)
            EndGame();
    }

    public bool PlayerCollides(RectTransform debris)
    {
        if (player == null) return false;
        return RectOverlaps(player, debris, collisionBufferX, collisionBufferY);
    }

    private Rect GetWorldRect(RectTransform rt, float bufferX = 0f, float bufferY = 0f)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float xMin = corners[0].x;
        float yMin = corners[0].y;
        float width = corners[2].x - corners[0].x;
        float height = corners[2].y - corners[0].y;

        Rect rect = new Rect(xMin, yMin, width, height);
        rect.xMin += bufferX;
        rect.xMax -= bufferX;
        rect.yMin += bufferY;
        rect.yMax -= bufferY;

        return rect;
    }

    private bool RectOverlaps(RectTransform a, RectTransform b, float bufferX = 20f, float bufferY = 20f)
    {
        Rect aWorld = GetWorldRect(a, bufferX, bufferY);
        Rect bWorld = GetWorldRect(b, bufferX, bufferY);
        return aWorld.Overlaps(bWorld, true);
    }

    public void RegisterDebris()
    {
        totalDebrisSpawned++;
        maxScore = totalDebrisSpawned * pointsPerDebris;
    passingScore = Mathf.CeilToInt(maxScore * 0.6f);
    }

    public void OnDebrisDodged()
    {
        score += pointsPerDebris;
        Debug.Log($"Debris dodged! +{pointsPerDebris} points. Total score: {score}/{maxScore}");
    }

    public void OnPlayerHit()
    {
        totalDebrisHit++;
        Debug.Log($"Player hit debris! Total hits: {totalDebrisHit}");
    }

    public void EndGame()
    {
        if (!isRunning) return;
        isRunning = false;

        if (spawner != null)
            spawner.StopSpawning();

        Debug.Log($"Game Over: Score = {score}/{maxScore}, Passing = {passingScore}");

        SaveAndTransition();
    }

    private void SaveAndTransition()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon";
        string difficulty = "Easy";
        int miniGameIndex = 1;

        if (currentScene.StartsWith("TyphoonEasy"))
        {
            disaster = "Typhoon";
            difficulty = "Easy";
            string numPart = currentScene.Replace("TyphoonEasy", "");
            int.TryParse(numPart, out miniGameIndex);
        }
        else if (currentScene.StartsWith("TyphoonHard"))
        {
            disaster = "Typhoon";
            difficulty = "Hard";
            string numPart = currentScene.Replace("TyphoonHard", "");
            int.TryParse(numPart, out miniGameIndex);
        }

        // 🔹 Keep scores in memory for TransitionScene
        GameResults.Score = score;
        GameResults.MaxScore = maxScore;
        GameResults.Passed = score >= passingScore;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = miniGameIndex;
        GameResults.Difficulty = difficulty;

        // 🔹 Store only pass/fail in DB
        DBManager.SaveProgress(disaster, difficulty, miniGameIndex, GameResults.Passed);

        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);
        SceneManager.LoadScene("TransitionScene");
    }
}
