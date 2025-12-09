using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

public class MiniGameNumberDisplay : MonoBehaviour
{
[Header("UI References")]
public TMP_Text labelText;          // assign the child TMP text
public Image shadowBackground;      // assign the shadow background image

private void Start()
{
    UpdateLabel();
}

public void UpdateLabel()
{
    if (labelText == null)
    {
        Debug.LogError("[MiniGameNumberDisplay] No TMP Text assigned!");
        return;
    }

    string disaster = SceneTracker.CurrentDisaster;
    string difficulty = SceneTracker.CurrentDifficulty;
    string currentScene = SceneManager.GetActiveScene().name;

    // If this scene is a quiz, hide text and shadow background
    if (currentScene.ToLower().Contains("quiz"))
    {
        labelText.gameObject.SetActive(false);
        if (shadowBackground != null)
            shadowBackground.gameObject.SetActive(false);
        return;
    }

    // Ensure shadow background is visible for non-quiz scenes
    if (shadowBackground != null)
        shadowBackground.gameObject.SetActive(true);

    if (string.IsNullOrEmpty(disaster) || string.IsNullOrEmpty(difficulty))
    {
        labelText.text = "Mini-game";
        return;
    }

    // Load mini-game list from SceneTracker via reflection
    string key = $"{disaster}_{difficulty}";
    var dictField = typeof(SceneTracker).GetField("miniGameSequences",
        BindingFlags.NonPublic | BindingFlags.Static);
    var dict = dictField?.GetValue(null) as Dictionary<string, string[]>;

    if (dict == null || !dict.ContainsKey(key))
    {
        labelText.text = "Mini-game";
        return;
    }

    string[] miniGames = dict[key];

    // Find index of current scene in the sequence
    int index = System.Array.IndexOf(miniGames, currentScene);

    if (index < 0)
    {
        // Not found: fallback label
        labelText.text = "Mini-game";
        return;
    }

    // show 1-based index
    labelText.text = $"Mini-game {index + 1}";
}

}
