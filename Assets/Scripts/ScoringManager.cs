using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private int currentScore = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddScore(int points)
    {
        currentScore += points;
    }

    public int GetScore()
    {
        return currentScore;
    }

    public void ResetScore()
    {
        currentScore = 0;
    }
}
