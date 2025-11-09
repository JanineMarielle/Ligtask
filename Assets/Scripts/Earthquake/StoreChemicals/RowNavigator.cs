using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RowNavigator : MonoBehaviour
{
    [Header("References")]
    public RectTransform cabinetImage;
    public Button nextRowButton;
    public Button previousRowButton;

    [Header("Row Targets")]
    [Tooltip("Assign the RectTransforms representing the rows, e.g. element 0, 5, and 10.")]
    public List<RectTransform> rowElements; // Should contain elements 0, 5, and 10

    [Header("Settings")]
    public float transitionDuration = 0.5f;
    public float initialZoomScale = 1.2f;
    public float zoomDuration = 0.8f;

    private int currentRow = 0;
    private Vector3 originalCabinetScale;
    private Vector2 originalCabinetPos;

    private void Awake()
    {
        originalCabinetScale = cabinetImage.localScale;
        originalCabinetPos = cabinetImage.anchoredPosition;

        nextRowButton.interactable = false;
        previousRowButton.interactable = false;

        nextRowButton.onClick.AddListener(NextRow);
        previousRowButton.onClick.AddListener(PreviousRow);
    }

    public void StartZoom()
    {
        StartCoroutine(InitialZoomTop());
    }

    private IEnumerator InitialZoomTop()
    {
        Vector3 startScale = originalCabinetScale;
        Vector3 endScale = originalCabinetScale * initialZoomScale;

        Vector2 startPos = cabinetImage.anchoredPosition;
        Vector2 endPos = originalCabinetPos;

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            float t = elapsed / zoomDuration;
            cabinetImage.localScale = Vector3.Lerp(startScale, endScale, t);
            cabinetImage.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cabinetImage.localScale = endScale;
        cabinetImage.anchoredPosition = endPos;

        UpdateButtonInteractable();
        StartCoroutine(AnimateCabinetToRow(currentRow));
    }

    public void NextRow()
    {
        if (currentRow < rowElements.Count - 1)
        {
            currentRow++;
            StartCoroutine(AnimateCabinetToRow(currentRow));
            UpdateButtonInteractable();
        }
    }

    public void PreviousRow()
    {
        if (currentRow > 0)
        {
            currentRow--;
            StartCoroutine(AnimateCabinetToRow(currentRow));
            UpdateButtonInteractable();
        }
    }

    private void UpdateButtonInteractable()
    {
        previousRowButton.interactable = currentRow > 0;
        nextRowButton.interactable = currentRow < rowElements.Count - 1;
    }

    private IEnumerator AnimateCabinetToRow(int targetRow)
    {
        if (targetRow < 0 || targetRow >= rowElements.Count)
            yield break;

        Vector2 startPos = cabinetImage.anchoredPosition;

        // Get the Y position of the target element relative to the cabinet parent
        float targetY = -rowElements[targetRow].anchoredPosition.y;
        Vector2 endPos = new Vector2(originalCabinetPos.x, targetY);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float t = elapsed / transitionDuration;
            cabinetImage.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cabinetImage.anchoredPosition = endPos;
    }
}
