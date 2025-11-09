using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class TreePlantingManager : MonoBehaviour, IGameStarter
{
    [Header("UI References")]
    public Image bgImage;               // Active soil background
    public Image bgImageCopy;           // Offscreen background copy
    public RectTransform uiPanel;       // Active UI panel (hole, water, tree)
    public RectTransform uiPanelCopy;   // Offscreen UI panel copy
    public TMP_Text instructionText;

    [Header("Clock")]
    public RectTransform clockParent;
    public Transform hourHand;
    public Transform minuteHand;

    [Header("Sprites")]
    public Sprite holeSprite;
    public Sprite[] waterFrames;
    public Sprite[] treePlantFrames;

    [Header("Game Settings")]
    public int totalRounds = 5;
    public float slideDuration = 0.5f;
    public float scorePerRound = 10f;

    private bool gameStarted = false;
    private bool inputLocked = true;
    private int currentRound = 0;
    private int score = 0;
    private int currentDrainHours;
    private int gameState = 0; // 0=dig, 1=water, 2=drain, 3=plant/skip

    private Vector2 startTouchPos;
    private Vector2 endTouchPos;
    private Canvas canvas;

    private void Awake()
    {
        uiPanel.gameObject.SetActive(false);
        uiPanelCopy.gameObject.SetActive(false);
        bgImageCopy.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        inputLocked = false;
        StartRound();
        Debug.Log("✅ Tree Planting Game Started");
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        ResetClock();
    }

    void Update()
    {
        if (!gameStarted || inputLocked) return;

        if (Input.GetMouseButtonDown(0))
            startTouchPos = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            endTouchPos = Input.mousePosition;
            float swipeDist = endTouchPos.x - startTouchPos.x;

            if (swipeDist < -100f)
            {
                inputLocked = true;
                StartCoroutine(SlideToNextHole());
                return;
            }

            if (Mathf.Abs(swipeDist) < 30f)
                HandleTap();
        }
    }

    void HandleTap()
    {
        if (inputLocked) return;

        switch (gameState)
        {
            case 0:
                uiPanel.gameObject.SetActive(true);
                uiPanel.GetComponent<Image>().sprite = holeSprite;
                instructionText.text = "Tap to fill the hole with water.";
                gameState = 1;
                break;

            case 1:
                StartCoroutine(FillWithWater());
                break;

            case 3:
                StartCoroutine(PlantTreeAnimation());
                break;
        }
    }

    void StartRound()
    {
        if (currentRound >= totalRounds)
        {
            EndGame();
            return;
        }

        uiPanel.gameObject.SetActive(false);
        uiPanel.GetComponent<Image>().sprite = null;
        instructionText.text = "Tap to dig a hole.";
        gameState = 0;
        inputLocked = false;
        ResetClock();
    }

    IEnumerator FillWithWater()
    {
        inputLocked = true;
        Image uiImage = uiPanel.GetComponent<Image>();
        uiPanel.gameObject.SetActive(true);

        // Just show the full water frame (first frame)
        uiImage.sprite = waterFrames[0];

        instructionText.text = "Observe how fast the water drains...";
        yield return new WaitForSeconds(0.6f);

        // Now start the draining phase
        StartCoroutine(SimulateDrain());
    }

    IEnumerator SimulateDrain()
    {
        gameState = 2;
        inputLocked = true;

        currentDrainHours = Random.Range(0, 7); // Random hours between 0 and 6
        float hourDuration = 1.5f; // 1 hour = 1.5 seconds
        float totalDuration = currentDrainHours * hourDuration;

        if (totalDuration < 0.5f) totalDuration = 0.5f;

        float elapsed = 0f;
        Image uiImage = uiPanel.GetComponent<Image>();
        ResetClock();

        // Animate draining alongside clock spinning
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalDuration);

            // Clock movement
            float hourRot = Mathf.Lerp(0f, -30f * currentDrainHours, t);
            float minuteRot = Mathf.Lerp(0f, -360f * currentDrainHours, t);
            hourHand.localRotation = Quaternion.Euler(0, 0, hourRot);
            minuteHand.localRotation = Quaternion.Euler(0, 0, minuteRot);

            // Drain water smoothly
            int frameIndex = Mathf.FloorToInt(t * (waterFrames.Length - 1));
            uiImage.sprite = waterFrames[frameIndex];

            yield return null;
        }

        // Hints based on observed drain speed
        if (currentDrainHours < 2)
            instructionText.text = "The water drained instantly!";
        else if (currentDrainHours <= 5)
            instructionText.text = "The water drained at a steady pace.";
        else
            instructionText.text = "The water did not drain very quickly.";

        instructionText.text += "\nTap to plant a tree or swipe left to skip.";
        gameState = 3;
        inputLocked = false;
    }

    IEnumerator PlantTreeAnimation()
    {
        inputLocked = true;
        Image uiImage = uiPanel.GetComponent<Image>();

        // Play planting animation
        foreach (var frame in treePlantFrames)
        {
            uiImage.sprite = frame;
            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.3f);

        // ✅ Scoring logic
        bool correctCondition = currentDrainHours >= 2 && currentDrainHours <= 4;

        if (correctCondition)
        {
            score += Mathf.RoundToInt(scorePerRound);
            instructionText.text = "The soil drained just right — perfect for planting!";
            Debug.Log($"🌳 Round {currentRound + 1}: SUCCESS | +{scorePerRound} pts | Total: {score}");
        }
        else
        {
            instructionText.text = "The soil wasn’t suitable for planting this time.";
            Debug.Log($"🌱 Round {currentRound + 1}: NO SCORE | Drain hours: {currentDrainHours}");
        }

        yield return new WaitForSeconds(1.2f);

        StartCoroutine(SlideToNextHole());

    }

    IEnumerator SlideToNextHole()
    {
        inputLocked = true;
        bgImageCopy.gameObject.SetActive(true);
        uiPanelCopy.gameObject.SetActive(true);

        float screenWidth = canvas.GetComponent<RectTransform>().rect.width;
        RectTransform bgRect = bgImage.rectTransform;
        RectTransform bgCopyRect = bgImageCopy.rectTransform;

        uiPanelCopy.anchoredPosition = new Vector2(screenWidth, uiPanel.anchoredPosition.y);
        bgCopyRect.anchoredPosition = new Vector2(screenWidth, bgRect.anchoredPosition.y);

        Vector2 startPosUI = uiPanel.anchoredPosition;
        Vector2 startPosBG = bgRect.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            uiPanel.anchoredPosition = Vector2.Lerp(startPosUI, new Vector2(-screenWidth, startPosUI.y), t);
            bgRect.anchoredPosition = Vector2.Lerp(startPosBG, new Vector2(-screenWidth, startPosBG.y), t);

            uiPanelCopy.anchoredPosition = Vector2.Lerp(new Vector2(screenWidth, startPosUI.y), Vector2.zero, t);
            bgCopyRect.anchoredPosition = Vector2.Lerp(new Vector2(screenWidth, startPosBG.y), Vector2.zero, t);

            yield return null;
        }

        uiPanel.gameObject.SetActive(false);
        bgImage.gameObject.SetActive(false);

        // Swap references
        (uiPanel, uiPanelCopy) = (uiPanelCopy, uiPanel);
        (bgImage, bgImageCopy) = (bgImageCopy, bgImage);

        currentRound++;
        StartRound();
    }

    void ResetClock()
    {
        hourHand.localRotation = Quaternion.identity;
        minuteHand.localRotation = Quaternion.identity;
    }

    void EndGame()
    {
        if (!gameStarted) return;

        gameStarted = false;
        inputLocked = true;

        int totalScore = Mathf.Clamp(score, 0, totalRounds * 10);
        bool passed = totalScore >= (totalRounds * 10 * 0.6f);

        GameResults.Score = totalScore;
        GameResults.Passed = passed;
        GameResults.DisasterName = "Landslide";
        GameResults.MiniGameIndex = 1;
        GameResults.Difficulty = "Easy";

        DBManager.SaveProgress("Landslide", "Easy", 1, passed);
        SceneTracker.SetCurrentMiniGame("Landslide", "Easy", SceneManager.GetActiveScene().name);

        Debug.Log($"🏁 Game Ended | Score: {totalScore} | Passed: {passed}");
        SceneManager.LoadScene("TransitionScene");
    }
}
