using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class SwipeManager : MonoBehaviour
{
    [Header("Window Reference")]
    public RectTransform window; 
    public float minSwipeDistance = 100f; // min swipe distance in pixels

    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    private Dictionary<int, Vector2> touchStartPositions = new Dictionary<int, Vector2>();

    void Awake()
    {
        raycaster = FindObjectOfType<GraphicRaycaster>();
        eventSystem = FindObjectOfType<EventSystem>();

        if (raycaster == null)
            Debug.LogError("GraphicRaycaster not found in scene!");
    }

    void Update()
    {
        HandleMouseInput();
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
            touchStartPositions[0] = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            if (touchStartPositions.ContainsKey(0))
            {
                HandleSwipe(touchStartPositions[0], Input.mousePosition);
                touchStartPositions.Remove(0);
            }
        }
    }

    private void HandleTouchInput()
    {
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
                touchStartPositions[touch.fingerId] = touch.position;

            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (touchStartPositions.TryGetValue(touch.fingerId, out Vector2 startPos))
                {
                    HandleSwipe(startPos, touch.position);
                    touchStartPositions.Remove(touch.fingerId);
                }
            }
        }
    }

    private void HandleSwipe(Vector2 start, Vector2 end)
    {
        Vector2 swipe = end - start;
        if (swipe.magnitude < minSwipeDistance) return;

        PointerEventData pointerData = new PointerEventData(eventSystem) { position = start };
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (var result in results)
        {
            Person person = result.gameObject.GetComponent<Person>();
            if (person == null) continue;

            // ✅ Determine if swipe is opposite to running direction
            bool isLeftOfWindow = person.transform.position.x < window.position.x;

            // Swipe **against** the movement direction
            if ((swipe.x < 0 && isLeftOfWindow) || (swipe.x > 0 && !isLeftOfWindow))
            {
                person.SwipeBack();
                break; // only swipe one person per input
            }
        }
    }
}
