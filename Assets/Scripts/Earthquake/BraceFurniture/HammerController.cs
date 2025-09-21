using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class HammerController : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public RectTransform hammerRect;
    public RectTransform furnitureRect;
    public Canvas mainCanvas;    // Hammer + furniture canvas
    public Canvas nailCanvas;    // 🔹 New canvas for spawning nails
    public HammerAnimator hammerAnimator;

    [Header("Prefabs")]
    public GameObject nailPrefab; 
    private RectTransform currentNail;

    [Header("Settings")]
    public float hammerSpeed = 500f;
    public float strikeDuration = 0.2f;
    public float nailDownDistance = 50f;

    [Header("Alignment & Padding (Editable)")]
    public float alignmentRange = 50f;
    public float horizontalPaddingFraction = 0.2f;

    [Header("Nail Overlap Settings")]
    public float startOverlapFraction = 0.1f;
    public float endOverlapFraction = 1f;
    public float nailHorizontalPadding = 0.3f;

    [Header("Round Settings")]
    public int totalRounds = 3;
    private int currentRound = 0;

    // 🔹 Scoring
    private int nailsHammered = 0;
    private int score = 0;

    private Vector3 hammerStartPos;
    private Vector3 hammerEndPos;
    private bool movingRight = true;
    private bool isStriking = false;
    private bool canStrike = true;
    private bool gameActive = false;

    public delegate void RoundEndDelegate(bool success);
    public event RoundEndDelegate OnRoundEnd;

    private void Start()
    {
        SetupHammerRange();
    }

    private void SetupHammerRange()
    {
        if (hammerRect == null || mainCanvas == null) return;

        RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float padding = canvasWidth * horizontalPaddingFraction;
        float halfHammerWidth = hammerRect.rect.width / 2f;

        hammerStartPos = new Vector3(-canvasWidth / 2 + padding + halfHammerWidth,
                                     hammerRect.localPosition.y, 0);
        hammerEndPos = new Vector3(canvasWidth / 2 - padding - halfHammerWidth,
                                   hammerRect.localPosition.y, 0);
        hammerRect.localPosition = hammerStartPos;

        if (hammerAnimator != null)
            hammerAnimator.enabled = false;
    }

    private void Update()
    {
        if (!gameActive) return;

        if (!isStriking)
            SlideHammer();

        if (!isStriking && canStrike && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
            TryHammer();
    }

    private void SlideHammer()
    {
        float step = hammerSpeed * Time.deltaTime;
        Vector3 pos = hammerRect.localPosition;
        if (movingRight)
        {
            pos.x += step;
            if (pos.x >= hammerEndPos.x) movingRight = false;
        }
        else
        {
            pos.x -= step;
            if (pos.x <= hammerStartPos.x) movingRight = true;
        }
        hammerRect.localPosition = pos;
    }

    private void TryHammer()
    {
        if (currentNail == null) return;

        isStriking = true;
        canStrike = false;

        float hammerStrikeX = hammerRect.localPosition.x - (hammerRect.rect.width * 0.3f);
        float nailX = currentNail.localPosition.x;
        bool success = Mathf.Abs(hammerStrikeX - nailX) <= alignmentRange;

        StartCoroutine(HammerStrike(success));
    }

    private IEnumerator HammerStrike(bool success)
    {
        if (hammerAnimator != null)
            hammerAnimator.PlayOnce();

        yield return new WaitForSeconds(strikeDuration);

        if (success && currentNail != null)
        {
            currentNail.localPosition -= new Vector3(0, nailDownDistance, 0);

            float furnitureTopY = furnitureRect.localPosition.y + furnitureRect.rect.height / 2f;
            float nailBottomY = currentNail.localPosition.y - currentNail.rect.height / 2f;
            float overlap = furnitureTopY - nailBottomY;

            if (overlap >= currentNail.rect.height * endOverlapFraction)
            {
                nailsHammered++;
                score = nailsHammered * 50; // 🔹 50 points per hammered nail

                currentRound++;
                OnRoundEnd?.Invoke(true);

                if (currentRound >= totalRounds)
                {
                    Debug.Log("All rounds completed!");
                    gameActive = false;
                    EndGame();
                }
                else
                {
                    StartCoroutine(NextRound(currentNail));
                }
                yield break;
            }
        }

        isStriking = false;
        yield return new WaitForSeconds(0.5f);
        canStrike = true;
    }

    private IEnumerator NextRound(RectTransform oldNail)
    {
        Vector3 startPos = oldNail.localPosition;
        Vector3 endPos = startPos + Vector3.left * nailCanvas.GetComponent<RectTransform>().rect.width;
        float t = 0f;
        float slideDuration = 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            oldNail.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        Destroy(oldNail.gameObject);

        currentNail = SpawnNailOffScreen();
        Vector3 targetPos = PositionNailOnFurniture(currentNail);
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            currentNail.localPosition = Vector3.Lerp(currentNail.localPosition, targetPos, t);
            yield return null;
        }

        hammerRect.localPosition = hammerStartPos;
        movingRight = true;
        isStriking = false;
        canStrike = true;
    }

    private RectTransform SpawnNailOffScreen()
    {
        GameObject newNail = Instantiate(nailPrefab, nailCanvas.transform);
        RectTransform rt = newNail.GetComponent<RectTransform>();

        float furnitureTopY = furnitureRect.localPosition.y + furnitureRect.rect.height / 2f;
        float nailBottomY = furnitureTopY - (rt.rect.height * startOverlapFraction);
        float nailY = nailBottomY + (rt.rect.height / 2f);

        RectTransform canvasRect = nailCanvas.GetComponent<RectTransform>();
        float halfCanvasWidth = canvasRect.rect.width / 2f;

        rt.localPosition = new Vector3(halfCanvasWidth + rt.rect.width, nailY, 0);
        return rt;
    }

    private Vector3 PositionNailOnFurniture(RectTransform rt)
    {
        float furnitureTopY = furnitureRect.localPosition.y + furnitureRect.rect.height / 2f;
        float nailBottomY = furnitureTopY - (rt.rect.height * startOverlapFraction);
        float nailY = nailBottomY + (rt.rect.height / 2f);

        RectTransform canvasRect = nailCanvas.GetComponent<RectTransform>();
        float halfCanvasWidth = canvasRect.rect.width / 2f;
        float padding = halfCanvasWidth * nailHorizontalPadding;

        float minX = -halfCanvasWidth + padding + (rt.rect.width / 2f);
        float maxX = halfCanvasWidth - padding - (rt.rect.width / 2f);
        float nailX = Random.Range(minX, maxX);

        return new Vector3(nailX, nailY, 0);
    }

    public void ResetRound()
    {
        currentRound = 0;
        nailsHammered = 0;
        score = 0;

        if (currentNail != null)
            Destroy(currentNail.gameObject);

        currentNail = Instantiate(nailPrefab, nailCanvas.transform).GetComponent<RectTransform>();
        currentNail.localPosition = PositionNailOnFurniture(currentNail);

        hammerRect.localPosition = hammerStartPos;
        movingRight = true;
        isStriking = false;
        canStrike = true;
    }

    public void StartGame()
    {
        ResetRound();
        gameActive = true;
    }

    // 🔹 Endgame with scoring + earthquake mapping
    private void EndGame()
    {
        int maxScore = totalRounds * 50;
        int passingScore = Mathf.RoundToInt(maxScore * 0.7f);

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon"; 
        string difficulty = "Easy";  
        int miniGameIndex = 2; // example index for Hammering

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
        else if (currentScene == "BraceFurniture")
        {
            disaster = "Earthquake";
            difficulty = "Easy";
        }
        else if (currentScene == "FurnitureHard")
        {
            disaster = "Earthquake";
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
