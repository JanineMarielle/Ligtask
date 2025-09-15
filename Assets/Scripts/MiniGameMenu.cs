using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class MiniGameMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button[] miniGameButtons;

    private void Start()
    {
        SetupMenu();
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

        string key = $"{disaster}_{difficulty}";
        var sequences = SceneTracker.GetAllKeys().Contains(key)
            ? SceneTracker.GetAllKeys().FirstOrDefault(k => k == key)
            : null;

        if (sequences == null)
        {
            Debug.LogError("[MiniGameMenu] No sequence found for key: " + key);
            return;
        }

        string[] miniGames = null;
        var trackerType = typeof(SceneTracker);
        var dictField = trackerType.GetField("miniGameSequences",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var dict = dictField.GetValue(null) as Dictionary<string, string[]>;
        if (dict != null && dict.ContainsKey(key))
            miniGames = dict[key];

        if (miniGames == null || miniGames.Length == 0)
        {
            Debug.LogError("[MiniGameMenu] No mini-games found for " + key);
            return;
        }

        var otherGames = miniGames
            .Select((scene, index) => new { scene, index })
            .Where(x => x.scene != currentScene)
            .ToList();

        for (int i = 0; i < miniGameButtons.Length; i++)
        {
            if (i < otherGames.Count)
            {
                string targetScene = otherGames[i].scene;
                int gameNumber = otherGames[i].index + 1;

                miniGameButtons[i].gameObject.SetActive(true);

                TMP_Text label = miniGameButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"Game {gameNumber}";

                miniGameButtons[i].onClick.RemoveAllListeners();

                // ✅ Always allow switching
                miniGameButtons[i].interactable = true;
                miniGameButtons[i].onClick.AddListener(() =>
                {
                    Debug.Log("[MiniGameMenu] Loading scene: " + targetScene);
                    SceneTracker.SetCurrentMiniGame(disaster, difficulty, targetScene);
                    SceneManager.LoadScene(targetScene);
                });
            }
            else
            {
                miniGameButtons[i].gameObject.SetActive(false);
            }
        }
    }
}
