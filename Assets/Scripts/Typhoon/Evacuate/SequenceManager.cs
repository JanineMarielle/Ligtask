using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SequenceManager : MonoBehaviour
{
    [Header("References")]
    public RectTransform car;
    public RectTransform building;

    [Header("Settings")]
    public int rounds = 3;
    public int arrowsPerRound = 4;
    public float carOffsetX = -50f;
    public float carOffsetY = -100f;
    public float moveDuration = 0.5f;

    private List<List<string>> allSequences = new List<List<string>>();
    private int currentRound = 0;
    private int currentIndex = 0;
    private int totalRightArrows = 0;
    private int correctRightArrows = 0;
    private float rightStepDistance;

    void Start()
    {
        GenerateAllSequences();
        totalRightArrows = 0;
        foreach (var seq in allSequences)
        {
            foreach (string arrow in seq)
            {
                if (arrow == "Right") totalRightArrows++;
            }
        }
        float totalDistance = building.anchoredPosition.x - car.anchoredPosition.x;
        rightStepDistance = totalRightArrows > 0 ? Mathf.Abs(totalDistance) / totalRightArrows : 0f;
        Debug.Log($"Total Right Arrows: {totalRightArrows}, Step: {rightStepDistance}");
        PlayRound();
    }

    void GenerateAllSequences()
    {
        allSequences.Clear();
        for (int i = 0; i < rounds; i++)
        {
            List<string> sequence = new List<string>();
            for (int j = 0; j < arrowsPerRound; j++)
            {
                string[] possibleArrows = { "Up", "Down", "Right" };
                string arrow = possibleArrows[Random.Range(0, possibleArrows.Length)];
                sequence.Add(arrow);
            }
            allSequences.Add(sequence);
        }
    }

    void PlayRound()
    {
        if (currentRound >= rounds)
        {
            Debug.Log("All rounds finished!");
            return;
        }
        Debug.Log($"Round {currentRound + 1}: {string.Join(", ", allSequences[currentRound])}");
        currentIndex = 0;
    }

    public void OnPlayerInput(string inputArrow)
    {
        if (currentRound >= rounds) return;
        string expectedArrow = allSequences[currentRound][currentIndex];
        Debug.Log($"Expected: {expectedArrow}, Got: {inputArrow}");
        if (inputArrow == expectedArrow)
        {
            if (inputArrow == "Right")
            {
                correctRightArrows++;
                StartCoroutine(MoveCarRight());
            }
            else if (inputArrow == "Up")
            {
                MoveCarVertical(1);
            }
            else if (inputArrow == "Down")
            {
                MoveCarVertical(-1);
            }
        }
        else
        {
            Debug.Log("Wrong input!");
        }
        currentIndex++;
        if (currentIndex >= arrowsPerRound)
        {
            currentRound++;
            PlayRound();
        }
    }

    IEnumerator MoveCarRight()
    {
        Vector2 startPos = car.anchoredPosition;
        float parentWidth = ((RectTransform)car.parent).rect.width;
        float rightEdgeX = parentWidth * 0.5f;
        Vector2 targetPos = startPos + new Vector2(rightStepDistance, 0);
        targetPos.x = Mathf.Min(targetPos.x, rightEdgeX);
        float elapsed = 0;
        while (elapsed < moveDuration)
        {
            car.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        car.anchoredPosition = targetPos;
    }

    void MoveCarVertical(int dir)
    {
        float rowHeight = 100f;
        Vector2 pos = car.anchoredPosition;
        pos.y += dir * rowHeight;
        pos.y = Mathf.Clamp(pos.y, -rowHeight, rowHeight);
        car.anchoredPosition = pos;
    }
}
