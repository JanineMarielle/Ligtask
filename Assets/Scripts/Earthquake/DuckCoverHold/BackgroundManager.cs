using UnityEngine;
using System.Collections;

public class BackgroundManager : MonoBehaviour
{
    [Header("References")]
    public RectTransform backgroundA;
    public RectTransform backgroundB;

    [Header("Settings")]
    public float slideDuration = 1.2f;

    private RectTransform activeBackground;
    private RectTransform nextBackground;
    private bool isSliding = false;

    void Start()
    {
        if (backgroundA != null) StretchBackground(backgroundA);
        if (backgroundB != null) StretchBackground(backgroundB);

        activeBackground = backgroundA;
        nextBackground = backgroundB;
    }

    private void StretchBackground(RectTransform bg)
    {
        bg.anchorMin = new Vector2(0f, 0f);
        bg.anchorMax = new Vector2(1f, 1f);
        bg.pivot = new Vector2(0.5f, 0.5f); // ✅ safer pivot for sliding
        bg.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Slide backgrounds horizontally.
    /// </summary>
    public IEnumerator SlideBackground()
    {
        if (isSliding) yield break;
        isSliding = true;

        float width = activeBackground.rect.width;

        // Position next background immediately to the right
        nextBackground.anchoredPosition = activeBackground.anchoredPosition + new Vector2(width, 0);

        Vector2 oldBgStart = activeBackground.anchoredPosition;
        Vector2 oldBgEnd   = oldBgStart - new Vector2(width, 0);

        Vector2 nextBgStart = nextBackground.anchoredPosition;
        Vector2 nextBgEnd   = oldBgStart;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            activeBackground.anchoredPosition = Vector2.Lerp(oldBgStart, oldBgEnd, t);
            nextBackground.anchoredPosition   = Vector2.Lerp(nextBgStart, nextBgEnd, t);

            yield return null;
        }

        // Snap final positions
        activeBackground.anchoredPosition = oldBgEnd;
        nextBackground.anchoredPosition   = Vector2.zero;

        // Swap
        SwapBackgrounds();

        isSliding = false;
    }

    // ✅ Helpers for DuckCoverHold
    public RectTransform GetActiveBackground() => activeBackground;
    public RectTransform GetNextBackground() => nextBackground;

    public void SwapBackgrounds()
    {
        RectTransform temp = activeBackground;
        activeBackground = nextBackground;
        nextBackground = temp;
    }
}
