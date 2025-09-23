using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using SQLite4Unity3d;

public class ProgressGround : MonoBehaviour, IGameStarter
{
    private const int ARROW_UP = 0;
    private const int ARROW_DOWN = 1;
    private const int ARROW_RIGHT = 2;

    [Header("References")]
    public RectTransform car;
    public RectTransform building;

    [Header("Offsets")]
    public float carOffsetX = -50f;
    public float carOffsetY = 0f;
    public float buildingOffsetX = 50f;
    public float buildingOffsetY = 0f;

    [Header("Layout")]
    [SerializeField] private float rowHeight = 100f;
    private Vector2 carStartPos;

    [Header("Arrow Sequence System")]
    public Image arrowSlot;
    public Sprite upArrow, downArrow, rightArrow;

    [Header("Arrow Buttons")]
    public RectTransform arrowGrid;
    public float arrowGridPaddingY = 20f;
    public Button upBtn, downBtn, rightBtn;

    [Header("Feedback System")]
    public FeedbackManager feedbackManager;

    [Header("Settings")]
    [Tooltip("Number of rounds the player must complete")]
    public int totalRounds = 3;

    [Tooltip("Number of arrows per round/sequence")]
    public int arrowsPerSequence = 4;

    private readonly List<List<int>> allSequences = new List<List<int>>();
    private List<int> sequence = new List<int>();
    private readonly List<int> playerInput = new List<int>();
    private bool inputEnabled = false;

    // 🔹 Score removed, only track pass/fail
    private int correctInputs = 0;
    private int totalInputs = 0;

    private int currentRound = 0;
    private int currentRow = 1;

    private int totalRightArrows = 0;
    private float rightStepDistance;

    private bool isPaused = false;
    private Coroutine sequenceCoroutine = null;

    private void Awake()
    {
        if (feedbackManager == null)
        {
            feedbackManager = FindObjectOfType<FeedbackManager>();
            if (feedbackManager != null)
                Debug.Log("[ProgressGround] FeedbackManager found automatically!");
            else
                Debug.LogError("[ProgressGround] FeedbackManager not found in scene!");
        }
    }

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
        DBManager.Init();
        if (arrowSlot != null) arrowSlot.gameObject.SetActive(false);

        float parentWidth = ((RectTransform)car.parent).rect.width;
        float startX = -parentWidth * 0.5f + carOffsetX;
        float startY = carOffsetY;
        carStartPos = new Vector2(startX, startY);
        car.anchoredPosition = carStartPos;
        currentRow = 1;

        PositionArrowGrid();
        SetButtonsInteractable(false);
        PreGenerateSequences();

        // Recalculate right step after sequence generation
        float rightEdgeX = parentWidth * 0.5f;
        float totalDistance = rightEdgeX - carStartPos.x;
        rightStepDistance = totalDistance / Mathf.Max(1, totalRightArrows);

        totalInputs = arrowsPerSequence * totalRounds;

        Debug.Log($"[Start] StartX={carStartPos.x}, RightEdgeX={rightEdgeX}, TotalRightArrows={totalRightArrows}, RightStep={rightStepDistance}");
    }

    void PreGenerateSequences()
    {
        allSequences.Clear();
        totalRightArrows = 0;

        for (int r = 0; r < totalRounds; r++)
        {
            List<int> newSeq = new List<int>();
            int row = 1;
            for (int i = 0; i < arrowsPerSequence; i++)
            {
                int arrow = GetValidArrow(row);
                newSeq.Add(arrow);
                if (arrow == ARROW_UP && row < 2) row++;
                else if (arrow == ARROW_DOWN && row > 0) row--;
                else if (arrow == ARROW_RIGHT) totalRightArrows++;
            }
            allSequences.Add(newSeq);
        }

        if (totalRightArrows == 0)
        {
            allSequences[0][0] = ARROW_RIGHT;
            totalRightArrows = 1;
            Debug.LogWarning("[ProgressGround] No Right arrows generated; forced one Right at [round0,index0].");
        }
    }

    public void StartGame()
    {
        Debug.Log("ProgressGround Game Started!");
        currentRound = 0;
        correctInputs = 0;
        StartNextRound();
    }

    void PositionArrowGrid()
    {
        if (arrowGrid != null)
        {
            arrowGrid.anchoredPosition = new Vector2(
                arrowGrid.anchoredPosition.x,
                car.anchoredPosition.y + arrowGridPaddingY
            );
        }
    }

    void StartNextRound()
    {
        currentRound++;
        if (currentRound > totalRounds)
        {
            EndGame();
            return;
        }
        if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = StartCoroutine(PlaySequence(allSequences[currentRound - 1]));
    }

    IEnumerator PlaySequence(List<int> seq)
    {
        sequence = seq;
        playerInput.Clear();
        for (int i = 0; i < sequence.Count; i++)
        {
            while (isPaused) yield return null;
            int arrow = sequence[i];
            arrowSlot.sprite = GetArrowSprite(arrow);
            arrowSlot.gameObject.SetActive(true);
            float elapsed = 0f;
            float displayTime = 0.6f;
            while (elapsed < displayTime)
            {
                if (!isPaused) elapsed += Time.deltaTime;
                yield return null;
            }
            arrowSlot.gameObject.SetActive(false);
            elapsed = 0f;
            float gapTime = 0.2f;
            while (elapsed < gapTime)
            {
                if (!isPaused) elapsed += Time.deltaTime;
                yield return null;
            }
        }
        inputEnabled = true;
        SetButtonsInteractable(true);
    }

    private int GetValidArrow(int row)
    {
        List<int> possible = new List<int> { ARROW_RIGHT };
        if (row < 2) possible.Add(ARROW_UP);
        if (row > 0) possible.Add(ARROW_DOWN);
        return possible[Random.Range(0, possible.Count)];
    }

    public void OnUpButton() => OnArrowButton(ARROW_UP);
    public void OnDownButton() => OnArrowButton(ARROW_DOWN);
    public void OnRightButton() => OnArrowButton(ARROW_RIGHT);

    public void OnArrowButton(int arrowIndex)
    {
        if (!inputEnabled || isPaused) return;

        // Map any unexpected "3" input to ARROW_RIGHT
        if (arrowIndex == 3) arrowIndex = ARROW_RIGHT;

        if (arrowIndex < ARROW_UP || arrowIndex > ARROW_RIGHT) return;

        int currentIndex = playerInput.Count;
        if (currentIndex >= sequence.Count) return;

        int expected = sequence[currentIndex];
        playerInput.Add(arrowIndex);

        if (arrowIndex == expected)
        {
            MoveCar(arrowIndex);
            correctInputs++; // Keep current scoring logic
            if (feedbackManager != null) feedbackManager.ShowPositive();
        }
        else
        {
            if (feedbackManager != null) feedbackManager.ShowNegative();
        }

        if (playerInput.Count == sequence.Count)
        {
            inputEnabled = false;
            SetButtonsInteractable(false);
            Invoke(nameof(StartNextRound), 1f);
        }
    }

    private void EndGame()
{
    int maxScore = totalInputs * 10; 
    int score = Mathf.Clamp(correctInputs * 10, 0, maxScore);  
    int passingScore = Mathf.RoundToInt(maxScore * 0.6f); 
    bool passed = score >= passingScore;

    string currentScene = SceneManager.GetActiveScene().name;
    string disaster = "Typhoon"; 
    string difficulty = "Easy";   
    int miniGameIndex = 2;        

    if (currentScene.StartsWith("TyphoonEasy"))
    {
        difficulty = "Easy";
        string numberPart = currentScene.Replace("TyphoonEasy", "");
        int.TryParse(numberPart, out miniGameIndex);
    }
    else if (currentScene.StartsWith("TyphoonHard"))
    {
        difficulty = "Hard";
        string numberPart = currentScene.Replace("TyphoonHard", "");
        int.TryParse(numberPart, out miniGameIndex);
    }

    GameResults.Score = score;
    GameResults.MaxScore = maxScore;
    GameResults.Passed = passed;
    GameResults.DisasterName = disaster;
    GameResults.MiniGameIndex = miniGameIndex;
    GameResults.Difficulty = difficulty;

    DBManager.SaveProgress(disaster, difficulty, miniGameIndex, passed);

    SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

    SceneManager.LoadScene("TransitionScene");
}

    Sprite GetArrowSprite(int arrow)
    {
        switch (arrow)
        {
            case ARROW_UP: return upArrow;
            case ARROW_DOWN: return downArrow;
            case ARROW_RIGHT: return rightArrow;
        }
        return null;
    }

    void SetButtonsInteractable(bool state)
    {
        upBtn.interactable = state;
        downBtn.interactable = state;
        rightBtn.interactable = state;
    }

    void MoveCar(int arrow)
    {
        Vector2 pos = car.anchoredPosition;

        // Vertical movement
        if (arrow == ARROW_UP && currentRow < 2)
        {
            currentRow++;
            pos.y = Mathf.Min(pos.y + rowHeight, carStartPos.y + rowHeight);
        }
        else if (arrow == ARROW_DOWN && currentRow > 0)
        {
            currentRow--;
            pos.y = Mathf.Max(pos.y - rowHeight, carStartPos.y - rowHeight);
        }

        // Horizontal movement
        if (arrow == ARROW_RIGHT)
        {
            float rightEdgeX = ((RectTransform)car.parent).rect.width * 0.5f;
            pos.x = Mathf.Min(pos.x + rightStepDistance, rightEdgeX);
            Debug.Log($"[MoveCar] RIGHT step={rightStepDistance}, newX={pos.x}");
        }

        car.anchoredPosition = pos;
    }

}
