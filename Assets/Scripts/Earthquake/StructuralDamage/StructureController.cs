using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class StructureController : MonoBehaviour, IGameStarter
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

    [Header("UI References")]
    public TMP_Text progressText;
    public Button menuButton;
    public Slider timerSlider;
    public TimerLogic timerLogic;

    [Header("Game Settings")]
    public float gameDuration = 60f;

    private int currentBackgroundIndex = -1;
    private List<int> backgroundOrder = new List<int>();
    private List<GameObject> activeCracks = new List<GameObject>();

    private bool gameStarted = false;
    private bool timerEnded = false;

    private int totalCracks;
    private int cracksFound;

    void Start()
    {
        foreach (var bg in backgrounds)
        {
            bg.SetActive(false);
            var img = bg.GetComponent<Image>();
            if (img != null) img.raycastTarget = false; // hidden bgs won’t block raycasts
        }

        // randomize order
        List<int> indices = new List<int>();
        for (int i = 0; i < backgrounds.Length; i++) indices.Add(i);
        while (indices.Count > 0)
        {
            int rand = Random.Range(0, indices.Count);
            backgroundOrder.Add(indices[rand]);
            indices.RemoveAt(rand);
        }

        LoadNextBackground();
        SetCrackButtonsInteractable(false);

        if (timerLogic != null)
            timerLogic.OnTimerFinished += HandleTimerEnd;
    }

    public void StartGame()
    {
        gameStarted = true;
        SetCrackButtonsInteractable(true);
        timerEnded = false;

        if (timerLogic != null)
            timerLogic.StartTimer(gameDuration);

        Debug.Log("✅ Game Started");
    }

    private void SetCrackButtonsInteractable(bool state)
    {
        foreach (var crack in activeCracks)
        {
            Button btn = crack.GetComponent<Button>();
            if (btn != null)
                btn.interactable = state;
        }
    }

    void LoadNextBackground()
    {
        currentBackgroundIndex++;
        if (currentBackgroundIndex >= backgroundOrder.Count)
        {
            EndGame();
            return;
        }

        foreach (var crack in activeCracks) Destroy(crack);
        activeCracks.Clear();

        foreach (var bg in backgrounds)
        {
            bg.SetActive(false);
            var img = bg.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = false;
        }

        int bgIndex = backgroundOrder[currentBackgroundIndex];
        GameObject activeBg = backgrounds[bgIndex];
        activeBg.SetActive(true);

        var bgImage = activeBg.GetComponent<Image>();
        if (bgImage != null)
            bgImage.raycastTarget = true;

        bool isCR = activeBg.name.Contains(crBackgroundName);
        Sprite[] colorCrackArray = isCR ? blueCrackSprites : redCrackSprites;

        int crackCount = Random.Range(minCracks, maxCracks + 1);
        int blackCrackCount = Mathf.RoundToInt(crackCount * blackCrackRatio);
        int colorCrackCount = crackCount - blackCrackCount;

        totalCracks += crackCount;

        RectTransform bgRect = activeBg.GetComponent<RectTransform>();
        RectTransform[] childRects = activeBg.GetComponentsInChildren<RectTransform>(true);

        // Exclusion rects for UI and children
        List<RectTransform> exclusionRects = new List<RectTransform>(childRects);
        if (menuButton != null) exclusionRects.Add(menuButton.GetComponent<RectTransform>());
        if (progressText != null) exclusionRects.Add(progressText.GetComponent<RectTransform>());
        if (timerSlider != null) exclusionRects.Add(timerSlider.GetComponent<RectTransform>());

        // Spawn cracks
        for (int i = 0; i < blackCrackCount; i++)
        {
            var crackObj = SpawnCrack(bgRect, exclusionRects, blackCrackSprites);
            if (crackObj != null) activeCracks.Add(crackObj);
        }

        for (int i = 0; i < colorCrackCount; i++)
        {
            var crackObj = SpawnCrack(bgRect, exclusionRects, colorCrackArray);
            if (crackObj != null) activeCracks.Add(crackObj);
        }

        UpdateProgressText();
        if (!gameStarted)
            SetCrackButtonsInteractable(false);
        else
            SetCrackButtonsInteractable(true);
    }

    GameObject SpawnCrack(RectTransform bgRect, List<RectTransform> exclusionRects, Sprite[] crackSpriteArray)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Sprite crackSprite = crackSpriteArray[Random.Range(0, crackSpriteArray.Length)];
            float randomScale = Random.Range(minCrackSize, maxCrackSize);
            Vector2 crackSize = crackSprite.rect.size * randomScale;

            if (crackSize.x > bgRect.rect.width || crackSize.y > bgRect.rect.height)
            {
                Debug.LogWarning("⚠️ Background too small for crack sprite at requested scale.");
                continue;
            }

            float halfWidth = bgRect.rect.width / 2f - crackSize.x / 2f;
            float halfHeight = bgRect.rect.height / 2f - crackSize.y / 2f;

            Vector2 randomPos = new Vector2(
                Random.Range(-halfWidth, halfWidth),
                Random.Range(-halfHeight, halfHeight)
            );

            Rect crackBounds = new Rect(randomPos - crackSize / 2f, crackSize);

            bool overlaps = false;
            foreach (var ui in exclusionRects)
            {
                if (ui == null || ui == bgRect) continue;

                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    bgRect,
                    RectTransformUtility.WorldToScreenPoint(null, ui.position),
                    null,
                    out localPos
                );

                Rect uiRect = new Rect(localPos - ui.rect.size / 2f, ui.rect.size);
                if (crackBounds.Overlaps(uiRect))
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

                Image crackImage = crackBtn.GetComponent<Image>();
                crackImage.sprite = crackSprite;
                crackImage.raycastTarget = true;

                crackBtn.onClick.RemoveAllListeners();
                crackBtn.onClick.AddListener(() => CrackFound(crackBtn.gameObject));

                return crackBtn.gameObject;
            }
        }

        return null;
    }

    void CrackFound(GameObject crack)
    {
        if (!gameStarted) return;

        if (activeCracks.Contains(crack))
        {
            activeCracks.Remove(crack);
            cracksFound++;
            Destroy(crack);

            if (activeCracks.Count == 0)
                LoadNextBackground();
            else
                UpdateProgressText();

            if (cracksFound >= totalCracks)
                EndGame();
        }
    }

    void UpdateProgressText()
    {
        if (progressText != null)
            progressText.text = $"Cracks left: {activeCracks.Count}";
    }

    private void HandleTimerEnd()
    {
        timerEnded = true;
        EndGame();
    }

    void EndGame()
    {
        if (!gameStarted) return;
        gameStarted = false;
        SetCrackButtonsInteractable(false);
        if (timerLogic != null) timerLogic.StopTimer();

        int score = Mathf.RoundToInt(((float)cracksFound / totalCracks) * 100);
        score = Mathf.Clamp(score, 0, 100);

        bool passed = score >= 60;
        GameResults.Score = score;
        GameResults.Passed = passed;
        GameResults.DisasterName = "Earthquake";
        GameResults.MiniGameIndex = 5;
        GameResults.Difficulty = "Hard";

        DBManager.SaveProgress("Earthquake", "Hard", 5, passed);
        SceneTracker.SetCurrentMiniGame("Earthquake", "Hard", SceneManager.GetActiveScene().name);

        Debug.Log($"🏁 Game Ended | Found: {cracksFound}/{totalCracks} | TimerEnded: {timerEnded} | Score: {score} pts | Passed: {passed}");

        SceneManager.LoadScene("TransitionScene");
    }
}
