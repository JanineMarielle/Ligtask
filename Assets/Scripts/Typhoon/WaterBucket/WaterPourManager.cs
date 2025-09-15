using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WaterPourManager : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public RectTransform bar;
    public RectTransform range;
    public Image line;
    public BucketAnimation bucket;
    public Slider progressBar;

    [Header("Settings")]
    public float gravity = -200f;
    public float tapForce = 400f;
    public float totalPourTime = 10f;
    public int totalRounds = 3;

    [Header("Scoring")]
    private int score = 0;
    private const int pointsPerRound = 30;

    private float velocity = 0f;
    private float pourProgress = 0f;
    private bool isRunning = false;
    private bool isPouring = false;

    private float barMinY;
    private float barMaxY;
    private int currentRound = 0;

    void Start()
    {
        float halfHeight = bar.rect.height / 2f;
        barMinY = -halfHeight;
        barMaxY = halfHeight;

        if (progressBar != null)
        {
            progressBar.maxValue = totalPourTime;
            progressBar.value = totalPourTime;
        }
    }

    void Update()
    {
        if (!isRunning) return;

        // --- Player input
        velocity = (Input.GetMouseButton(0) || Input.touchCount > 0) ? tapForce * Time.deltaTime : gravity * Time.deltaTime;

        // --- Move the line
        Vector3 pos = line.rectTransform.localPosition;
        pos.y = Mathf.Clamp(pos.y + velocity, barMinY, barMaxY);
        line.rectTransform.localPosition = pos;

        // --- Check overlap with range
        bool insideRange = GetWorldRect(line.rectTransform).Overlaps(GetWorldRect(range), true);

        if (insideRange)
        {
            pourProgress = Mathf.Clamp(pourProgress + Time.deltaTime, 0, totalPourTime);

            if (!isPouring)
            {
                isPouring = true;
                bucket.StartPour();
            }

            if (pourProgress >= totalPourTime)
                CompleteRound();
        }
        else if (isPouring)
        {
            isPouring = false;
            bucket.StopPour();
        }

        if (progressBar != null)
            progressBar.value = totalPourTime - pourProgress;
    }

    public void StartGame()
    {
        isRunning = true;
        velocity = 0f;
        pourProgress = 0f;
        isPouring = false;
        score = 0;
        currentRound = 1;

        if (progressBar != null)
            progressBar.value = totalPourTime;

        ResetLineAndRange();

        bucket.StopAllCoroutines();
        bucket.bucketImage.sprite = bucket.idleSprite;
    }

    private void CompleteRound()
    {
        isPouring = false;
        bucket.StopPour();

        // Add points
        score += pointsPerRound;
        Debug.Log($"✅ Round {currentRound} complete! +{pointsPerRound} points. Total score: {score}");

        if (currentRound < totalRounds)
        {
            currentRound++;
            pourProgress = 0f;
            if (progressBar != null)
                progressBar.value = totalPourTime;

            ResetLineAndRange();
        }
        else
        {
            EndGame(true);
        }
    }

    public void EndGame(bool success)
    {
        if (!isRunning) return;

        isRunning = false;
        isPouring = false;
        bucket.StopPour();

        Debug.Log(success
            ? $"🏆 All rounds completed! Final Score = {score}"
            : "❌ Failed water pouring task!");

        SaveAndTransition(success);
    }

    private void SaveAndTransition(bool success)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon";
        string difficulty = "Easy";
        int miniGameIndex = 1;

        if (currentScene.StartsWith("TyphoonEasy"))
        {
            difficulty = "Easy";
            string numPart = currentScene.Replace("TyphoonEasy", "");
            int.TryParse(numPart, out miniGameIndex);
        }
        else if (currentScene.StartsWith("TyphoonHard"))
        {
            difficulty = "Hard";
            string numPart = currentScene.Replace("TyphoonHard", "");
            int.TryParse(numPart, out miniGameIndex);
        }

        int maxScore = pointsPerRound * totalRounds;
        int passingScore = Mathf.CeilToInt(maxScore * 0.7f);
        bool passed = success && score >= passingScore;

        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = miniGameIndex;
        GameResults.Difficulty = difficulty;

        // ✅ Save progress (this also updates overall disaster flags internally)
        DBManager.SaveProgress(disaster, difficulty, miniGameIndex, passed);

        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);
        SceneManager.LoadScene("TransitionScene");
    }

    private void ResetLineAndRange()
    {
        line.rectTransform.localPosition = Vector3.zero;

        float rangeHalfHeight = range.rect.height / 2f;
        float randomY = Random.Range(barMinY + rangeHalfHeight, barMaxY - rangeHalfHeight);
        range.localPosition = new Vector3(0f, randomY, 0f);
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float minX = corners[0].x;
        float minY = corners[0].y;
        float width = corners[2].x - minX;
        float height = corners[2].y - minY;

        return new Rect(minX, minY, width, height);
    }
}
