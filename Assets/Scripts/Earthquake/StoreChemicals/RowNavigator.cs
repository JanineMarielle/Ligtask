using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RowNavigator : MonoBehaviour
{
    [Header("References")]
    public RectTransform cabinetImage;     
    public RectTransform cabinetGrid;      
    public Button nextRowButton;
    public Button previousRowButton;

    [Header("Settings")]
    public int totalRows = 3;
    public float transitionDuration = 0.5f;
    public float initialZoomScale = 1.2f;
    public float zoomDuration = 0.8f;

    private int currentRow = 0;
    private Vector3 originalCabinetScale;
    private Vector2 originalCabinetPos;
    private GridLayoutGroup gridLayout;

    private void Awake()
    {
        // Force pivot to top so zoom starts at row 0
        cabinetImage.pivot = new Vector2(0.5f, 1f);

        originalCabinetScale = cabinetImage.localScale;
        originalCabinetPos = cabinetImage.anchoredPosition;

        gridLayout = cabinetGrid.GetComponent<GridLayoutGroup>();

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
        if (currentRow < totalRows - 1)
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
        nextRowButton.interactable = currentRow < totalRows - 1;
    }

    private float GetRowHeight()
    {
        if (gridLayout != null)
        {
            return gridLayout.cellSize.y + gridLayout.spacing.y;
        }
        return 200f; // fallback
    }

    private IEnumerator AnimateCabinetToRow(int targetRow)
    {
        Vector2 startPos = cabinetImage.anchoredPosition;

        // Calculate actual row height considering zoom
        float scaledRowHeight = GetRowHeight() * cabinetImage.localScale.y;
        Vector2 endPos = originalCabinetPos + new Vector2(0, targetRow * scaledRowHeight);

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
