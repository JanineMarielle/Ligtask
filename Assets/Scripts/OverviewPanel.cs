using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class OverviewPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject overviewPanel;
    public TMP_Text disasterTitleText;
    public TMP_Text overviewText;
    public Button easyButton;
    public Button hardButton;
    public Button quizButton;
    public Button closeButton;

    [Header("Optional")]
    public Image disasterImageUI;
    public Image bgBlurImage;

    [Header("Background Animator")]
    public Animator bgAnimator; 

    [Header("Testing Options")]
    public bool useTestProgress = true;
    public bool testHardUnlocked = false;
    public bool testQuizCompleted = false;

    private string selectedDBName;

    void Start()
    {
        overviewPanel.SetActive(false);
        if (bgBlurImage != null)
            bgBlurImage.gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseOverviewPanel);

        // ✅ Apply saved background animation preference
        if (bgAnimator != null)
        {
            bool isAnimationOn = PlayerPrefs.GetInt("BGAnimation", 1) == 1;
            bgAnimator.enabled = isAnimationOn;
        }
    }

    public void OpenOverview(
        string displayName,
        string dbName,
        string overview,
        string easyScene,
        string hardScene,
        string quizScene,
        Sprite disasterImage = null
    )
    {
        selectedDBName = dbName;

        // Set UI
        disasterTitleText.text = displayName;
        overviewText.text = overview;

        if (disasterImageUI != null && disasterImage != null)
            disasterImageUI.sprite = disasterImage;

        overviewPanel.SetActive(true);
        if (bgBlurImage != null)
            bgBlurImage.gameObject.SetActive(true);

        // Set up buttons
        easyButton.onClick.RemoveAllListeners();
        hardButton.onClick.RemoveAllListeners();
        quizButton.onClick.RemoveAllListeners();

        // Check progress
        bool hardUnlocked = useTestProgress ? testHardUnlocked : CheckHardUnlocked(dbName);
        bool quizCompleted = useTestProgress ? testQuizCompleted : CheckQuizCompleted(dbName);

        easyButton.interactable = true;
        hardButton.interactable = hardUnlocked;
        quizButton.interactable = quizCompleted;

        easyButton.onClick.AddListener(() =>
        {
            SceneTracker.SetCurrentDisasterDifficulty(dbName, "Easy");
            SceneManager.LoadScene(easyScene);
        });

        if (hardUnlocked)
        {
            hardButton.onClick.AddListener(() =>
            {
                SceneTracker.SetCurrentDisasterDifficulty(dbName, "Hard");
                SceneManager.LoadScene(hardScene);
            });
        }

        if (quizCompleted)
        {
            quizButton.onClick.AddListener(() =>
            {
                SceneTracker.SetCurrentDisasterDifficulty(dbName, "Easy"); // or Hard depending on quiz
                SceneManager.LoadScene(quizScene);
            });
        }
    }

    private bool CheckHardUnlocked(string dbName)
    {
        var progress = DBManager.GetDisasterProgress(dbName);
        return progress != null && progress.HardUnlocked;
    }

    private bool CheckQuizCompleted(string dbName)
    {
        var progress = DBManager.GetDisasterProgress(dbName);
        return progress != null && progress.QuizCompleted;
    }

    public void CloseOverviewPanel()
    {
        overviewPanel.SetActive(false);
        if (bgBlurImage != null)
            bgBlurImage.gameObject.SetActive(false);
    }
}
