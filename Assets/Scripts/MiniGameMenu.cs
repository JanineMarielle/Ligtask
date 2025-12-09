using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Reflection;

public class MiniGameMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button[] miniGameButtons; // first button = Restart

    private void Start()
    {
        SetupMenu();
    }

    public void SetupMenu()
    {
        string disaster = SceneTracker.CurrentDisaster;
        string difficulty = SceneTracker.CurrentDifficulty;

        if (string.IsNullOrEmpty(disaster) || string.IsNullOrEmpty(difficulty))
        {
            Debug.LogError("[MiniGameMenu] No current disaster/difficulty found!");
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        bool isQuizScene = currentScene.ToLower().Contains("quiz");

        // --- Access mini-game sequences through reflection ---
        string key = $"{disaster}_{difficulty}";
        var dictField = typeof(SceneTracker).GetField("miniGameSequences",
            BindingFlags.NonPublic | BindingFlags.Static);
        var dict = dictField?.GetValue(null) as Dictionary<string, string[]>;

        if (dict == null || !dict.ContainsKey(key))
        {
            Debug.LogError("[MiniGameMenu] No mini-games found for " + key);
            return;
        }

        string[] miniGames = dict[key];
        var progressList = DBManager.GetMiniGameProgress(disaster, difficulty);

        // Count non-quiz scenes for button limit
        int nonQuizCount = miniGames.Count(scene => !scene.ToLower().Contains("quiz"));

        int buttonIndex = 0;

        // ------------------------------
        // FIRST BUTTON = RESTART
        // ------------------------------
        Button restartButton = miniGameButtons[buttonIndex];
        restartButton.gameObject.SetActive(true);

        TMP_Text restartLabel = restartButton.GetComponentInChildren<TMP_Text>();
        if (restartLabel != null)
            restartLabel.text = isQuizScene ? "Restart Quiz" : "Restart";

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() => SceneManager.LoadScene(currentScene));
        restartButton.interactable = true;

        buttonIndex++;

        // ------------------------------
        // QUIZ SCENE → ONLY Restart + Main Menu
        // Hide all other buttons
        // ------------------------------
        if (isQuizScene)
        {
            for (; buttonIndex < miniGameButtons.Length; buttonIndex++)
                miniGameButtons[buttonIndex].gameObject.SetActive(false);

            return;
        }

        // ------------------------------
        // REGULAR MINI-GAMES (No Quiz Buttons Allowed)
        // ------------------------------
        int shownButtons = 0;
        for (int i = 0; i < miniGames.Length; i++)
        {
            string targetScene = miniGames[i];

            // Skip current scene (restart already covers it)
            if (targetScene == currentScene)
                continue;

            // Skip quiz scenes in normal mode
            if (targetScene.ToLower().Contains("quiz"))
                continue;

            if (buttonIndex >= miniGameButtons.Length || shownButtons >= nonQuizCount)
                break;

            Button miniButton = miniGameButtons[buttonIndex];
            miniButton.gameObject.SetActive(true);

            int sceneIndex = i;
            string capturedTarget = targetScene;

            // LABEL
            TMP_Text label = miniButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"Mini-game {sceneIndex + 1}";

            // INTERACTABILITY
            bool interactable = sceneIndex == 0 ||
                                progressList.Any(p => p.MiniGameIndex == sceneIndex && p.Passed);

            miniButton.interactable = interactable;

            if (label != null)
            {
                Color c = label.color;
                c.a = interactable ? 1f : 0.5f;
                label.color = c;
            }

            // BUTTON ACTION
            miniButton.onClick.RemoveAllListeners();
            miniButton.onClick.AddListener(() =>
            {
                SceneTracker.SetCurrentMiniGame(disaster, difficulty, capturedTarget);
                SceneManager.LoadScene(capturedTarget);
            });

            buttonIndex++;
            shownButtons++;
        }

        // ------------------------------
        // HIDE UNUSED BUTTONS
        // ------------------------------
        for (; buttonIndex < miniGameButtons.Length; buttonIndex++)
            miniGameButtons[buttonIndex].gameObject.SetActive(false);
    }
}
