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
    public TextMeshProUGUI messageText;  
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

        if (isQuiz)
            resultText.text = passed ? "Quiz Passed" : "Fail";
        else
            resultText.text = passed ? "Success" : "Fail";

        nextButton.interactable = passed;

        Debug.Log($"Last mini-game: {SceneTracker.LastMinigameScene}");
        Debug.Log($"Current Disaster: {currentDisaster}, Difficulty: {currentDifficulty}");

        bool firstTimePassedQuiz = false;
        if (isQuiz)
        {
            var progress = DBManager.GetDisasterProgress(currentDisaster);
            if (progress != null)
            {
                firstTimePassedQuiz = !progress.QuizCompleted;
            }
        }

        if (isQuiz)
        {
            if (passed)
            {
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

        nextButton.gameObject.SetActive(!isQuiz);
        mainMenuButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);

        string nextScene = SceneTracker.PeekNextScene(currentDisaster, currentDifficulty);

        if (string.IsNullOrEmpty(nextScene))
        {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false); 
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTransitionMusic(passed);

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

    public void OnContinue()
    {
        string nextScene = SceneTracker.GetNextScene(currentDisaster, currentDifficulty);

        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            Debug.LogWarning("No next mini-game found! Make sure your sequence is set up correctly.");
    }

    public void OnRetryButton()
    {
        string lastScene = SceneTracker.LastMinigameScene;

        if (!string.IsNullOrEmpty(lastScene))
            SceneManager.LoadScene(lastScene);
        else
            Debug.LogWarning("No last mini-game recorded! Cannot retry.");
    }

    public void OnMainMenuButton()
    {
        SceneManager.LoadScene("DisasterSelection"); 
    }
}
