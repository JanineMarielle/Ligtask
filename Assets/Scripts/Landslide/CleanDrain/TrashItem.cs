using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrashItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;

    public CleanDrainManager gameManager;
    public RectTransform dropZone;

    public int roundIndex;
    public System.Action<int> OnCollected;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        mainCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / mainCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (IsInsideDropZone())
        {
            OnCollected?.Invoke(roundIndex);
            Destroy(gameObject);
        }
    }

    bool IsInsideDropZone()
    {
        Rect trashRect = GetWorldRect(rectTransform);
        Rect zoneRect = GetWorldRect(dropZone);
        return trashRect.Overlaps(zoneRect);
    }

    Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Rect(
            corners[0].x,
            corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y
        );
    }
}
