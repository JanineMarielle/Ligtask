using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using SQLite4Unity3d;

public class DBManager : MonoBehaviour
{
    private static SQLiteConnection db;
    private static string dbPath;
    private static bool initialized = false;

    public static void Init()
    {
        if (initialized) return;

        dbPath = Path.Combine(Application.persistentDataPath, "ligtask.db");

        // Open or create DB
        db = new SQLiteConnection(dbPath);

        // Create tables if they don’t exist
        db.CreateTable<DisasterProgress>();
        db.CreateTable<MiniGameProgress>();

        // Seed default disasters if first time running
        if (db.Table<DisasterProgress>().Count() == 0)
        {
            Debug.Log("[DBManager] Seeding default disaster data...");
            SeedInitialData();
        }

        initialized = true;
    }

    private void Awake()
    {
        Init();
    }

    private static void SeedInitialData()
    {
        db.Insert(new DisasterProgress { DisasterName = "Typhoon", IsUnlocked = true });
        db.Insert(new DisasterProgress { DisasterName = "Earthquake" });
        db.Insert(new DisasterProgress { DisasterName = "Flood" });
        db.Insert(new DisasterProgress { DisasterName = "Landslide" });
        db.Insert(new DisasterProgress { DisasterName = "Volcano" });
    }

    // -------------------------
    // PROGRESS METHODS
    // -------------------------
    public static DisasterProgress GetDisasterProgress(string disasterName)
    {
        if (db == null) Init();
        return db.Table<DisasterProgress>().FirstOrDefault(x => x.DisasterName == disasterName);
    }

    public static void SaveProgress(string disasterName, string difficulty, int miniGameIndex, int score)
    {
        if (db == null) Init();

        bool passed = false;

        // 🔹 Apply score rules
        if (difficulty == "Easy" && score >= 60) passed = true;
        else if (difficulty == "Hard" && score >= 60) passed = true;
        else if (difficulty == "Quiz" && score >= 70) passed = true;

        // Handle minigame-level progress
        var existing = db.Table<MiniGameProgress>()
            .FirstOrDefault(x => x.DisasterName == disasterName
                            && x.Difficulty == difficulty
                            && x.MiniGameIndex == miniGameIndex);

        if (existing != null)
        {
            if (!existing.Passed && passed)
            {
                existing.Passed = true;
                db.Update(existing);
            }
        }
        else
        {
            db.Insert(new MiniGameProgress
            {
                DisasterName = disasterName,
                Difficulty = difficulty,
                MiniGameIndex = miniGameIndex,
                Passed = passed
            });
        }

        // Handle disaster-level progress
        var progress = GetDisasterProgress(disasterName);
        if (progress == null) return;

        if (difficulty == "Easy" && passed)
        {
            progress.EasyCompleted = true;
        }
        else if (difficulty == "Hard" && passed)
        {
            progress.HardCompleted = true;
        }
        else if (difficulty == "Quiz" && passed)
        {
            progress.QuizCompleted = true;
            progress.HardUnlocked = true; // 🔑 Unlock Hard only via Quiz
            UnlockNextDisaster(disasterName);
        }

        db.Update(progress);
    }

    private static void UnlockNextDisaster(string currentDisaster)
    {
        var allDisasters = db.Table<DisasterProgress>().ToList();
        for (int i = 0; i < allDisasters.Count; i++)
        {
            if (allDisasters[i].DisasterName == currentDisaster && i + 1 < allDisasters.Count)
            {
                allDisasters[i + 1].IsUnlocked = true;
                db.Update(allDisasters[i + 1]);
                break;
            }
        }
    }

    public static List<MiniGameProgress> GetMiniGameProgress(string disasterName, string difficulty)
    {
        if (db == null) Init();
        return db.Table<MiniGameProgress>()
                 .Where(x => x.DisasterName == disasterName && x.Difficulty == difficulty)
                 .ToList();
    }

    // -------------------------
    // DEBUG HELPERS
    // -------------------------
    public static void DebugListAllProgress()
    {
        if (db == null) Init();

        var all = db.Table<DisasterProgress>().ToList();
        Debug.Log("===== Disaster Progress Table =====");
        foreach (var d in all)
        {
            Debug.Log($"{d.DisasterName} | Unlocked={d.IsUnlocked} | Easy={d.EasyCompleted} | HardUnlocked={d.HardUnlocked} | Hard={d.HardCompleted} | Quiz={d.QuizCompleted}");
        }
    }

    public static void DebugListAllMiniGames()
    {
        if (db == null) Init();

        var all = db.Table<MiniGameProgress>().ToList();
        Debug.Log("===== MiniGame Progress Table =====");
        foreach (var m in all)
        {
            Debug.Log($"{m.DisasterName} | {m.Difficulty} | MiniGame={m.MiniGameIndex} | Passed={m.Passed}");
        }
    }
}

// -------------------------
// TABLE CLASSES
// -------------------------
public class DisasterProgress
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string DisasterName { get; set; }
    public bool EasyCompleted { get; set; }
    public bool HardUnlocked { get; set; }
    public bool HardCompleted { get; set; }
    public bool QuizCompleted { get; set; }
    public bool IsUnlocked { get; set; }
}

public class MiniGameProgress
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string DisasterName { get; set; }
    public string Difficulty { get; set; } // "Easy", "Hard", "Quiz"
    public int MiniGameIndex { get; set; }
    public bool Passed { get; set; }       // true if minigame completed successfully
}
