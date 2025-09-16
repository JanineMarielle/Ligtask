using UnityEngine;

public class DebrisMover : MonoBehaviour
{
    private float speed;
    private RectTransform rt;
    private AvoidObstacleManager manager;
    private bool hasReported = false; // ensure dodged is reported only once
    private bool isPaused = false; // new pause flag

    // Optional: allow random variation (e.g., 0.8x to 1.2x of base speed)
    private const float speedVariationMin = 0.8f;
    private const float speedVariationMax = 1.2f;

    public void Init(float baseFallSpeed, AvoidObstacleManager mgr, bool randomizeSpeed = false)
    {
        speed = randomizeSpeed 
            ? baseFallSpeed * Random.Range(speedVariationMin, speedVariationMax) 
            : baseFallSpeed;

        manager = mgr;
    }

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt == null)
            Debug.LogError($"[DebrisMover] No RectTransform found on {gameObject.name}!");
    }

    void OnEnable()
    {
        SidePanelController.OnPauseStateChanged += HandlePause;
    }

    void OnDisable()
    {
        SidePanelController.OnPauseStateChanged -= HandlePause;
    }

    private void HandlePause(bool paused)
    {
        isPaused = paused;
    }

    void Update()
    {
        if (rt == null) return; // safety

        if (!isPaused) // only move debris if not paused
        {
            // Move debris down
            rt.anchoredPosition += Vector2.down * speed * Time.deltaTime;

            if (manager != null && !hasReported)
            {
                // Check collision with player
                if (manager.PlayerCollides(rt))
                {
                    Debug.Log($"[Collision] Player hit by {gameObject.name} at world pos {rt.position}");
                    manager.OnPlayerHit();
                    hasReported = true;
                    Destroy(gameObject);
                    return;
                }

                // Check if debris has safely left the screen → dodged
                if (rt.anchoredPosition.y < -Screen.height / 2f - 200f)
                {
                    manager.OnDebrisDodged();
                    hasReported = true;
                    Destroy(gameObject);
                    return;
                }
            }

            // Safety: destroy debris if somehow goes way off-screen
            if (rt.anchoredPosition.y < -Screen.height - 500f)
            {
                Destroy(gameObject);
            }
        }
    }
}
