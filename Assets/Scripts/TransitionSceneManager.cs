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
    public Button nextButton;
    public Button retryButton;
    public TextMeshProUGUI retryButtonText;
    public Button mainMenuButton;   // ✅ Always available

    [Header("Animation References")]
    public Image animationImage;    // Assign a UI Image to display animation frames
    public Sprite[] passFrames;     // Fill this in Inspector
    public Sprite[] failFrames;     // Fill this in Inspector
    public float frameRate = 0.1f;  // Time per frame in seconds

    private string currentDisaster;
    private string currentDifficulty;

    private void Start()
    {
        // Get disaster/difficulty
        currentDisaster = SceneTracker.CurrentDisaster;
        currentDifficulty = SceneTracker.CurrentDifficulty;

        int score = GameResults.Score;
        bool passed = GameResults.Passed;

        // ✅ Update UI
        scoreText.text = $"Score: {score}";
        resultText.text = passed ? "You Passed!" : "Try Again";

        nextButton.interactable = passed;
        retryButtonText.text = passed ? "Play Again" : "Retry";

        Debug.Log($"Last mini-game: {SceneTracker.LastMinigameScene}");
        Debug.Log($"Current Disaster: {currentDisaster}, Difficulty: {currentDifficulty}");

        // ✅ If last scene was a quiz → hide Next button
        if (!string.IsNullOrEmpty(SceneTracker.LastMinigameScene) &&
            SceneTracker.LastMinigameScene.ToLower().Contains("quiz"))
        {
            nextButton.gameObject.SetActive(false);
        }
        else
        {
            nextButton.gameObject.SetActive(true);
        }

        // ✅ Main menu button always available
        mainMenuButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);

        // ✅ Play transition music depending on pass/fail
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTransitionMusic(passed);
        }

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

    // Continue / Next button
    public void OnContinue()
    {
        string nextScene = SceneTracker.GetNextScene(currentDisaster, currentDifficulty);

        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            Debug.LogWarning("No next mini-game found! Make sure your sequence is set up correctly.");
    }

    // Retry button
    public void OnRetryButton()
    {
        string lastScene = SceneTracker.LastMinigameScene;

        if (!string.IsNullOrEmpty(lastScene))
        {
            SceneManager.LoadScene(lastScene);
        }
        else
        {
            Debug.LogWarning("No last mini-game recorded! Cannot retry.");
        }
    }

    // ✅ Main Menu button
    public void OnMainMenuButton()
    {
        SceneManager.LoadScene("DisasterSelection"); // 🔹 replace with your actual main menu scene name
    }
}
