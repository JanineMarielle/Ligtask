using UnityEngine;
using System;

public class Person : MonoBehaviour
{
    public static event Action<Person> OnPersonDestroyed;

    private RectTransform rect;
    private Vector2 moveDir;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 200f;

    private bool swiped = false;
    private bool saved = false;
    private bool processed = false; // prevents double counting

    private RectTransform window;
    private bool allowOverlapping = false;
    private Action onDone;

    [Header("Lifetime Settings")]
    [SerializeField] private float maxLifetime = 10f;
    private float lifetime;

    public event Action<Person> onSaved;
    public event Action<Person> onFailed;

    private bool isPaused = false;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        SidePanelController.OnPauseStateChanged += OnPauseStateChanged;
    }

    private void OnDestroy()
    {
        OnPersonDestroyed?.Invoke(this);
    }

    private void OnPauseStateChanged(bool paused)
    {
        isPaused = paused;
    }

    private void Update()
    {
        if (isPaused) return;

        rect.anchoredPosition += moveDir * speed * Time.deltaTime;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            FailIfNotProcessed();
            FinishAndDestroy();
            return;
        }

        if (!allowOverlapping && RectOverlaps(rect, window))
        {
            FailIfNotProcessed();
            FinishAndDestroy();
            return;
        }

        CheckOutOfBounds();
    }

    public void SwipeBack()
    {
        if (swiped) return;
        swiped = true;

        moveDir = -moveDir;
        speed *= 5f;

        Vector3 scale = rect.localScale;
        scale.x = -scale.x;
        rect.localScale = scale;

        // ✅ Immediately mark as saved
        if (!saved && !processed)
        {
            saved = true;
            processed = true;
            onSaved?.Invoke(this);
            Debug.Log($"[Person] Swiped and saved: {name}");
        }
    }

    private void CheckOutOfBounds()
    {
        float screenWidth = ((RectTransform)rect.parent).rect.width / 2f;

        // Always destroy after leaving screen, but saving already counted
        if (Mathf.Abs(rect.anchoredPosition.x) > screenWidth + 50f)
        {
            FinishAndDestroy();
        }
    }

    private void FailIfNotProcessed()
    {
        if (processed) return;
        processed = true;
        onFailed?.Invoke(this);
    }

    public void Finish() => onDone?.Invoke();

    private void FinishAndDestroy()
    {
        Finish();
        Destroy(gameObject);
    }

    private bool RectOverlaps(RectTransform a, RectTransform b)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(b, a.position);
    }

    public void Init(RectTransform windowTransform, bool allowOverlapping, Action onDone)
    {
        window = windowTransform;
        this.allowOverlapping = allowOverlapping;
        this.onDone = onDone;

        float directionX = window.anchoredPosition.x > rect.anchoredPosition.x ? 1f : -1f;
        moveDir = new Vector2(directionX, 0f);

        Vector3 scale = rect.localScale;
        scale.x = Mathf.Abs(scale.x) * (directionX > 0 ? 1 : -1);
        rect.localScale = scale;

        lifetime = maxLifetime;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
}
