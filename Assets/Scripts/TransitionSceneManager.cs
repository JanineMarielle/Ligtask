using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionSceneManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI messageText;   // ✅ For short messages
    public Button nextButton;
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Animation References")]
    public Image animationImage;
    public Sprite[] passFrames;
    public Sprite[] failFrames;
    public float frameRate = 0.1f;

    private string currentDisaster;
    private string currentDifficulty;

    private void Start()
    {
        currentDisaster = SceneTracker.CurrentDisaster;
        currentDifficulty = SceneTracker.CurrentDifficulty;

        int score = GameResults.Score;
        bool passed = GameResults.Passed;

        scoreText.text = $"{score}";

        bool isQuiz = !string.IsNullOrEmpty(SceneTracker.LastMinigameScene) &&
                      SceneTracker.LastMinigameScene.ToLower().Contains("quiz");

        // ✅ Update result text
        if (isQuiz)
            resultText.text = passed ? "Quiz Passed" : "Fail";
        else
            resultText.text = passed ? "Success" : "Fail";

        // ✅ Disable Next button if player failed
        nextButton.interactable = passed;

        Debug.Log($"Last mini-game: {SceneTracker.LastMinigameScene}");
        Debug.Log($"Current Disaster: {currentDisaster}, Difficulty: {currentDifficulty}");

        // ✅ Determine if this is the first time passing the quiz
        bool firstTimePassedQuiz = false;
        if (isQuiz)
        {
            var progress = DBManager.GetDisasterProgress(currentDisaster);
            if (progress != null)
            {
                // If the quiz wasn’t previously passed, mark as first time
                firstTimePassedQuiz = !progress.QuizCompleted;
            }
        }

        // ✅ Display appropriate short message
        if (isQuiz)
        {
            if (passed)
            {
                // Only show unlock message if first time passing quiz
                messageText.text = firstTimePassedQuiz
                    ? "You have unlocked a hard mode and a new minigame!"
                    : "";
            }
            else
            {
                messageText.text = "Don't give up, try again!";
            }
        }
        else
        {
            messageText.text = passed ? "" : "Score higher points to unlock the next minigame.";
        }

        // ✅ Handle Next button visibility
        nextButton.gameObject.SetActive(!isQuiz);
        mainMenuButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);

        // ✅ Play transition music
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTransitionMusic(passed);

        // ✅ Play pass/fail animation
        StartCoroutine(PlayAnimation(passed ? passFrames : failFrames));
    }

    private IEnumerator PlayAnimation(Sprite[] frames)
    {
        int index = 0;
        while (true)
        {
            if (frames.Length > 0)
            {
                animationImage.sprite = frames[index];
                index = (index + 1) % frames.Length;
            }
            yield return new WaitForSeconds(frameRate);
        }
    }

    // ✅ Continue / Next button
    public void OnContinue()
    {
        string nextScene = SceneTracker.GetNextScene(currentDisaster, currentDifficulty);

        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            Debug.LogWarning("No next mini-game found! Make sure your sequence is set up correctly.");
    }

    // ✅ Retry button
    public void OnRetryButton()
    {
        string lastScene = SceneTracker.LastMinigameScene;

        if (!string.IsNullOrEmpty(lastScene))
            SceneManager.LoadScene(lastScene);
        else
            Debug.LogWarning("No last mini-game recorded! Cannot retry.");
    }

    // ✅ Main Menu button
    public void OnMainMenuButton()
    {
        SceneManager.LoadScene("DisasterSelection"); // 🔹 replace with your actual main menu scene name
    }
}
