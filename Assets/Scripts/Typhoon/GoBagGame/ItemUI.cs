using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GoBagItemSO itemData;           
    private GoBagGameManager gameManager;   
    private Vector2 originalAnchoredPos;    
    private RectTransform rectTransform;
    private Image itemImage;

    private Vector2 dragStartPos;

    public GoBagItemSO ItemData => itemData;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        itemImage = GetComponent<Image>();
        if (itemImage == null)
            Debug.LogError("ItemUI requires an Image component!");
    }

    public void Setup(GoBagItemSO data, GoBagGameManager manager)
    {
        itemData = data;
        gameManager = manager;

        originalAnchoredPos = rectTransform.anchoredPosition;

        rectTransform.anchoredPosition = originalAnchoredPos;

        if (itemData != null && itemImage != null)
            itemImage.sprite = itemData.itemSprite;

        gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float swipeDistance = eventData.position.y - dragStartPos.y;

        if (swipeDistance > 100f)
        {
            rectTransform.anchoredPosition = new Vector2(originalAnchoredPos.x, originalAnchoredPos.y + 1000f);
            gameManager.OnItemSwiped(this, true);
        }
        else if (swipeDistance < -100f) 
        {
            rectTransform.anchoredPosition = new Vector2(originalAnchoredPos.x, originalAnchoredPos.y - 1000f);
            gameManager.OnItemSwiped(this, false);
        }
        else
        {
            rectTransform.anchoredPosition = originalAnchoredPos;
        }
    }
}
