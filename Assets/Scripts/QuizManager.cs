using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour, IGameStarter
{
    [Header("Quiz Data")]
    public Quiz currentQuiz;

    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text questionCounterText;   // 👈 New text for "Question X of Y"
    public Button[] optionButtons;

    [Header("Button Sprites")]
    public Sprite defaultSprite;
    public Sprite correctSprite;
    public Sprite incorrectSprite;

    private List<Questions> quizQuestions = new List<Questions>();
    private int currentQuestionIndex = 0;
    private int score = 0;
    private int maxScore = 0;
    private bool isRunning = false;
    private bool isAnswerSelected = false;

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

        isAnswerSelected = false;
        ResetButtonSprites();

        Questions q = quizQuestions[currentQuestionIndex];
        questionText.text = q.questionText;

        // 🧮 Update "Question X of Y" text
        if (questionCounterText != null)
        {
            questionCounterText.text = $"{currentQuestionIndex + 1}";
        }

        // Randomize option order
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
                optionButtons[i].onClick.AddListener(() => SubmitAnswer(choiceIndex, q.correctAnswerIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void SubmitAnswer(int selectedIndex, int correctIndex)
    {
        if (!isRunning || isAnswerSelected) return;
        isAnswerSelected = true;

        Questions q = quizQuestions[currentQuestionIndex];
        bool isCorrect = selectedIndex == correctIndex;

        // Update button visuals
        for (int i = 0; i < optionButtons.Length; i++)
        {
            Image btnImage = optionButtons[i].GetComponent<Image>();
            TMP_Text btnText = optionButtons[i].GetComponentInChildren<TMP_Text>();

            if (btnText == null || btnImage == null) continue;

            if (btnText.text == q.options[selectedIndex])
                btnImage.sprite = isCorrect ? correctSprite : incorrectSprite;

            if (btnText.text == q.options[correctIndex])
                btnImage.sprite = correctSprite;

            optionButtons[i].onClick.RemoveAllListeners();
        }

        if (isCorrect)
        {
            score += 10;
            Debug.Log("✅ Correct!");
        }
        else
        {
            Debug.Log("❌ Wrong!");
        }

        Invoke(nameof(NextQuestion), 1f);
    }

    private void NextQuestion()
    {
        currentQuestionIndex++;
        ShowQuestion();
    }

    private void ResetButtonSprites()
    {
        foreach (Button btn in optionButtons)
        {
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null)
                btnImage.sprite = defaultSprite;
        }
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
        string disaster = SceneTracker.CurrentDisaster ?? "Unknown";
        string difficulty = SceneTracker.CurrentDifficulty ?? "Easy";
        string currentScene = SceneManager.GetActiveScene().name;

        int passingScore = Mathf.RoundToInt(maxScore * 0.7f);
        bool passed = score >= passingScore;

        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = disaster;
        GameResults.Difficulty = difficulty;
        GameResults.MiniGameIndex = -1;

        DBManager.SaveProgress(disaster, "Quiz", -1, passed);
        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

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
