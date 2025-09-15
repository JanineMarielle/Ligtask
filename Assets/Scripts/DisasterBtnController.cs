using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DisasterBtnController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject disasterButton;        
    public GameObject difficultyButtons;     
    public GameObject lockedOverlay;         

    [Header("Disaster Info")]
    public string disasterName;             
    private DisasterProgress progress;      

    [Header("Testing Options")]
    public bool useTestProgress = true;     
    public bool testIsUnlocked = false;     
    public bool testHardUnlocked = false;  

    private Button disasterBtnComponent;

    private void Awake()
    {
        if (disasterButton == null)
        {
            Debug.LogError($"[ERROR] disasterButton reference is missing for {disasterName}");
        }
        else
        {
            disasterBtnComponent = disasterButton.GetComponent<Button>();
            if (disasterBtnComponent == null)
            {
                Debug.LogError($"[ERROR] No Button component found on disasterButton for {disasterName}");
            }
            else
            {
                disasterBtnComponent.onClick.AddListener(() =>
                {
                    Debug.Log($"[CLICK] Disaster button '{disasterName}' clicked.");
                    OnDisasterButtonPressed();
                });
            }
        }
    }

    private void Start()
    {
        Debug.Log($"[START] Initializing disaster: {disasterName}");

        if (useTestProgress)
        {
            progress = new DisasterProgress
            {
                IsUnlocked = testIsUnlocked,
                HardUnlocked = testHardUnlocked
            };
            Debug.Log($"[TEST MODE] Created progress: Unlocked={progress.IsUnlocked}, Hard={progress.HardUnlocked}");
        }
        else
        {
            progress = DBManager.GetDisasterProgress(disasterName);
            Debug.Log($"[DB MODE] Loaded progress from DB: Unlocked={progress.IsUnlocked}, Hard={progress.HardUnlocked}");
        }

        if (progress == null)
        {
            Debug.LogError($"[ERROR] No DB entry found for {disasterName}");
            return;
        }

        if (disasterName.Trim().ToLower() == "typhoon")
        {
            progress.IsUnlocked = true;
            progress.HardUnlocked = true;
            Debug.Log("[TYHOON OVERRIDE] Forcing unlocked state");
        }

        if (lockedOverlay != null)
        {
            bool shouldShow = !progress.IsUnlocked;
            lockedOverlay.SetActive(shouldShow);
            Debug.Log($"[OVERLAY] Setting active={shouldShow} for {disasterName}");
        }
        else
        {
            Debug.LogWarning($"[OVERLAY] No overlay object assigned for {disasterName}");
        }

        if (disasterBtnComponent != null)
        {
            disasterBtnComponent.interactable = progress.IsUnlocked;
            Debug.Log($"[BUTTON] Interactable={disasterBtnComponent.interactable} for {disasterName}");
        }

        ShowDisasterButton();
    }

    public void OnDisasterButtonPressed()
    {
        Debug.Log($"[ACTION] OnDisasterButtonPressed called for {disasterName}");

        if (progress == null)
        {
            Debug.LogError($"[ERROR] progress is null for {disasterName}, cannot process button press");
            return;
        }

        if (!progress.IsUnlocked)
        {
            Debug.Log($"[LOCKED] Disaster '{disasterName}' is locked");
            return;
        }

        if (progress.HardUnlocked)
        {
            Debug.Log($"[SHOW] Showing difficulty buttons for '{disasterName}'");
            if (disasterButton != null) disasterButton.SetActive(false);
            if (difficultyButtons != null) difficultyButtons.SetActive(true);
            else Debug.LogWarning($"[WARNING] difficultyButtons GameObject is null for {disasterName}");
        }
        else
        {
            Debug.Log($"[LOAD] Loading easy level for '{disasterName}'");
            LoadEasyLevel();
        }
    }

    public void LoadEasyLevel()
    {
        Debug.Log($"[LOAD] Loading scene: {disasterName}Easy");

        SceneTracker.SetCurrentDisasterDifficulty(disasterName, "Easy");

        SceneManager.LoadScene(disasterName + "Easy");
    }

    public void LoadHardLevel()
    {
        Debug.Log($"[LOAD] Loading scene: {disasterName}Hard");

        SceneTracker.SetCurrentDisasterDifficulty(disasterName, "Hard");

        SceneManager.LoadScene(disasterName + "Hard");
    }

    private void ShowDisasterButton()
    {
        if (disasterButton != null) disasterButton.SetActive(true);
        else Debug.LogWarning($"[WARNING] disasterButton GameObject is null for {disasterName}");
        if (difficultyButtons != null) difficultyButtons.SetActive(false);
        else Debug.LogWarning($"[WARNING] difficultyButtons GameObject is null for {disasterName}");
    }
}
