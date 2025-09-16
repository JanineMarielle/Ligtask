using UnityEngine;
using System.Collections;

public class DebrisSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public RectTransform spawnArea;
    public GameObject[] debrisPrefabs;
    public float spawnInterval = 1.2f;

    [Tooltip("Falling speed of debris in pixels per second")]
    public float fallSpeed = 300f;

    [Header("Manager Reference")]
    public AvoidObstacleManager manager;

    [Header("Off-Counter Settings")]
    public float debrisOffset = 120f;

    [Header("Difficulty Scaling")]
    public float safeLaneExtraWidth = 40f;
    public float fillRatioMin = 0.7f;
    public float fillRatioMax = 0.8f;

    private float spawnOffsetY = 100f;
    private bool isSpawning = false;
    private bool isPaused = false;
    private Coroutine spawnCoroutine;

    // Current safe lane boundaries
    private float safeLaneLeft;
    private float safeLaneRight;
    private float safeLaneWidth;
    private float safeLaneVerticalBuffer = 20f;

    private float lastOffCounterX = float.MinValue;

    // 🔹 Persistent safe lane
    private float currentSafeLaneCenter;
    public float laneDrift = 100f; // max shift per wave

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

    public void BeginSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;

            // Initialize safe lane to center at start
            currentSafeLaneCenter = 0f;

            spawnCoroutine = StartCoroutine(SpawnLoop());
        }
    }

    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }

    private IEnumerator SpawnLoop()
    {
        string diff = manager.GetDifficulty();

        while (isSpawning)
        {
            if (!isPaused)
                SpawnDebrisRowWave();

            float waitTime = spawnInterval;
            if (diff == "Easy") waitTime = spawnInterval * 1.8f; // slower spawns
            else if (diff == "Hard") waitTime = spawnInterval * 0.8f; // faster

            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (!isPaused)
                {
                    elapsed += Time.deltaTime;

                    if (Random.value < 0.02f)
                        SpawnOffCounterDebris();
                }
                yield return null;
            }
        }
    }

    private void SpawnDebrisRowWave()
    {
        if (debrisPrefabs.Length == 0 || manager == null) return;

        float halfWidth = spawnArea.rect.width / 2f;

        // Get actual player width
        float playerWidth = manager.player != null ? manager.player.rect.width : 200f;

        string diff = manager.GetDifficulty();

        if (diff == "Easy")
            safeLaneWidth = playerWidth + 500f;   // 🔹 much wider lane for Easy
        else if (diff == "Hard")
            safeLaneWidth = playerWidth + 80f;    // tight lane for Hard
        else
            safeLaneWidth = playerWidth + 200f;   // default/Normal


        // 🔹 Drift lane gradually
        float drift = Random.Range(-laneDrift, laneDrift);
        currentSafeLaneCenter += drift;

        currentSafeLaneCenter = Mathf.Clamp(currentSafeLaneCenter,
            -halfWidth + safeLaneWidth / 2f,
            halfWidth - safeLaneWidth / 2f);

        safeLaneLeft = currentSafeLaneCenter - safeLaneWidth / 2f;
        safeLaneRight = currentSafeLaneCenter + safeLaneWidth / 2f;

        // Spawn debris
        SpawnDebrisRow(-halfWidth, safeLaneLeft);   // left debris
        SpawnDebrisRow(safeLaneRight, halfWidth);  // right debris
    }

    private void SpawnDebrisRow(float xMin, float xMax)
    {
        float availableWidth = xMax - xMin;

        string diff = manager.GetDifficulty();
        float fillRatio = 0.7f; // default

        // 🔹 Adjust debris density per difficulty
        if (diff == "Easy")
        {
            // Instead of extremely low ratio (0.05f–0.1f),
            // scale with screen width & debris size
            float avgDebrisWidth = debrisPrefabs[0].GetComponent<RectTransform>().rect.width;
            int maxPossible = Mathf.Max(1, Mathf.FloorToInt(availableWidth / avgDebrisWidth));

            // At least 20–40% of possible slots get debris
            int debrisCount = Mathf.Clamp(Mathf.RoundToInt(maxPossible * Random.Range(0.2f, 0.4f)), 1, maxPossible);
            fillRatio = (float)debrisCount / maxPossible;
        }
        else if (diff == "Hard")
        {
            fillRatio = Random.Range(0.7f, 0.85f);
        }

        float fillWidth = availableWidth * fillRatio;
        float startX = xMin + (availableWidth - fillWidth) / 2f;
        float currentX = startX;

        while (currentX < startX + fillWidth)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            GameObject debris = Instantiate(prefab, spawnArea);
            RectTransform rt = debris.GetComponent<RectTransform>();

            float debrisWidth = rt.rect.width;
            float debrisHeight = rt.rect.height;

            if (currentX + debrisWidth > startX + fillWidth)
            {
                Destroy(debris);
                break;
            }

            float debrisX = currentX + debrisWidth / 2f;

            // 🔹 Base spawn Y
            float spawnY = spawnArea.rect.height / 2f + debrisHeight / 2f + safeLaneVerticalBuffer;
            if (diff == "Easy")
                spawnY += debrisHeight * Random.Range(1.0f, 1.5f);

            rt.anchoredPosition = new Vector2(debrisX, spawnY);

            var mover = debris.AddComponent<DebrisMover>();
            mover.Init(fallSpeed, manager);
            manager.RegisterDebris();

            // 🔹 Proportional spacing
            if (diff == "Easy")
                currentX += debrisWidth * Random.Range(1.5f, 2.0f);
            else
                currentX += debrisWidth + Random.Range(10f, 30f);
        }

    }

    private void SpawnOffCounterDebris()
    {
        if (debrisPrefabs.Length == 0 || manager == null) return;

        string diff = manager.GetDifficulty();

        GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
        GameObject debris = Instantiate(prefab, spawnArea);
        RectTransform rt = debris.GetComponent<RectTransform>();

        float halfWidth = spawnArea.rect.width / 2f;
        float buffer = 10f;
        float debrisWidth = rt.rect.width;
        float debrisHeight = rt.rect.height;

        // 🔹 Horizontal placement outside safe lane
        float randomX = Random.value < 0.5f
            ? Random.Range(-halfWidth, safeLaneLeft - buffer)
            : Random.Range(safeLaneRight + buffer, halfWidth);

        // 🔹 Scale horizontal spacing with debris size
        float minOffset = diff == "Easy" ? debrisWidth * 2.0f : debrisOffset;
        if (Mathf.Abs(randomX - lastOffCounterX) < minOffset)
        {
            Destroy(debris);
            return;
        }
        lastOffCounterX = randomX;

        // 🔹 More vertical spacing in Easy
        float spawnY = spawnArea.rect.height / 2f + debrisHeight / 2f + spawnOffsetY;
        if (diff == "Easy")
            spawnY += debrisHeight * Random.Range(1.2f, 2.0f);

        rt.anchoredPosition = new Vector2(randomX, spawnY);

        // Randomly flip sprite
        if (Random.value < 0.5f)
        {
            Vector3 scale = rt.localScale;
            scale.x *= -1f;
            rt.localScale = scale;
        }

        var mover = debris.AddComponent<DebrisMover>();
        mover.Init(fallSpeed, manager);

        manager.RegisterDebris();
    }
}
