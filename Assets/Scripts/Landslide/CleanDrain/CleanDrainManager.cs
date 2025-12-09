using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class CleanDrainManager : MonoBehaviour, IGameStarter
{
[Header("Prefabs")]
public GameObject normalTrashPrefab;
public GameObject bottlePrefab;


[Header("Sprite Sets")]
public List<Sprite> normalTrashSprites;
public List<Sprite> bottleSprites;

[Header("UI References")]
public RectTransform dropZone;
public Transform spawnCVsParent;
public GameObject spawnCanvasPrefab;
public UnityEngine.UI.Image bgImagePrefab;
public TimerLogic timerLogic;

[Header("Drop Zone Padding")]
public float dropZoneTopPadding = 80f;
public float dropZoneSidePadding = 50f;
public int trashCount = 6;

[Header("Rounds")]
public int totalRounds = 3;
private int currentRound = 0;

[Header("Game Settings")]
public int pointsPerTrash = 10;
public float timePerRound = 15f;

private Queue<Sprite> spriteQueue;
private Dictionary<Sprite, bool> spriteIsBottle = new Dictionary<Sprite, bool>();
private List<RectTransform> roundCanvases = new List<RectTransform>();
private List<UnityEngine.UI.Image> roundBGs = new List<UnityEngine.UI.Image>();
private List<int> roundTrashRemaining = new List<int>();

private int score = 0;
private bool roundActive = false;
private bool gameStarted = false;

void Awake()
{
    dropZone.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
    PrepareSpriteQueue();
    CreateRounds();
}

public void StartGame()
{
    if (gameStarted) return;
    gameStarted = true;
    score = 0;
    currentRound = 0;

    StartRound(currentRound);
}

void PrepareSpriteQueue()
{
    spriteQueue = new Queue<Sprite>();
    spriteIsBottle.Clear();

    List<Sprite> combined = new List<Sprite>();
    foreach (var s in normalTrashSprites) { combined.Add(s); spriteIsBottle[s] = false; }
    foreach (var s in bottleSprites) { combined.Add(s); spriteIsBottle[s] = true; }

    for (int i = 0; i < combined.Count; i++)
    {
        int rand = Random.Range(i, combined.Count);
        (combined[i], combined[rand]) = (combined[rand], combined[i]);
    }

    foreach (var sprite in combined)
        spriteQueue.Enqueue(sprite);
}

Sprite GetNextSprite()
{
    if (spriteQueue.Count == 0) PrepareSpriteQueue();
    return spriteQueue.Dequeue();
}

void CreateRounds()
{
    for (int i = 0; i < totalRounds; i++)
    {
        var bg = Instantiate(bgImagePrefab, spawnCVsParent);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = (i == 0) ? Vector2.zero : new Vector2(Screen.width, 0);
        roundBGs.Add(bg);

        var roundGO = Instantiate(spawnCanvasPrefab, spawnCVsParent);
        RectTransform rect = roundGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = (i == 0) ? Vector2.zero : new Vector2(Screen.width, 0);
        roundCanvases.Add(rect);

        RectTransform container = FindSpawnCanvas(roundGO.transform) ?? roundGO.GetComponent<RectTransform>();
        int totalTrash = SpawnTrashItems(container, i);
        roundTrashRemaining.Add(totalTrash);
    }
}

RectTransform FindSpawnCanvas(Transform parent)
{
    foreach (Transform child in parent)
    {
        if (child.childCount > 0) return child.GetComponent<RectTransform>();
        var found = FindSpawnCanvas(child);
        if (found != null) return found;
    }
    return null;
}

int SpawnTrashItems(RectTransform container, int roundIndex)
{
    for (int i = 0; i < trashCount; i++)
    {
        Sprite sprite = GetNextSprite();
        GameObject prefab = spriteIsBottle[sprite] ? bottlePrefab : normalTrashPrefab;
        var trash = Instantiate(prefab, container);
        trash.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
        trash.GetComponent<RectTransform>().anchoredPosition = GetValidSpawnPosition(trash.GetComponent<RectTransform>(), container);

        TrashItem item = trash.GetComponent<TrashItem>();
        item.gameManager = this;
        item.roundIndex = roundIndex;
        item.dropZone = dropZone;
        item.OnCollected = TrashCollected;
    }
    return trashCount;
}

Vector2 GetValidSpawnPosition(RectTransform trashRect, RectTransform container)
{
    int attempts = 0;
    while (attempts < 80)
    {
        attempts++;
        float randX = Random.Range(-container.rect.width / 2, container.rect.width / 2);
        float randY = Random.Range(-container.rect.height / 2, container.rect.height / 2);
        Vector2 pos = new Vector2(randX, randY);
        trashRect.anchoredPosition = pos;
        if (!IsOverlappingDropZone(trashRect)) return pos;
    }
    return Vector2.zero;
}

bool IsOverlappingDropZone(RectTransform rect)
{
    Rect trashRect = GetWorldRect(rect);
    Rect paddedDZ = GetPaddedDropZone();
    return trashRect.Overlaps(paddedDZ);
}

Rect GetWorldRect(RectTransform rt)
{
    Vector3[] corners = new Vector3[4];
    rt.GetWorldCorners(corners);
    return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
}

public Rect GetPaddedDropZone()
{
    Vector3[] corners = new Vector3[4];
    dropZone.GetWorldCorners(corners);
    corners[0].x += dropZoneSidePadding;
    corners[1].x += dropZoneSidePadding;
    corners[2].x -= dropZoneSidePadding;
    corners[3].x -= dropZoneSidePadding;
    corners[1].y -= dropZoneTopPadding;
    corners[2].y -= dropZoneTopPadding;
    return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
}

public void TrashCollected(int roundIndex)
{
    roundTrashRemaining[roundIndex]--;
    score += pointsPerTrash;

    if (roundTrashRemaining[roundIndex] <= 0) OnRoundComplete();
}

void StartRound(int round)
{
    roundCanvases[round].gameObject.SetActive(true);
    roundBGs[round].gameObject.SetActive(true);
    roundActive = true;

    if (timerLogic != null)
    {
        timerLogic.OnTimerFinished -= EndRoundDueToTimeout;
        timerLogic.OnTimerFinished += EndRoundDueToTimeout;
        timerLogic.StartTimer(timePerRound);
    }
}

void EndRoundDueToTimeout()
{
    if (!roundActive) return;
    roundActive = false;
    OnRoundComplete();
}

public void OnRoundComplete()
{
    roundActive = false;
    currentRound++;
    if (currentRound >= totalRounds)
    {
        GameOver();
    }
    else
    {
        StartCoroutine(SlideToNextRound(currentRound));
    }
}

IEnumerator SlideToNextRound(int nextRound)
{
    RectTransform oldCanvas = roundCanvases[nextRound - 1];
    RectTransform newCanvas = roundCanvases[nextRound];
    var oldBG = roundBGs[nextRound - 1];
    var newBG = roundBGs[nextRound];

    float duration = 0.5f;
    float t = 0f;
    Vector2 oldStart = oldCanvas.anchoredPosition;
    Vector2 oldEnd = new Vector2(-Screen.width, 0);
    Vector2 newStart = newCanvas.anchoredPosition;
    Vector2 newEnd = Vector2.zero;

    Vector2 bgOldStart = oldBG.rectTransform.anchoredPosition;
    Vector2 bgOldEnd = oldEnd;
    Vector2 bgNewStart = newBG.rectTransform.anchoredPosition;
    Vector2 bgNewEnd = Vector2.zero;

    while (t < duration)
    {
        t += Time.deltaTime;
        float L = t / duration;

        oldCanvas.anchoredPosition = Vector3.Lerp(oldStart, oldEnd, L);
        newCanvas.anchoredPosition = Vector3.Lerp(newStart, newEnd, L);

        oldBG.rectTransform.anchoredPosition = Vector3.Lerp(bgOldStart, bgOldEnd, L);
        newBG.rectTransform.anchoredPosition = Vector3.Lerp(bgNewStart, bgNewEnd, L);

        yield return null;
    }

    oldCanvas.anchoredPosition = oldEnd;
    newCanvas.anchoredPosition = newEnd;
    oldBG.rectTransform.anchoredPosition = bgOldEnd;
    newBG.rectTransform.anchoredPosition = bgNewEnd;

    StartRound(nextRound);
}

void GameOver()
{
    gameStarted = false;
    roundActive = false;

    int totalScore = Mathf.Clamp(score, 0, totalRounds * trashCount * pointsPerTrash);
    bool passed = totalScore >= (totalRounds * trashCount * pointsPerTrash * 0.6f);

    GameResults.Score = totalScore;
    GameResults.Passed = passed;
    GameResults.DisasterName = "Landslide";
    GameResults.MiniGameIndex = 2;
    GameResults.Difficulty = "Easy";

    DBManager.SaveProgress("Landslide", "Easy", 2, passed);
    SceneTracker.SetCurrentMiniGame("Landslide", "Easy", SceneManager.GetActiveScene().name);

    Debug.Log($"🏁 Game Ended | Score: {totalScore} | Passed: {passed}");

    SceneManager.LoadScene("TransitionScene");
}


}
