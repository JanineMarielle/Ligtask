using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CabinetManager : MonoBehaviour
{
    [Header("Cabinets")]
    public RectTransform currentCabinet;
    public RectTransform nextCabinet; // Assign in Inspector

    [Header("Animation Settings")]
    public float zoomOutScale = 1f;
    public float zoomDuration = 0.5f;
    public float slideDuration = 0.6f;
    public float offscreenX = -1500f;
    public float onscreenX = 0f;
    public float incomingStartX = 1500f;

    [Header("Basket Settings")]
    public RectTransform basketDropZone; // Assign in Inspector
    public RectTransform itemContainer;  // Assign in Inspector
    public float basketAnimDuration = 1f;

    private void Awake()
    {
        // Place next cabinet offscreen at start
        if (nextCabinet != null)
            nextCabinet.anchoredPosition = new Vector2(incomingStartX, nextCabinet.anchoredPosition.y);
    }

    public void PlayCabinetSwap(Action onFinished)
    {
        if (currentCabinet == null || nextCabinet == null)
        {
            onFinished?.Invoke();
            return;
        }

        StartCoroutine(SwapRoutine(onFinished));
    }

    private IEnumerator SwapRoutine(Action onFinished)
    {
        // --- Step 1: Zoom out old cabinet ---
        Vector3 startScale = currentCabinet.localScale;
        Vector3 endScale = Vector3.one * zoomOutScale;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            float t = elapsed / zoomDuration;
            currentCabinet.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentCabinet.localScale = endScale;

        // --- Step 2: Slide cabinets ---
        Vector2 oldStart = currentCabinet.anchoredPosition;
        Vector2 oldEnd = new Vector2(offscreenX, oldStart.y);
        Vector2 newStart = new Vector2(incomingStartX, nextCabinet.anchoredPosition.y);
        Vector2 newEnd = new Vector2(onscreenX, newStart.y);

        elapsed = 0f;
        while (elapsed < slideDuration)
        {
            float t = elapsed / slideDuration;
            currentCabinet.anchoredPosition = Vector2.Lerp(oldStart, oldEnd, t);
            nextCabinet.anchoredPosition = Vector2.Lerp(newStart, newEnd, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentCabinet.anchoredPosition = oldEnd;
        nextCabinet.anchoredPosition = newEnd;

        // --- Step 3: Animate basket + items AFTER swap ---
        yield return StartCoroutine(AnimateBasketToCenter());

        onFinished?.Invoke();
    }

    private IEnumerator AnimateBasketToCenter()
    {
        if (basketDropZone == null) yield break;

        Vector3 startPos = basketDropZone.position;
        Vector3 targetPos = new Vector3(Screen.width / 2f, Screen.height / 2f, startPos.z);

        // Record initial offsets of children relative to basket
        Dictionary<Transform, Vector3> childOffsets = new Dictionary<Transform, Vector3>();
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
                childOffsets[child] = child.position - startPos;
        }

        float elapsed = 0f;
        while (elapsed < basketAnimDuration)
        {
            float t = elapsed / basketAnimDuration;
            basketDropZone.position = Vector3.Lerp(startPos, targetPos, t);

            if (itemContainer != null)
            {
                foreach (Transform child in itemContainer)
                {
                    if (childOffsets.ContainsKey(child))
                        child.position = basketDropZone.position + childOffsets[child];
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final positions
        basketDropZone.position = targetPos;
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
                child.position = basketDropZone.position + childOffsets[child];
        }

        // Destroy children and hide basket
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
                Destroy(child.gameObject);
        }
        basketDropZone.gameObject.SetActive(false);
    }
}
