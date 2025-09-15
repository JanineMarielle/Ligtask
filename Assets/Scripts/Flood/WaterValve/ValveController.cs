using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ValveController : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public RectTransform valveImage;  
    public Slider progressBar;        

    [Header("Settings")]
    public float requiredRotation = 360f;     
    public float sensitivity = 1f;            
    public float progressMultiplier = 0.05f;  

    private float accumulatedRotation = 0f;
    private Vector2 lastPos;
    private bool isInteracting = false;

    // --- Game Control ---
    private bool gameStarted = false;
    private int score = 0;
    private const int maxScore = 100;
    private int passingScore = 70;

    void Start()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = requiredRotation;
            progressBar.value = 0;
        }
    }

    void Update()
    {
        if (!gameStarted) return; // Valve cannot turn until game starts

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastPos = Input.mousePosition;
            isInteracting = true;
        }
        else if (Input.GetMouseButton(0) && isInteracting)
        {
            Vector2 mousePos = Input.mousePosition;
            RotateValve(lastPos, mousePos);
            lastPos = mousePos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isInteracting = false;
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastPos = touch.position;
                isInteracting = true;
            }
            else if (touch.phase == TouchPhase.Moved && isInteracting)
            {
                Vector2 touchPos = touch.position;
                RotateValve(lastPos, touchPos);
                lastPos = touchPos;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isInteracting = false;
            }
        }
    }

    void RotateValve(Vector2 prevPos, Vector2 currentPos)
    {
        Vector2 valveScreenPos = valveImage.position;

        Vector2 prevDir = prevPos - valveScreenPos;
        Vector2 currDir = currentPos - valveScreenPos;

        float angle = Vector2.SignedAngle(prevDir, currDir);

        if (angle > 0f && accumulatedRotation <= 0f)
        {
            angle = 0f;
        }

        if (angle < 0f && accumulatedRotation >= requiredRotation)
        {
            angle = 0f;
        }

        valveImage.Rotate(Vector3.forward, angle * sensitivity);

        // Update progress bar
        accumulatedRotation -= angle * progressMultiplier;
        accumulatedRotation = Mathf.Clamp(accumulatedRotation, 0, requiredRotation);

        if (progressBar != null)
            progressBar.value = accumulatedRotation;

        if (accumulatedRotation >= requiredRotation)
        {
            EndGame();
        }
    }

    // --- IGameStarter Implementation ---
    public void StartGame()
    {
        gameStarted = true;
        accumulatedRotation = 0f;
        if (progressBar != null)
            progressBar.value = 0f;

        score = 0;
        Debug.Log("[Valve] Game Started!");
    }

    // --- Endgame Logic (matches GoBagGameManager) ---
    private void EndGame()
    {
        score = Mathf.Clamp(score, 0, maxScore);

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon"; // default
        string difficulty = "Easy";  // default
        int miniGameIndex = 1;       // Valve mini-game index

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
