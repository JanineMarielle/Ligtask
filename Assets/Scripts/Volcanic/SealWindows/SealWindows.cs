using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SealWindows : MonoBehaviour, IGameStarter
{
    [Header("Scene Panels")]
    public List<GameObject> backgrounds;
    public Button leftArrow;
    public Button rightArrow;
    public Button backButton;

    [Header("Touch Blocker")]
    public GameObject touchBlocker;

    [Header("Zoom Settings")]
    public float zoomScale = 1.5f;

    [Header("Window Sprites")]
    public Sprite[] windowOpenVariants;
    public Sprite windowClosed;

    [Header("Door Reference")]
    public Button doorButton;
    public Sprite doorOpen;
    public Sprite doorClosed;

    [Header("Settings")]
    public int totalOpenWindows = 5;
    public TapeController tapeController;

    [Header("Timer Settings")]
    public TimerLogic timer;
    public float roundTimerDuration = 60f;

    [HideInInspector] public List<RectTransform> allWindows = new List<RectTransform>();
    [HideInInspector] public int currentIndex = 0;
    [HideInInspector] public List<RectTransform> initiallyOpenWindows = new List<RectTransform>();
    public Dictionary<RectTransform, GameObject> windowToBackground = new Dictionary<RectTransform, GameObject>();

    private Button currentUIButton;
    private bool isZoomed = false;
    private Vector3 originalBGScale;
    private Vector3 originalBGPos;

    [Header("Door Rag UI")]
    public RectTransform doorRagUI;

    private const int pointsPerDoor = 10;
    private const int pointsPerWindow = 10;
    private const int pointsPerEdge = 5;

    private float timerValue;
    private bool timerRunning = false;
    private bool gameEnded = false;

    void Start()
    {
        SetupScenes();
        ShowScene(0);

        leftArrow.onClick.AddListener(() => ShowScene(currentIndex - 1));
        rightArrow.onClick.AddListener(() => ShowScene(currentIndex + 1));
        backButton.onClick.AddListener(ExitZoom);
        backButton.gameObject.SetActive(false);

        if (touchBlocker != null)
            touchBlocker.SetActive(true);

        // Always hide rag at the start
        if (doorRagUI != null)
            doorRagUI.gameObject.SetActive(false);

        // Tape controller setup
        tapeController.allWindows = allWindows;
        tapeController.sealWindows = this;

        foreach (var win in allWindows)
        {
            if (!tapeController.windowEdges.ContainsKey(win))
            {
                tapeController.windowEdges[win] = new Dictionary<string, bool>()
                {
                    { "Top", false },
                    { "Bottom", false },
                    { "Left", false },
                    { "Right", false }
                };
            }
        }
    }

    public void StartGame()
    {
        if (touchBlocker != null)
            touchBlocker.SetActive(false);

        timerValue = roundTimerDuration;
        timerRunning = true;

        if (timer != null)
        {
            timer.StartTimer(roundTimerDuration);
            timer.OnTimerFinished += OnTimerEnded;
        }

        Debug.Log("▶ Game Started!");
    }

    void Update()
    {
        if (!timerRunning || gameEnded) return;

        timerValue -= Time.deltaTime;

        if (timerValue <= 0f)
        {
            timerValue = 0f;
            timerRunning = false;
            EndGame();
        }
    }

    private void OnTimerEnded()
    {
        EndGame();
    }

    public void CheckEndGame()
    {
        if (AllWindowsClosedAndTaped())
            EndGame();
    }

    private bool AllWindowsClosedAndTaped()
    {
        foreach (var kvp in tapeController.windowEdges)
        {
            RectTransform window = kvp.Key;
            if (window.name.Contains("Shelf")) continue;

            if (!IsWindowClosed(window)) return false;

            foreach (bool isTaped in kvp.Value.Values)
                if (!isTaped) return false;
        }
        return true;
    }

    private bool IsWindowClosed(RectTransform window)
    {
        if (window == null) return false;
        Image img = window.GetComponent<Image>();
        return img != null && img.sprite == windowClosed;
    }

    private void EndGame()
    {
        if (gameEnded) return;
        gameEnded = true;
        timerRunning = false;

        int score = 0;

        if (doorButton != null)
        {
            Image doorImg = doorButton.GetComponent<Image>();
            if (doorImg.sprite == doorClosed)
                score += pointsPerDoor; 

            if (doorRagUI != null && doorRagUI.gameObject.activeSelf)
                score += pointsPerDoor; 
        }

        foreach (RectTransform window in initiallyOpenWindows)
        {
            if (IsWindowClosed(window))
                score += pointsPerWindow;

            foreach (bool taped in tapeController.windowEdges[window].Values)
                if (taped) score += pointsPerEdge;
        }

        int maxPossible = initiallyOpenWindows.Count * (pointsPerWindow + 4 * pointsPerEdge);
        GameResults.Passed = score >= Mathf.RoundToInt(maxPossible * 0.6f);

        GameResults.Score = score;
        GameResults.DisasterName = "Volcanic";
        GameResults.MiniGameIndex = 1;
        GameResults.Difficulty = "Easy";

        DBManager.SaveProgress("Volcanic", "Easy", 1, GameResults.Passed);
        SceneTracker.SetCurrentMiniGame("Volcanic", "Easy", SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("TransitionScene");
    }

    void SetupScenes()
    {
        allWindows.Clear();
        windowToBackground.Clear();

        foreach (GameObject bg in backgrounds)
        {
            foreach (Transform child in bg.transform)
            {
                foreach (Transform nested in child)
                {
                    Button btn = nested.GetComponent<Button>();
                    if (btn != null && nested != doorButton?.transform)
                    {
                        RectTransform rt = btn.GetComponent<RectTransform>();
                        allWindows.Add(rt);
                        windowToBackground[rt] = bg;
                    }
                }

                Button directBtn = child.GetComponent<Button>();
                if (directBtn != null && child != doorButton?.transform)
                {
                    RectTransform rt = directBtn.GetComponent<RectTransform>();
                    allWindows.Add(rt);
                    windowToBackground[rt] = bg;
                }
            }
        }

        // Randomly open windows
        HashSet<int> openIndices = new HashSet<int>();
        while (openIndices.Count < Mathf.Min(totalOpenWindows, allWindows.Count))
            openIndices.Add(Random.Range(0, allWindows.Count));

        for (int i = 0; i < allWindows.Count; i++)
        {
            Button btn = allWindows[i].GetComponent<Button>();
            Image img = btn.GetComponent<Image>();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnUISelected(btn, false));

            if (openIndices.Contains(i))
            {
                img.sprite = windowOpenVariants[Random.Range(0, windowOpenVariants.Length)];
                initiallyOpenWindows.Add(allWindows[i]);
            }
            else
            {
                img.sprite = windowClosed;
            }
        }

        // Door logic
        if (doorButton != null)
        {
            Image doorImg = doorButton.GetComponent<Image>();
            bool doorIsOpen = Random.value > 0.5f;
            doorImg.sprite = doorIsOpen ? doorOpen : doorClosed;

            doorButton.onClick.RemoveAllListeners();
            doorButton.onClick.AddListener(() => OnUISelected(doorButton, true));
        }
    }

    void ShowScene(int index)
    {
        currentIndex = (index + backgrounds.Count) % backgrounds.Count;
        for (int i = 0; i < backgrounds.Count; i++)
            backgrounds[i].SetActive(i == currentIndex);
    }

    void OnUISelected(Button ui, bool door)
{
    if (door)
    {
        Image doorImg = ui.GetComponent<Image>();
        bool isOpen = doorImg.sprite == doorOpen;

        doorImg.sprite = doorClosed;
        if (doorRagUI != null)
            doorRagUI.gameObject.SetActive(isOpen ? false : true);

        return;
    }

    RectTransform bg = backgrounds[currentIndex].GetComponent<RectTransform>();
    RectTransform canvas = bg.parent.GetComponent<RectTransform>();
    RectTransform target = ui.GetComponent<RectTransform>();

    // If another window is zoomed, reset it first
    if (isZoomed && currentUIButton != null && currentUIButton != ui)
    {
        ExitZoom();
    }

    // Store original scale and position only if not already zoomed
    if (!isZoomed)
    {
        originalBGScale = bg.localScale;
        originalBGPos = bg.localPosition;
    }

    isZoomed = true;
    currentUIButton = ui;

    // Remove previous listener and add zoom-close listener
    ui.onClick.RemoveAllListeners();
    ui.onClick.AddListener(() => CloseZoomedWindow(ui));

    // Apply zoom
    bg.localScale = originalBGScale * zoomScale;

    Vector2 targetCanvasPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvas,
        RectTransformUtility.WorldToScreenPoint(null, target.position),
        null,
        out targetCanvasPos
    );

    Vector2 offset = Vector2.zero - targetCanvasPos;
    bg.localPosition = originalBGPos + new Vector3(offset.x, offset.y, 0);

    ClampBackground(bg, canvas);

    // Activate tape grid
    tapeController.ActivateForGrid(target.parent as RectTransform);

    // Hide arrows, show back button
    leftArrow.gameObject.SetActive(false);
    rightArrow.gameObject.SetActive(false);
    backButton.gameObject.SetActive(true);
}

    void CloseZoomedWindow(Button ui)
    {
        if (!isZoomed) return;

        ui.GetComponent<Image>().sprite = windowClosed;
    }

    void ExitZoom()
    {
        if (!isZoomed) return;

        tapeController.Deactivate();

        RectTransform bg = backgrounds[currentIndex].GetComponent<RectTransform>();
        bg.localScale = originalBGScale;
        bg.localPosition = originalBGPos;

        leftArrow.gameObject.SetActive(true);
        rightArrow.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);

        if (currentUIButton != null)
        {
            Button ui = currentUIButton;
            ui.onClick.RemoveAllListeners();
            ui.onClick.AddListener(() => OnUISelected(ui, false));
        }

        isZoomed = false;
        currentUIButton = null;
    }

    void ClampBackground(RectTransform bg, RectTransform canvas)
    {
        float scaledWidth = bg.rect.width * bg.localScale.x;
        float scaledHeight = bg.rect.height * bg.localScale.y;

        float canvasWidth = canvas.rect.width;
        float canvasHeight = canvas.rect.height;

        Vector3 pos = bg.localPosition;

        float maxX = (scaledWidth - canvasWidth) / 2f;
        float maxY = (scaledHeight - canvasHeight) / 2f;

        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);

        bg.localPosition = pos;
    }
}
