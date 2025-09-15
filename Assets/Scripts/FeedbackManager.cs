using UnityEngine;
using TMPro;
using System.Collections;

public class FeedbackManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI feedbackPrefab; 
    public Canvas mainCanvas;               

    [Header("Settings")]
    public float displayDuration = 1.5f;
    public float slideDistance = 50f;
    public float fadeSpeed = 2f;

    private string[] positiveReactions = new string[]
    {
        "Very good!", "Nice!", "Correct!", "Well done!", "Great!"
    };

    private string[] negativeReactions = new string[]
    {
        "Uh oh!", "That's not right!", "Try again!", "Oops!", "Incorrect!"
    };

    private Color positiveColor = Color.green;
    private Color negativeColor = Color.red;

    public void ShowPositive()
    {
        string reaction = positiveReactions[Random.Range(0, positiveReactions.Length)];
        StartCoroutine(DisplayFeedback(reaction, positiveColor));
    }

    public void ShowNegative()
    {
        string reaction = negativeReactions[Random.Range(0, negativeReactions.Length)];
        StartCoroutine(DisplayFeedback(reaction, negativeColor));
    }

    private IEnumerator DisplayFeedback(string text, Color color)
    {
        if (feedbackPrefab == null || mainCanvas == null)
        {
            Debug.LogError("[FeedbackManager] feedbackPrefab or mainCanvas not assigned!");
            yield break;
        }

        // Instantiate a new TMP text object
        TextMeshProUGUI tmp = Instantiate(feedbackPrefab, mainCanvas.transform);
        tmp.text = text;
        tmp.color = color;

        // Disable raycast so it won't block swipes or clicks
        tmp.raycastTarget = false;

        RectTransform rect = tmp.GetComponent<RectTransform>();

        // Place feedback in the center of the canvas
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        tmp.gameObject.SetActive(true);

        float elapsed = 0f;
        Vector3 startPos = rect.anchoredPosition;
        Vector3 endPos = startPos + Vector3.up * slideDistance;

        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / displayDuration;

            // Slide
            rect.anchoredPosition = Vector3.Lerp(startPos, endPos, t);

            // Fade
            Color c = tmp.color;
            c.a = Mathf.Lerp(1f, 0f, t * fadeSpeed);
            tmp.color = c;

            yield return null;
        }

        Destroy(tmp.gameObject);
    }

}
