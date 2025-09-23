using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour, IGameStarter
{
    [Header("Quiz Data")]
    public Quiz currentQuiz;   // Assign in Inspector

    [Header("UI References")]
    public TMP_Text questionText;      
    public Button[] optionButtons;     

    [Header("Managers")]
    public FeedbackManager feedbackManager;

    private List<Questions> quizQuestions = new List<Questions>();
    private int currentQuestionIndex = 0;
    private int score = 0;
    private int maxScore = 0;
    private bool isRunning = false;

    private void Awake()
    {
        if (feedbackManager == null)
        {
            feedbackManager = FindObjectOfType<FeedbackManager>();
            if (feedbackManager != null)
                Debug.Log("[QuizManager] FeedbackManager found automatically!");
            else
                Debug.LogWarning("[QuizManager] FeedbackManager not found in scene. Feedback will be disabled.");
        }
    }

    public void StartGame()
    {
        if (currentQuiz == null || currentQuiz.questions.Length == 0)
        {
            Debug.LogWarning("⚠️ No quiz assigned or quiz has no questions!");
            return;
        }

        isRunning = true;
        score = 0;

        quizQuestions = new List<Questions>(currentQuiz.questions);
        ShuffleList(quizQuestions);

        if (quizQuestions.Count > 10)
            quizQuestions = quizQuestions.GetRange(0, 10);

        maxScore = quizQuestions.Count * 10;
        currentQuestionIndex = 0;

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (!isRunning) return;

        if (currentQuestionIndex >= quizQuestions.Count)
        {
            EndGame();
            return;
        }

        Questions q = quizQuestions[currentQuestionIndex];
        questionText.text = q.questionText;

        List<int> optionOrder = new List<int>();
        for (int i = 0; i < q.options.Length; i++) optionOrder.Add(i);
        ShuffleList(optionOrder);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);

                TMP_Text btnText = optionButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                    btnText.text = q.options[optionOrder[i]];

                int choiceIndex = optionOrder[i];
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SubmitAnswer(choiceIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void SubmitAnswer(int selectedIndex)
    {
        if (!isRunning) return;

        Questions q = quizQuestions[currentQuestionIndex];

        if (selectedIndex == q.correctAnswerIndex)
        {
            score += 10;
            Debug.Log("✅ Correct!");
            if (feedbackManager != null) feedbackManager.ShowPositive();
        }
        else
        {
            Debug.Log("❌ Wrong!");
            if (feedbackManager != null) feedbackManager.ShowNegative();
        }

        currentQuestionIndex++;
        ShowQuestion();
    }

    public void EndGame()
    {
        if (!isRunning) return;
        isRunning = false;

        Debug.Log($"🏆 Quiz finished! Score: {score}/{maxScore}");
        SaveAndTransition();
    }

    private void SaveAndTransition()
    {
        // Get current context from SceneTracker
        string disaster = SceneTracker.CurrentDisaster ?? "Unknown";
        string difficulty = SceneTracker.CurrentDifficulty ?? "Easy"; 
        string currentScene = SceneManager.GetActiveScene().name;

        // Passing score: 70% for quizzes
        int passingScore = Mathf.RoundToInt(maxScore * 0.7f);
        bool passed = score >= passingScore;

        // Save results to GameResults
        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = disaster;
        GameResults.Difficulty = difficulty;
        GameResults.MiniGameIndex = -1; // quiz is terminal, no index

        // Save to DB (force difficulty Easy for consistency in menu if needed)
        DBManager.SaveProgress(disaster, "Easy", -1, passed);

        // Update SceneTracker with last scene = current quiz
        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

        // Transition
        SceneManager.LoadScene("TransitionScene");
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }
}
