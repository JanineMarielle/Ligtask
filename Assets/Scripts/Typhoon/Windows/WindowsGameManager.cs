using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using SQLite4Unity3d;

public class WindowsGameManager : MonoBehaviour, IGameStarter
{
    [Header("Spawner References")]
    public PersonSpawner leftSpawner;
    public PersonSpawner rightSpawner;

    [Header("Game Settings")]
    [SerializeField] private int totalToSpawn = 10;
    [SerializeField] private int pointsPerPerson = 10; // Dynamic: 10 per saved person

    private int totalPeople;
    private int peopleSpawned;
    private int peopleSaved;
    private int activePeople;
    private bool gameEnded;
    private int score;
    private int maxScore;
    private int passingScore;

    private int personCounter = 0; // Unique ID tracker

    private void Awake()
    {
        if (leftSpawner != null)
        {
            leftSpawner.onPersonSpawned -= OnPersonSpawned;
            leftSpawner.onPersonSpawned += OnPersonSpawned;
            leftSpawner.enabled = true;
        }

        if (rightSpawner != null)
        {
            rightSpawner.onPersonSpawned -= OnPersonSpawned;
            rightSpawner.onPersonSpawned += OnPersonSpawned;
            rightSpawner.enabled = true;
        }

        Person.OnPersonDestroyed -= OnPersonDestroyedGlobal;
        Person.OnPersonDestroyed += OnPersonDestroyedGlobal;
    }

    private void OnDestroy()
    {
        Person.OnPersonDestroyed -= OnPersonDestroyedGlobal;

        if (leftSpawner != null)
            leftSpawner.onPersonSpawned -= OnPersonSpawned;
        if (rightSpawner != null)
            rightSpawner.onPersonSpawned -= OnPersonSpawned;
    }

    public void StartGame()
    {
        totalPeople = totalToSpawn;
        maxScore = totalPeople * pointsPerPerson;
        passingScore = Mathf.CeilToInt(maxScore * 0.7f); 

        peopleSpawned = 0;
        peopleSaved = 0;
        activePeople = 0;
        score = 0;
        gameEnded = false;
        personCounter = 0;

        StartCoroutine(SpawnRoutine());

        Debug.Log($"[WindowsGameManager] Starting game: totalPeople={totalPeople}, maxScore={maxScore}, passingScore={passingScore}");
    }

    private IEnumerator SpawnRoutine()
    {
        while (peopleSpawned < totalPeople)
        {
            PersonSpawner chosenSpawner = null;

            if (leftSpawner != null && rightSpawner != null)
                chosenSpawner = Random.value < 0.5f ? leftSpawner : rightSpawner;
            else if (leftSpawner != null)
                chosenSpawner = leftSpawner;
            else if (rightSpawner != null)
                chosenSpawner = rightSpawner;

            if (chosenSpawner != null)
            {
                chosenSpawner.SpawnOne(); // OnPersonSpawned increments
                float delay = Random.Range(chosenSpawner.MinDelay, chosenSpawner.MaxDelay);
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }
        }
    }

    private void OnPersonSpawned(Person person)
    {
        if (person == null) return;

        personCounter++;
        person.name = $"Person_{personCounter}";

        peopleSpawned++;
        activePeople++;
        Debug.Log($"[WindowsGameManager] Spawned: {person.name}, TotalSpawned={peopleSpawned}/{totalPeople}, Active={activePeople}");

        person.onSaved += OnPersonSaved;
        person.onFailed += OnPersonFailed;
    }

    private void OnPersonSaved(Person person)
    {
        if (person == null) return;

        peopleSaved++;
        score += pointsPerPerson;

        Debug.Log($"[WindowsGameManager] Saved: {person.name}, Score={score}/{maxScore}");
        CheckEnd();
    }

    private void OnPersonFailed(Person person)
    {
        if (person == null) return;

        Debug.Log($"[WindowsGameManager] Failed: {person.name}");
        CheckEnd();
    }

    private void OnPersonDestroyedGlobal(Person person)
    {
        if (person == null) return;

        person.onSaved -= OnPersonSaved;
        person.onFailed -= OnPersonFailed;

        activePeople = Mathf.Max(0, activePeople - 1);

        Debug.Log($"[WindowsGameManager] Destroyed: {person.name}, Active={activePeople}, Spawned={peopleSpawned}/{totalPeople}");
        CheckEnd();
    }

    private void CheckEnd()
    {
        if (gameEnded) return;

        if (peopleSpawned >= totalPeople && activePeople <= 0)
        {
            gameEnded = true;
            Debug.Log("[WindowsGameManager] All people processed. Ending game...");
            EndGame();
        }
    }

    private void EndGame()
    {
        score = Mathf.Clamp(score, 0, maxScore);

        string currentScene = SceneManager.GetActiveScene().name;
        string disaster = "Typhoon";
        string difficulty = "Easy";
        int miniGameIndex = 1;

        // Determine difficulty based on scene name
        if (currentScene.Contains("Hard"))
            difficulty = "Hard";

        if (currentScene.StartsWith("TyphoonEasy") || currentScene.StartsWith("TyphoonHard"))
        {
            string numberPart = new string(
                currentScene.ToCharArray(
                    currentScene.IndexOf(difficulty) + difficulty.Length,
                    currentScene.Length - (currentScene.IndexOf(difficulty) + difficulty.Length)
                )
            );
            int.TryParse(numberPart, out miniGameIndex);
        }

        bool passed = score >= passingScore;

        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = disaster;
        GameResults.MiniGameIndex = miniGameIndex;
        GameResults.Difficulty = difficulty;

        DBManager.SaveProgress(disaster, difficulty, miniGameIndex, passed);

        SceneTracker.SetCurrentMiniGame(disaster, difficulty, currentScene);

        SceneManager.LoadScene("TransitionScene");

    }

}
