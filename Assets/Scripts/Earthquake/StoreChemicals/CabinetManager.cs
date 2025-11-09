using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CabinetManager : MonoBehaviour
{
    [Header("Cabinets")]
    public RectTransform currentCabinet;
    public RectTransform nextCabinet;

    [Header("Animation Settings")]
    public float zoomOutScale = 1f;
    public float zoomDuration = 0.5f;
    public float slideDuration = 0.6f;
    public float offscreenX = -1500f;
    public float onscreenX = 0f;
    public float incomingStartX = 1500f;

    [Header("Basket Settings")]
    public RectTransform basketDropZone;
    public RectTransform itemContainer;
    public float basketAnimDuration = 1f;

    [Header("Cabinet Replacement")] 
    public Image cabinetImageToReplace;
    public Sprite replacementCabinetSprite;
    public Sprite finalCabinetSprite;

    // 👇 Add this
    private float fixedY = 0f; // You can adjust this in the Inspector if needed

    private void Awake()
    {
        // You can also automatically capture the current Y position of the cabinet
        if (currentCabinet != null)
            fixedY = currentCabinet.anchoredPosition.y;

        if (nextCabinet != null)
            nextCabinet.anchoredPosition = new Vector2(incomingStartX, fixedY);
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
        Vector2 oldEnd = new Vector2(offscreenX, fixedY);
        Vector2 newStart = new Vector2(incomingStartX, fixedY);
        Vector2 newEnd = new Vector2(onscreenX, fixedY);

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

        // --- Step 4: First replacement ---
        if (cabinetImageToReplace != null && replacementCabinetSprite != null)
        {
            cabinetImageToReplace.sprite = replacementCabinetSprite;
            cabinetImageToReplace.preserveAspect = true;
        }

        // --- Step 5: Wait 1 second, then final replacement ---
        yield return new WaitForSeconds(1f);

        if (cabinetImageToReplace != null && finalCabinetSprite != null)
        {
            cabinetImageToReplace.sprite = finalCabinetSprite;
            cabinetImageToReplace.preserveAspect = true;
        }

        // --- Step 6: Continue end sequence ---
        onFinished?.Invoke();
    }

    private IEnumerator AnimateBasketToCenter()
    {
        if (basketDropZone == null) yield break;

        Vector3 startPos = basketDropZone.position;
        Vector3 targetPos = new Vector3(Screen.width / 2f, Screen.height / 2f, startPos.z);

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

        basketDropZone.position = targetPos;

        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
                child.position = basketDropZone.position + childOffsets[child];
        }

        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
                Destroy(child.gameObject);
        }

        basketDropZone.gameObject.SetActive(false);
    }
}
