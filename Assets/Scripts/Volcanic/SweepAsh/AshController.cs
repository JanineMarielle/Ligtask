using UnityEngine;

public class AshController : MonoBehaviour
{
    private RectTransform rt;
    private bool isMoving = false;
    private Vector2 startPos;
    private Vector2 targetPos;
    private float moveDuration = 0.5f;
    private float elapsedTime;

    private float startAngle;
    private float targetAngle;

    private Vector2 lastPushDir; 
    private float lastPushDistance; 

    private RectTransform canvasRect;

    [Header("Off-Screen Settings")]
    public float padding = 2f;

    [Header("Settle Settings")]
    public float settleOffset = 20f;

    [HideInInspector]
    public SwipeAshManager swipeManager;

    public bool IsSettled { get; private set; } = false;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();

        // Auto-detect SwipeAshManager in the scene
        if (swipeManager == null)
        {
            swipeManager = FindObjectOfType<SwipeAshManager>();
            if (swipeManager == null)
            {
                Debug.LogWarning("SwipeAshManager not found in scene!");
            }
        }
    }

    void Update()
    {
        if (!isMoving && !IsSettled && IsOffScreen())
        {
            Destroy(gameObject);
            return;
        }

        // Only settle if not already settled
        if (!IsSettled && swipeManager != null && swipeManager.dustpanRect != null)
        {
            if (IsOverDustpan(swipeManager.dustpanRect))
                Settle(swipeManager.dustpanRect);
        }
    }

    public void Push(Vector2 direction, float distance)
    {
        if (isMoving || IsSettled) return;

        lastPushDir = direction.normalized;
        lastPushDistance = distance;

        startPos = rt.anchoredPosition;
        targetPos = startPos + lastPushDir * distance;
        elapsedTime = 0f;

        startAngle = rt.eulerAngles.z;
        float spinAmount = Random.Range(-30f, 30f);
        targetAngle = startAngle + spinAmount;

        StartCoroutine(SlideToTarget());
    }

    private System.Collections.IEnumerator SlideToTarget()
    {
        isMoving = true;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / moveDuration);

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            float angle = Mathf.LerpAngle(startAngle, targetAngle, t);
            rt.rotation = Quaternion.Euler(0f, 0f, angle);

            // Check if it entered the dustpan mid-slide
            if (!IsSettled && swipeManager != null && swipeManager.dustpanRect != null)
            {
                if (IsOverDustpan(swipeManager.dustpanRect))
                {
                    Settle(swipeManager.dustpanRect);
                    yield break; // stop moving immediately
                }
            }

            yield return null;
        }

        rt.anchoredPosition = targetPos;
        rt.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        isMoving = false;
    }

    public bool IsOverDustpan(RectTransform dustpan)
    {
        if (swipeManager == null) return false;

        // Convert ash position to dustpan local space
        Vector3 localPos = dustpan.InverseTransformPoint(rt.position);

        Rect paddedRect = new Rect(
            dustpan.rect.xMin + swipeManager.dustpanPaddingLeft,
            dustpan.rect.yMin + swipeManager.dustpanPaddingBottom,
            dustpan.rect.width - swipeManager.dustpanPaddingLeft - swipeManager.dustpanPaddingRight,
            dustpan.rect.height - swipeManager.dustpanPaddingTop - swipeManager.dustpanPaddingBottom
        );

        return paddedRect.Contains(localPos);
    }

    public void Settle(RectTransform dustpan)
    {
        if (IsSettled) return;

        IsSettled = true;
        isMoving = false;

        // Convert ash position to dustpan local space
        Vector3 localPos = dustpan.InverseTransformPoint(rt.position);

        // Clamp inside padded dustpan rect
        Rect paddedRect = new Rect(
            dustpan.rect.xMin + swipeManager.dustpanPaddingLeft,
            dustpan.rect.yMin + swipeManager.dustpanPaddingBottom,
            dustpan.rect.width - swipeManager.dustpanPaddingLeft - swipeManager.dustpanPaddingRight,
            dustpan.rect.height - swipeManager.dustpanPaddingTop - swipeManager.dustpanPaddingBottom
        );

        // Push a bit further along last swipe direction
        float extraDistance = Random.Range(10f, 30f); // tweak these values
        Vector3 extraOffset = new Vector3(lastPushDir.x, lastPushDir.y, 0f) * extraDistance;

        Vector3 worldPos = dustpan.TransformPoint(localPos) + extraOffset;

        // Clamp again in world space to not overshoot
        Vector3[] dpCorners = new Vector3[4];
        dustpan.GetWorldCorners(dpCorners);

        worldPos.x = Mathf.Clamp(worldPos.x,
            dpCorners[0].x + swipeManager.dustpanPaddingLeft,
            dpCorners[2].x - swipeManager.dustpanPaddingRight);
        worldPos.y = Mathf.Clamp(worldPos.y,
            dpCorners[0].y + swipeManager.dustpanPaddingBottom,
            dpCorners[2].y - swipeManager.dustpanPaddingTop);

        rt.position = worldPos;

        swipeManager?.NotifyAshCleared();
    }

    private bool IsOffScreen()
    {
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        Rect canvasBounds = new Rect(
            canvasCorners[0].x - padding,
            canvasCorners[0].y - padding,
            (canvasCorners[2].x - canvasCorners[0].x) + padding * 2,
            (canvasCorners[2].y - canvasCorners[0].y) + padding * 2
        );

        Vector3[] ashCorners = new Vector3[4];
        rt.GetWorldCorners(ashCorners);

        foreach (var corner in ashCorners)
        {
            if (canvasBounds.Contains(corner))
                return false;
        }

        return true;
    }
}
