using UnityEngine;

public class LeafController : MonoBehaviour
{
    private RectTransform rt;
    private bool isMoving = false;
    private Vector2 startPos;
    private Vector2 targetPos;
    private float moveDuration = 0.5f;
    private float elapsedTime;

    private float startAngle;
    private float targetAngle;

    private RectTransform canvasRect;
    private SwipeLeafManager manager;

    [Header("Off-Screen Settings")]
    public float padding = 50f; // extra padding outside screen

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        manager = FindObjectOfType<SwipeLeafManager>();
    }

    void Update()
    {
        // ✅ Continuously check if leaf is fully off-screen
        if (!isMoving && IsOffScreen())
        {
            // Notify manager before destroying
            if (manager != null)
            {
                manager.NotifyLeafCleared();
            }

            Destroy(gameObject);
        }
    }

    public void Push(Vector2 direction, float distance)
    {
        if (!isMoving)
        {
            startPos = rt.anchoredPosition;
            targetPos = startPos + direction.normalized * distance;
            elapsedTime = 0f;

            // Starting rotation
            startAngle = rt.eulerAngles.z;

            // Add random rotation for "tumble"
            float spinAmount = Random.Range(-30f, 30f); 
            targetAngle = startAngle + spinAmount;

            StartCoroutine(SlideToTarget());
        }
    }

    private System.Collections.IEnumerator SlideToTarget()
    {
        isMoving = true;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            float angle = Mathf.LerpAngle(startAngle, targetAngle, t);
            rt.rotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        rt.anchoredPosition = targetPos;
        rt.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        isMoving = false;
    }

    private bool IsOffScreen()
    {
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        // Expand canvas rect by padding
        Rect canvasBounds = new Rect(
            canvasCorners[0].x - padding,
            canvasCorners[0].y - padding,
            (canvasCorners[2].x - canvasCorners[0].x) + padding * 2,
            (canvasCorners[2].y - canvasCorners[0].y) + padding * 2
        );

        Vector3[] leafCorners = new Vector3[4];
        rt.GetWorldCorners(leafCorners);

        // ✅ If ANY corner is still inside, leaf stays alive
        foreach (var corner in leafCorners)
        {
            if (canvasBounds.Contains(corner))
                return false;
        }

        // All corners outside → destroy
        return true;
    }
}
