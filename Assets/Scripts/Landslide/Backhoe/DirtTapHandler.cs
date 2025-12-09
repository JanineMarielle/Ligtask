using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DirtTapHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    [Tooltip("Radius around tap position to clear dirt")]
    public float clearRadius = 120f;

    [Tooltip("Time per frame for backhoe animation")]
    public float backhoeAnimFrameRate = 0.05f;

    [Header("References")]
    public Canvas dirtCanvas;                // Dirt canvas (all dirt are children)
    public RectTransform backhoeParent;      // Parent RectTransform for backhoe animation
    public Sprite[] backhoeSprites;          // Animation frames for backhoe
    public RectTransform houseUI;            // House to detect when fully uncovered

    private bool canTap = true; // Prevent overlapping animations
    private bool houseFound = false; // House found flag

    void Update()
    {
        // Mouse input for testing in Editor
        if (Input.GetMouseButtonDown(0))
        {
            HandleTap(Input.mousePosition);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleTap(eventData.position);
    }

    void HandleTap(Vector2 screenPosition)
    {
        if (!canTap || houseFound) return;

        // Convert to backhoe parent position
        Vector2 backhoePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backhoeParent,
            screenPosition,
            dirtCanvas.worldCamera,
            out backhoePos
        );

        // Convert to dirtCanvas position
        Vector2 dirtPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dirtCanvas.transform as RectTransform,
            screenPosition,
            dirtCanvas.worldCamera,
            out dirtPos
        );

        StartCoroutine(PlayBackhoeAnimation(backhoePos, dirtPos));
    }

    IEnumerator PlayBackhoeAnimation(Vector2 backhoePos, Vector2 dirtPos)
    {
        canTap = false;

        // Spawn backhoe animation
        GameObject animGO = new GameObject("BackhoeAnim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        animGO.transform.SetParent(backhoeParent, false);
        RectTransform rt = animGO.GetComponent<RectTransform>();
        rt.anchoredPosition = backhoePos;
        rt.sizeDelta = backhoeParent.sizeDelta;

        Image img = animGO.GetComponent<Image>();

        // Animate backhoe
        foreach (Sprite frame in backhoeSprites)
        {
            img.sprite = frame;
            yield return new WaitForSeconds(backhoeAnimFrameRate);
        }

        Destroy(animGO);

        // Clear dirt in radius
        List<GameObject> toRemove = new List<GameObject>();
        foreach (Transform dirt in dirtCanvas.transform)
        {
            RectTransform dirtRT = dirt.GetComponent<RectTransform>();
            if (Vector2.Distance(dirtRT.anchoredPosition, dirtPos) <= clearRadius)
                toRemove.Add(dirt.gameObject);
        }

        foreach (GameObject d in toRemove)
            Destroy(d);

        // --- Check if house is now fully uncovered ---
        if (!houseFound && !IsDirtOverlappingHouse())
        {
            houseFound = true;
            Debug.Log("House found!"); 
            // TODO: Trigger game end or level complete
        }

        canTap = true;
    }

    bool IsDirtOverlappingHouse()
    {
        foreach (Transform dirt in dirtCanvas.transform)
        {
            RectTransform dirtRT = dirt.GetComponent<RectTransform>();

            // Use RectTransform bounds for better coverage
            Vector3[] dirtCorners = new Vector3[4];
            dirtRT.GetWorldCorners(dirtCorners);

            Vector3[] houseCorners = new Vector3[4];
            houseUI.GetWorldCorners(houseCorners);

            // Check if any corner of dirt overlaps house bounds
            for (int i = 0; i < 4; i++)
            {
                if (dirtCorners[i].x >= houseCorners[0].x && dirtCorners[i].x <= houseCorners[2].x &&
                    dirtCorners[i].y >= houseCorners[0].y && dirtCorners[i].y <= houseCorners[2].y)
                {
                    return true; // dirt is still overlapping house
                }
            }
        }
        return false; // no more dirt on house
    }
}
