using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WindowController : MonoBehaviour
{
    [Header("UI Elements")]
    public Image xMark; // assign in inspector
    public RectTransform windowRect; // assign your window rect here
    public float overlapBuffer = 10f; // allowed overlap before collision

    private void Start()
    {
        if (xMark != null)
            xMark.enabled = false;
    }

    private void Update()
    {
        Person[] people = FindObjectsOfType<Person>();
        foreach (var person in people)
        {
            RectTransform personRect = person.GetComponent<RectTransform>();
            if (personRect != null && windowRect != null)
            {
                if (IsColliding(personRect, windowRect))
                {
                    FlashX();
                    person.Finish();
                    Destroy(person.gameObject, 0.1f);
                }
            }
        }
    }

    private bool IsColliding(RectTransform person, RectTransform window)
{
    // Get world corners of person and window
    Vector3[] personCorners = new Vector3[4];
    Vector3[] windowCorners = new Vector3[4];
    person.GetWorldCorners(personCorners);
    window.GetWorldCorners(windowCorners);

    bool fromLeft = person.position.x < window.position.x;

    // Left spawn: personCorners[2].x is right edge
    // Right spawn: personCorners[0].x is left edge
    float personEdge = fromLeft ? personCorners[2].x : personCorners[0].x;
    float windowEdge = fromLeft ? windowCorners[0].x : windowCorners[2].x;

    // Adjust overlap: subtract half the person width to prevent early collision
    float personHalfWidth = (personCorners[2].x - personCorners[0].x) / 2f;
    float effectiveBuffer = overlapBuffer;

    if (fromLeft)
    {
        // Trigger collision only when the **center** of person + buffer reaches window edge
        return (personEdge - personHalfWidth + effectiveBuffer) >= windowEdge;
    }
    else
    {
        return (personEdge + personHalfWidth - effectiveBuffer) <= windowEdge;
    }
}

    public void FlashX()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (xMark != null)
        {
            xMark.enabled = true;
            yield return new WaitForSeconds(0.5f);
            xMark.enabled = false;
        }
    }
}
