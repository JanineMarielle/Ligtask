using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using SQLite4Unity3d;

public class StructureHard : MonoBehaviour, IGameStarter
{
    [Header("Background Settings")]
    public GameObject[] backgrounds;
    public string crBackgroundName = "CR";

    [Header("Crack Settings")]
    public Button crackButtonPrefab;
    public Sprite[] redCrackSprites;
    public Sprite[] blueCrackSprites;
    public Sprite[] blackCrackSprites;
    [Range(0, 1)] public float blackCrackRatio = 0.7f;
    public int minCracks = 3;
    public int maxCracks = 6;
    public float minCrackSize = 0.8f;
    public float maxCrackSize = 1.3f;
    public int maxSpawnAttempts = 30;

    [Header("UI Buttons")]
    public Button nextButton;
    public Button prevButton;
    public Button doneButton;
    public Button menuButton;

    // 🔹 Progress tracking
    private int cracksFound = 0;
    private int totalCracks = 0;
    private bool gameStarted = false;

    private int currentBackgroundIndex = 0;
    private Dictionary<int, List<GameObject>> cracksPerBackground = new Dictionary<int, List<GameObject>>();
    private Dictionary<int, bool> backgroundGenerated = new Dictionary<int, bool>();

    void Start()
    {
        DBManager.Init();

        foreach (var bg in backgrounds)
            bg.SetActive(false);

        nextButton.onClick.AddListener(NextBackground);
        prevButton.onClick.AddListener(PreviousBackground);
        doneButton.onClick.AddListener(EndGame);

        for (int i = 0; i < backgrounds.Length; i++)
            backgroundGenerated[i] = false;

        LoadBackground(currentBackgroundIndex);

        // disable buttons and cracks before StartGame()
        SetButtonsInteractable(false);
        SetCrackButtonsInteractable(false);
    }

    public void StartGame()
    {
        gameStarted = true;
        cracksFound = 0;

        SetButtonsInteractable(true);
        SetCrackButtonsInteractable(true);

        Debug.Log("[StructureHard] ✅ Game Started!");
    }

    private void SetButtonsInteractable(bool state)
    {
        if (nextButton) nextButton.interactable = state;
        if (prevButton) prevButton.interactable = state;
        if (doneButton) doneButton.interactable = state;
        if (menuButton) menuButton.interactable = state;
    }

    private void SetCrackButtonsInteractable(bool state)
    {
        foreach (var kvp in cracksPerBackground)
        {
            foreach (var crack in kvp.Value)
            {
                if (crack != null)
                {
                    Button btn = crack.GetComponent<Button>();
                    if (btn != null) btn.interactable = state;
                }
            }
        }
    }

    void LoadBackground(int index)
    {
        if (index < 0 || index >= backgrounds.Length) return;

        foreach (var bg in backgrounds)
            bg.SetActive(false);

        GameObject activeBg = backgrounds[index];
        activeBg.SetActive(true);

        foreach (var kvp in cracksPerBackground)
        {
            foreach (var crack in kvp.Value)
                if (crack != null) crack.SetActive(kvp.Key == index);
        }

        if (!backgroundGenerated[index])
        {
            cracksPerBackground[index] = new List<GameObject>();
            GenerateCracksForBackground(index);
            backgroundGenerated[index] = true;
        }

        if (!gameStarted)
            SetCrackButtonsInteractable(false);
    }

    void GenerateCracksForBackground(int index)
    {
        GameObject activeBg = backgrounds[index];
        RectTransform bgRect = activeBg.GetComponent<RectTransform>();

        bool isCR = activeBg.name.Contains(crBackgroundName);
        Sprite[] colorCrackArray = isCR ? blueCrackSprites : redCrackSprites;

        int cracksToSpawn = Random.Range(minCracks, maxCracks + 1);
        totalCracks += cracksToSpawn;

        int blackCrackCount = Mathf.RoundToInt(cracksToSpawn * blackCrackRatio);
        int colorCrackCount = cracksToSpawn - blackCrackCount;

        RectTransform[] childRects = activeBg.GetComponentsInChildren<RectTransform>(true);
        List<RectTransform> forbiddenRects = new List<RectTransform>(childRects);

        if (menuButton) forbiddenRects.Add(menuButton.GetComponent<RectTransform>());
        if (doneButton) forbiddenRects.Add(doneButton.GetComponent<RectTransform>());
        if (nextButton) forbiddenRects.Add(nextButton.GetComponent<RectTransform>());
        if (prevButton) forbiddenRects.Add(prevButton.GetComponent<RectTransform>());

        for (int i = 0; i < blackCrackCount; i++)
        {
            var crack = SpawnCrack(index, bgRect, forbiddenRects, blackCrackSprites);
            if (crack != null) cracksPerBackground[index].Add(crack);
        }

        for (int i = 0; i < colorCrackCount; i++)
        {
            var crack = SpawnCrack(index, bgRect, forbiddenRects, colorCrackArray);
            if (crack != null) cracksPerBackground[index].Add(crack);
        }
    }

    GameObject SpawnCrack(int bgIndex, RectTransform bgRect, List<RectTransform> forbiddenRects, Sprite[] crackSprites)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Sprite crackSprite = crackSprites[Random.Range(0, crackSprites.Length)];
            Vector2 randomPos = new Vector2(
                Random.Range(-bgRect.rect.width / 2f, bgRect.rect.width / 2f),
                Random.Range(-bgRect.rect.height / 2f, bgRect.rect.height / 2f)
            );

            float randomScale = Random.Range(minCrackSize, maxCrackSize);
            Vector2 crackSize = crackSprite.rect.size * randomScale;
            Rect crackBounds = new Rect(randomPos - crackSize / 2f, crackSize);

            bool overlaps = false;
            foreach (var forbidden in forbiddenRects)
            {
                if (forbidden == bgRect) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, forbidden.position, null, out Vector2 childCenter);
                Rect childRect = new Rect(childCenter - forbidden.rect.size / 2f, forbidden.rect.size);
                if (crackBounds.Overlaps(childRect))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                Button crackBtn = Instantiate(crackButtonPrefab, bgRect);
                RectTransform crackRect = crackBtn.GetComponent<RectTransform>();
                crackRect.anchoredPosition = randomPos;
                crackRect.localScale = Vector3.one * randomScale;
                crackRect.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                crackRect.localScale = new Vector3(
                    crackRect.localScale.x * (Random.value > 0.5f ? -1 : 1),
                    crackRect.localScale.y * (Random.value > 0.5f ? -1 : 1),
                    1
                );

                crackBtn.GetComponent<Image>().sprite = crackSprite;
                crackBtn.onClick.AddListener(() =>
                {
                    if (gameStarted)
                        CrackFound(bgIndex, crackBtn.gameObject);
                });

                crackBtn.interactable = false;
                return crackBtn.gameObject;
            }
        }

        Debug.LogWarning("Skipped spawning a crack due to lack of valid space.");
        return null;
    }

    void CrackFound(int bgIndex, GameObject crack)
    {
        if (!gameStarted) return;

        if (cracksPerBackground.ContainsKey(bgIndex) && cracksPerBackground[bgIndex].Contains(crack))
        {
            cracksPerBackground[bgIndex].Remove(crack);
            Destroy(crack);
            cracksFound++;
        }
    }

    private void EndGame()
    {
        if (!gameStarted) return;

        gameStarted = false;
        SetButtonsInteractable(false);
        SetCrackButtonsInteractable(false);

        int rawScore = cracksFound * 20;
        float ratio = (totalCracks > 0) ? (float)cracksFound / totalCracks : 0f;
        int percentageScore = Mathf.RoundToInt(ratio * 100f);
        bool passed = ratio >= 0.6f;

        GameResults.Score = percentageScore;
        GameResults.Passed = passed;
        GameResults.DisasterName = "Earthquake";
        GameResults.Difficulty = "Hard";
        GameResults.MiniGameIndex = 5;

        DBManager.SaveProgress("Earthquake", "Hard", 5, passed);
        SceneTracker.SetCurrentMiniGame("Earthquake", "Hard", SceneManager.GetActiveScene().name);

        Debug.Log($"🏁 Game Ended | Found: {cracksFound}/{totalCracks} | Raw: {rawScore} | Score: {percentageScore}% | Passed: {passed}");
        SceneManager.LoadScene("TransitionScene");
    }

    void NextBackground()
    {
        if (!gameStarted) return;

        currentBackgroundIndex++;
        if (currentBackgroundIndex >= backgrounds.Length)
            currentBackgroundIndex = 0;
        LoadBackground(currentBackgroundIndex);
    }

    void PreviousBackground()
    {
        if (!gameStarted) return;

        currentBackgroundIndex--;
        if (currentBackgroundIndex < 0)
            currentBackgroundIndex = backgrounds.Length - 1;
        LoadBackground(currentBackgroundIndex);
    }
}
