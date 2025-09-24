using System.Collections.Generic;
using UnityEngine;

public static class SceneTracker
{
    public static string LastMinigameScene { get; private set; }

    private static Dictionary<string, int> currentIndices = new Dictionary<string, int>();

    public static string CurrentDisaster { get; private set; }
    public static string CurrentDifficulty { get; private set; }

    private static Dictionary<string, string[]> miniGameSequences = new Dictionary<string, string[]>()
    {
        { "Typhoon_Easy", new string[] { "TyphoonEasy", "TyphoonEasy2", "TyphoonEasy3", "TyphoonEasy4", "TyphoonEasy5", "TyphoonQuiz" } },
        { "Typhoon_Hard", new string[] { "TyphoonHard", "TyphoonHard2", "TyphoonHard3", "TyphoonHard4", "TyphoonHard5" } },

        { "Flood_Easy", new string[] { "FloodEasy", "FloodEasy2", "FloodEasy3", "FloodEasy5", "FloodQuiz" } },
        { "Flood_Hard", new string[] { "FloodHard", "FloodHard2", "FloodHard3", "FloodHard5", "FloodQuiz" } },

        { "Earthquake_Easy", new string[] { "EarthquakeEasy", "EarthquakeEasy2", "EarthquakeEasy3", "EarthquakeQuiz" } },
        { "Earthquake_Hard", new string[] { "EarthquakeHard", "EarthquakeHard2", "EarthquakeHard3", "EarthquakeQuiz" } },

        { "Landslide_Easy", new string[] { "LandslideEasy", "LandslideEasy2", "LandslideEasy3", "LandslideQuiz" } },
        { "Landslide_Hard", new string[] { "LandslideHard", "LandslideHard2", "LandslideHard3", "LandslideQuiz" } },

        { "Volcano_Easy", new string[] { "VolcanoEasy", "VolcanoEasy2", "VolcanoEasy3", "VolcanoQuiz" } },
        { "Volcano_Hard", new string[] { "VolcanoHard", "VolcanoHard2", "VolcanoHard3", "VolcanoQuiz" } },
    };

    private static string GetKey(string disaster, string difficulty)
    {
        if (string.IsNullOrEmpty(disaster) || string.IsNullOrEmpty(difficulty))
        {
            Debug.LogError("SceneTracker: disaster or difficulty is empty!");
            return null;
        }
        return $"{disaster}_{difficulty}";
    }

    // -------------------------
    // Mini-game sequence navigation
    // -------------------------
    public static string GetNextScene(string disaster, string difficulty)
    {
        string key = GetKey(disaster, difficulty);
        if (string.IsNullOrEmpty(key) || !miniGameSequences.ContainsKey(key)) return null;

        if (!currentIndices.ContainsKey(key))
            currentIndices[key] = 0;

        int index = currentIndices[key];
        string[] sequence = miniGameSequences[key];

        if (index < sequence.Length - 1)
        {
            index++;
            currentIndices[key] = index;

            SetCurrentMiniGame(disaster, difficulty, sequence[index]);
            return sequence[index];
        }
        else
        {
            return "LevelCompleteScene";
        }
    }

    public static string GetCurrentScene(string disaster, string difficulty)
    {
        string key = GetKey(disaster, difficulty);
        if (string.IsNullOrEmpty(key) || !miniGameSequences.ContainsKey(key)) return null;

        if (!currentIndices.ContainsKey(key))
            currentIndices[key] = 0;

        int index = currentIndices[key];
        string[] sequence = miniGameSequences[key];

        if (index >= 0 && index < sequence.Length)
        {
            SetCurrentMiniGame(disaster, difficulty, sequence[index]);
            return sequence[index];
        }
        return null;
    }

    // -------------------------
    // Set current mini-game (with scene name)
    // -------------------------
    public static void SetCurrentMiniGame(string disaster, string difficulty, string sceneName)
    {
        CurrentDisaster = disaster;
        CurrentDifficulty = difficulty;
        LastMinigameScene = sceneName;

        string key = GetKey(disaster, difficulty);
        if (!string.IsNullOrEmpty(key) && miniGameSequences.ContainsKey(key))
        {
            string[] sequence = miniGameSequences[key];
            int index = System.Array.IndexOf(sequence, sceneName);

            if (index != -1)
                currentIndices[key] = index;
        }
    }

    // -------------------------
    // NEW: Set disaster & difficulty without specifying scene
    // -------------------------
    public static void SetCurrentDisasterDifficulty(string disaster, string difficulty)
    {
        CurrentDisaster = disaster;
        CurrentDifficulty = difficulty;

        string key = GetKey(disaster, difficulty);
        if (!string.IsNullOrEmpty(key) && miniGameSequences.ContainsKey(key))
        {
            // Default to first scene in sequence
            LastMinigameScene = miniGameSequences[key][0];
            currentIndices[key] = 0;
        }
        else
        {
            LastMinigameScene = null;
        }
    }

    // -------------------------
    // Reset everything
    // -------------------------
    public static void ResetAllSequences()
    {
        currentIndices.Clear();
        LastMinigameScene = null;
        CurrentDisaster = null;
        CurrentDifficulty = null;
    }

    // -------------------------
    // Helper for MiniGameMenu
    // -------------------------
    public static IEnumerable<string> GetAllKeys()
    {
        if (miniGameSequences == null)
            return new List<string>(); // return empty to avoid null errors

        return miniGameSequences.Keys;
    }
}
