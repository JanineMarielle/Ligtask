using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class MiniGameMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button[] miniGameButtons; // regular minigames
    public Button quizButton;        // assign quiz button separately

    private void Start()
    {
        SetupMenu();
    }

    public void GoToQuiz()
    {
        string disaster = SceneTracker.CurrentDisaster;
        string difficulty = SceneTracker.CurrentDifficulty;

        if (string.IsNullOrEmpty(disaster) || string.IsNullOrEmpty(difficulty))
        {
            Debug.LogError("[MiniGameMenu] Cannot go to quiz, disaster/difficulty missing!");
            return;
        }

        // Get the quiz scene from SceneTracker instead of hardcoding
        string key = $"{disaster}_{difficulty}";
        var dictField = typeof(SceneTracker).GetField("miniGameSequences",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var dict = dictField.GetValue(null) as Dictionary<string, string[]>;
        if (dict == null || !dict.ContainsKey(key))
        {
            Debug.LogError("[MiniGameMenu] Sequence not found for " + key);
            return;
        }

        string quizScene = dict[key].Last();
        Debug.Log($"[MiniGameMenu] Loading quiz scene: {quizScene}");
        SceneManager.LoadScene(quizScene);
    }

    public void SetupMenu()
    {
        string disaster = SceneTracker.CurrentDisaster;
        string difficulty = SceneTracker.CurrentDifficulty;
        string currentScene = SceneTracker.LastMinigameScene;

        if (string.IsNullOrEmpty(disaster) || string.IsNullOrEmpty(difficulty))
        {
            Debug.LogError("[MiniGameMenu] No current disaster/difficulty found!");
            return;
        }

        // Get mini-game sequence
        string key = $"{disaster}_{difficulty}";
        var dictField = typeof(SceneTracker).GetField("miniGameSequences",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var dict = dictField.GetValue(null) as Dictionary<string, string[]>;
        if (dict == null || !dict.ContainsKey(key))
        {
            Debug.LogError("[MiniGameMenu] No mini-games found for " + key);
            return;
        }

        string[] miniGames = dict[key];

        var progressList = DBManager.GetMiniGameProgress(disaster, difficulty);
        var disasterProgress = DBManager.GetDisasterProgress(disaster);

        // Regular minigames (exclude currentScene + quiz)
        int buttonIndex = 0;
        for (int i = 0; i < miniGames.Length - 1; i++) // exclude last (quiz)
        {
            string targetScene = miniGames[i];

            if (targetScene == currentScene)
                continue;

            if (buttonIndex >= miniGameButtons.Length)
                break;

            int gameNumber = i + 1;
            miniGameButtons[buttonIndex].gameObject.SetActive(true);

            TMP_Text label = miniGameButtons[buttonIndex].GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"Game {gameNumber}";

            miniGameButtons[buttonIndex].onClick.RemoveAllListeners();

            bool interactable = false;

            if (i == 0)
            {
                if (difficulty == "Easy")
                    interactable = true;
                else if (difficulty == "Hard")
                    interactable = disasterProgress != null && disasterProgress.QuizCompleted;
            }
            else
            {
                var prevGameProgress = progressList.FirstOrDefault(m => m.MiniGameIndex == i);
                if (prevGameProgress != null && prevGameProgress.Passed)
                    interactable = true;

                var currentGameProgress = progressList.FirstOrDefault(m => m.MiniGameIndex == i + 1);
                if (currentGameProgress != null && targetScene == currentScene)
                    interactable = true;
            }

            miniGameButtons[buttonIndex].interactable = interactable;

            if (label != null)
            {
                Color c = label.color;
                c.a = interactable ? 1f : 0.5f;
                label.color = c;
            }

            miniGameButtons[buttonIndex].onClick.AddListener(() =>
            {
                SceneTracker.SetCurrentMiniGame(disaster, difficulty, targetScene);
                SceneManager.LoadScene(targetScene);
            });

            buttonIndex++;
        }

        // Hide unused buttons
        for (; buttonIndex < miniGameButtons.Length; buttonIndex++)
            miniGameButtons[buttonIndex].gameObject.SetActive(false);

        // Quiz button
        if (quizButton != null)
        {
            string quizScene = miniGames.Last();

            quizButton.onClick.RemoveAllListeners();
            quizButton.onClick.AddListener(GoToQuiz);

            int lastMiniIndex = miniGames.Length - 2; // index before quiz
            var lastGameProgress = progressList.FirstOrDefault(m => m.MiniGameIndex == lastMiniIndex + 1);
            bool quizInteractable = lastGameProgress != null && lastGameProgress.Passed;

            quizButton.interactable = quizInteractable;

            TMP_Text quizLabel = quizButton.GetComponentInChildren<TMP_Text>();
            if (quizLabel != null)
            {
                Color c = quizLabel.color;
                c.a = quizInteractable ? 1f : 0.5f;
                quizLabel.color = c;
            }
        }
    }
}
