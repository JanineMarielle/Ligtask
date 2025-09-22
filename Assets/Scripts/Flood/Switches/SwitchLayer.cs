using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SwitchLayer : MonoBehaviour, IGameStarter
{
    [Header("References")]
    public GameObject overlayPrefab;      
    public RectTransform breakerParent;  
    public Transform gridParent;           
    public RectTransform mainBreaker;  
    public RectTransform mainSwitch;    
    private Vector2 mainSwitchHomePos;   
    
    [Header("UI References")]
    public TextMeshProUGUI roundText; 

    [Header("Timer Settings")]
    public TimerLogic timerLogic;            // Reference to your TimerLogic component
    public float roundDuration = 20f;        // Editable per-round duration

    [Header("Switch Offset Settings")]
    public float rightOnOffset = 10f;   
    public float leftOnOffset = -10f;  
    public float rightOffOffset = 35f;  
    public float leftOffOffset = -35f; 

    [Header("Main Breaker Offset Settings (X Axis)")]
    public float mainBreakerOnOffsetX = 0f;
    public float mainBreakerOffOffsetX = -40f; 

    [Header("Overlay Size")]
    public float overlayWidth = 100f;
    public float overlayHeight = 60f;

    [Header("Rounds Settings")]
    public int totalRounds = 3;
    private int currentRound = 0;

    [Header("Slide Settings")]
    public float slideDistance = 800f;
    public float slideDuration = 1f;

    [Header("Scoring")]
    public int pointsPerSwitch = 10;
    private int score = 0;
    private int maxScore = 0;
    private int passingScore = 0;

    private List<SwitchBehaviour> switches = new List<SwitchBehaviour>();
    private Button mainBreakerButton;
    private bool mainBreakerOn = true;
    private int switchesPerRound = 0;
    private int totalRemainingOn = 0;

    void Awake()
    {
        if (overlayPrefab == null) Debug.LogError("[SwitchLayer] overlayPrefab not assigned!");
        if (breakerParent == null) Debug.LogError("[SwitchLayer] breakerParent not assigned!");
        if (gridParent == null) Debug.LogError("[SwitchLayer] gridParent not assigned!");
        if (mainBreaker == null) Debug.LogWarning("[SwitchLayer] mainBreaker not assigned - round control will not work!");

        if (timerLogic == null) Debug.LogWarning("[SwitchLayer] TimerLogic not assigned!");
    }

    public void StartGame()
    {
        StartRounds();
    }

    public void StartRounds()
    {
        currentRound = 0;
        score = 0;

        if (mainSwitch != null)
            mainSwitchHomePos = mainSwitch.anchoredPosition;

        switchesPerRound = 0;
        for (int panelIndex = 0; panelIndex < gridParent.childCount; panelIndex++)
        {
            switchesPerRound += gridParent.GetChild(panelIndex).childCount;
        }

        if (switchesPerRound <= 0)
        {
            Debug.LogWarning("[SwitchLayer] No switch cells found in gridParent. Aborting StartRounds.");
            return;
        }

        maxScore = switchesPerRound * totalRounds * pointsPerSwitch;
        passingScore = Mathf.RoundToInt(maxScore * 0.7f);

        Debug.Log($"[SwitchLayer] Starting rounds. Rounds: {totalRounds}, switches/round: {switchesPerRound}, maxScore: {maxScore}, passing: {passingScore}");

        StartRound(firstRound: true);
    }

    // ---------------- Round Lifecycle ----------------
    void StartRound(bool firstRound = false)
    {
        ClearExistingOverlays();
        switches.Clear();

        AddOverlays();
        RandomizeSwitches();

        if (mainBreaker != null)
        {
            mainBreakerButton = mainBreaker.GetComponent<Button>();
            if (mainBreakerButton == null) mainBreakerButton = mainBreaker.gameObject.AddComponent<Button>();

            mainBreakerButton.onClick.RemoveAllListeners();
            mainBreakerButton.onClick.AddListener(OnMainBreakerTapped);

            SetMainBreakerState(true);
        }

        currentRound++;

        // ✅ Update round display text
        if (roundText != null)
        {
            roundText.text = $"Round: {currentRound}/{totalRounds}";
        }

        if (!firstRound)
        {
            Vector2 startPos = breakerParent.anchoredPosition + new Vector2(slideDistance, 0);
            Vector2 endPos = breakerParent.anchoredPosition;
            breakerParent.anchoredPosition = startPos;
            StartCoroutine(SlideIn(breakerParent, startPos, endPos));
        }

        // 🔹 Start round timer
        if (timerLogic != null)
        {
            timerLogic.StopTimer();
            timerLogic.StartTimer(roundDuration);
            timerLogic.OnTimerFinished -= HandleTimerFinished; // avoid duplicate subscriptions
            timerLogic.OnTimerFinished += HandleTimerFinished;
        }

        Debug.Log($"[SwitchLayer] Round {currentRound} started with timer {roundDuration} seconds.");
    }

    void HandleTimerFinished()
    {
        Debug.Log("[SwitchLayer] Timer finished, forcing main breaker tap.");
        OnMainBreakerTapped();
    }

    // ---------------- Switches ----------------
    void AddOverlays()
    {
        for (int panelIndex = 0; panelIndex < gridParent.childCount; panelIndex++)
        {
            Transform panel = gridParent.GetChild(panelIndex);

            for (int i = 0; i < panel.childCount; i++)
            {
                Transform cell = panel.GetChild(i);

                GameObject overlay = Instantiate(overlayPrefab, cell);
                overlay.transform.SetAsLastSibling();

                RectTransform overlayRT = overlay.GetComponent<RectTransform>();
                if (overlayRT != null)
                {
                    overlayRT.anchoredPosition = Vector2.zero;
                    overlayRT.sizeDelta = new Vector2(overlayWidth, overlayHeight);
                }

                SwitchBehaviour sb = overlay.AddComponent<SwitchBehaviour>();
                sb.Setup(panelIndex == 0, this);
                switches.Add(sb);
            }
        }
    }

    void ClearExistingOverlays()
    {
        for (int panelIndex = 0; panelIndex < gridParent.childCount; panelIndex++)
        {
            Transform panel = gridParent.GetChild(panelIndex);
            for (int i = 0; i < panel.childCount; i++)
            {
                Transform cell = panel.GetChild(i);
                for (int c = cell.childCount - 1; c >= 0; c--)
                {
                    Destroy(cell.GetChild(c).gameObject);
                }
            }
        }
    }

    void RandomizeSwitches()
    {
        foreach (var sw in switches)
        {
            bool setOn = Random.value > 0.5f;
            sw.SetState(setOn);
        }

        bool allOff = true;
        foreach (var sw in switches)
        {
            if (sw.IsOn) { allOff = false; break; }
        }

        if (allOff && switches.Count > 0)
        {
            int randIndex = Random.Range(0, switches.Count);
            switches[randIndex].SetState(true);
        }
    }

    public void OnSwitchToggled(SwitchBehaviour sw)
    {
        if (sw.IsOn)
        {
            sw.SetState(false);
        }
    }

    // ---------------- Main Breaker ----------------
    void OnMainBreakerTapped()
    {
        if (!mainBreakerOn) return;

        SetMainBreakerState(false);

        if (timerLogic != null)
            timerLogic.StopTimer(); // 🔹 Stop timer when breaker is hit

        int onCount = 0;
        foreach (var sw in switches)
        {
            if (sw.IsOn) onCount++;
        }

        totalRemainingOn += onCount; // 🔹 accumulate across all rounds

        Debug.Log($"[SwitchLayer] Main breaker OFF. {onCount} switches were still ON (total so far: {totalRemainingOn}).");
        StartCoroutine(HandleRoundTransition());
    }

    void SetMainBreakerState(bool isOn)
    {
        mainBreakerOn = isOn;
        if (mainBreaker != null)
        {
            Vector2 pos = mainBreaker.anchoredPosition;
            pos.x = isOn ? mainBreakerOnOffsetX : mainBreakerOffOffsetX;
            mainBreaker.anchoredPosition = pos;
        }
    }

    // ---------------- EndGame / Transition ----------------
    IEnumerator HandleRoundTransition()
    {
        Vector2 startPosBreaker = breakerParent.anchoredPosition;
        Vector2 endPosBreaker = startPosBreaker + new Vector2(-slideDistance, 0);

        Vector2 startPosSwitch = mainSwitch != null ? mainSwitch.anchoredPosition : Vector2.zero;
        Vector2 endPosSwitch = mainSwitch != null ? mainSwitchHomePos + new Vector2(-slideDistance, 0) : Vector2.zero;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerp = t / slideDuration;

            breakerParent.anchoredPosition = Vector2.Lerp(startPosBreaker, endPosBreaker, lerp);

            if (mainSwitch != null)
                mainSwitch.anchoredPosition = Vector2.Lerp(startPosSwitch, endPosSwitch, lerp);

            yield return null;
        }

        if (currentRound < totalRounds)
        {
            breakerParent.anchoredPosition = endPosBreaker + new Vector2(slideDistance, 0);
            if (mainSwitch != null)
                mainSwitch.anchoredPosition = mainSwitchHomePos + new Vector2(slideDistance, 0);

            StartRound(firstRound: false);
        }
        else
        {
            EndGame();
        }
    }

    IEnumerator SlideIn(RectTransform rt, Vector2 startPos, Vector2 endPos)
    {
        Vector2 switchStartPos = mainSwitch != null ? mainSwitch.anchoredPosition : Vector2.zero;
        Vector2 switchEndPos = mainSwitch != null ? mainSwitchHomePos : Vector2.zero;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerp = t / slideDuration;

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, lerp);

            if (mainSwitch != null)
                mainSwitch.anchoredPosition = Vector2.Lerp(switchStartPos, switchEndPos, lerp);

            yield return null;
        }

        rt.anchoredPosition = endPos;
        if (mainSwitch != null)
            mainSwitch.anchoredPosition = mainSwitchHomePos;
    }

    // ---------------- EndGame / Persistence ----------------
    private void EndGame()
    {
        int totalPossible = totalRounds * 100;
        int penalty = totalRemainingOn * pointsPerSwitch; // 10 points each
        score = totalPossible - penalty;
        if (score < 0) score = 0;

        Debug.Log($"[SwitchLayer] Final Scoring -> Base: {totalPossible}, Penalty: {penalty} ({totalRemainingOn} switches ON), Final Score: {score}");

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Flood";
        string difficulty = "Easy";

        if (currentScene.StartsWith("FloodEasy"))
        {
            disaster = "Flood";
            difficulty = "Easy";
        }
        else if (currentScene.StartsWith("FloodHard"))
        {
            disaster = "Flood";
            difficulty = "Hard";
        }

    // Passing score is now 60% of max possible
    maxScore = totalPossible;
    passingScore = Mathf.RoundToInt(maxScore * 0.6f);

        GameResults.Score = score;
        GameResults.Passed = score >= passingScore;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = 1; // unique index for this minigame
        GameResults.Difficulty = difficulty;

        try
        {
            // 🔹 Save minigame progress (DBManager also updates disaster-level flags + unlocks)
            DBManager.SaveProgress(disaster, difficulty, GameResults.MiniGameIndex, GameResults.Passed);

            // Track the last scene played
            SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[SwitchLayer] DBManager or SceneTracker not available or threw exception: " + ex.Message);
        }

        Debug.Log($"[SwitchLayer] EndGame -> score: {score}, max: {maxScore}, passing: {passingScore}, passed: {GameResults.Passed}");

        // Load transition scene
        SceneManager.LoadScene("TransitionScene");
    }

    // ---------------- Helper SwitchBehaviour class ----------------
    public class SwitchBehaviour : MonoBehaviour
    {
        private Button button;
        private RectTransform rt;
        private SwitchLayer manager;
        private bool isRightColumn;

        public bool IsOn { get; private set; }

        public void Setup(bool isRight, SwitchLayer mgr)
        {
            isRightColumn = isRight;
            manager = mgr;
            rt = GetComponent<RectTransform>();

            button = gameObject.GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();

            button.onClick.AddListener(() => manager.OnSwitchToggled(this));
        }

        public void SetState(bool on)
        {
            IsOn = on;

            float currentY = rt.anchoredPosition.y;

            if (isRightColumn)
                rt.anchoredPosition = new Vector2(on ? manager.rightOnOffset : manager.rightOffOffset, currentY);
            else
                rt.anchoredPosition = new Vector2(on ? manager.leftOnOffset : manager.leftOffOffset, currentY);
        }
    }
}
