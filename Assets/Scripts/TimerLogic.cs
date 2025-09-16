using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TimerLogic : MonoBehaviour
{
    [Header("UI References")]
    public Slider timerSlider;
    public TextMeshProUGUI timerText;

    [Header("Padding Settings")]
    public float leftPadding = 200f;
    public float rightPadding = 280f;
    public float topPadding = 20f;
    public float textRightOffset = 10f;

    public event Action OnTimerFinished;

    private float duration;
    private float timer;
    private bool isRunning = false;
    private bool isPaused = false; // auto pause flag

    private void OnEnable()
    {
        SidePanelController.OnPauseStateChanged += OnPauseStateChanged;
    }

    private void OnDisable()
    {
        SidePanelController.OnPauseStateChanged -= OnPauseStateChanged;
    }

    private void OnPauseStateChanged(bool paused)
    {
        isPaused = paused;
    }

    void Awake()
    {
        // Apply slider padding
        if (timerSlider != null)
        {
            RectTransform rt = timerSlider.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(leftPadding, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-rightPadding, rt.offsetMax.y);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -topPadding);
        }

        // Position timerText at right of slider (local space, not world)
        if (timerSlider != null && timerText != null)
        {
            RectTransform sliderRT = timerSlider.GetComponent<RectTransform>();
            RectTransform textRT = timerText.GetComponent<RectTransform>();

            // Anchor text to the right-middle of the slider
            textRT.SetParent(sliderRT);
            textRT.anchorMin = new Vector2(1f, 0.5f);
            textRT.anchorMax = new Vector2(1f, 0.5f);
            textRT.pivot = new Vector2(0f, 0.5f);

            // Apply local offset
            textRT.anchoredPosition = new Vector2(textRightOffset, 0f);
        }

    }

    public void StartTimer(float newDuration)
    {
        if (newDuration <= 0f)
        {
            Debug.LogError("[Timer] Invalid duration!");
            return;
        }

        duration = newDuration;
        timer = 0f;
        isRunning = true;

        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
            timerSlider.value = 1f;
        }

        UpdateTimerText();
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void Update()
    {
        if (!isRunning || isPaused) return; // respect pause

        timer += Time.deltaTime;

        if (timerSlider != null)
            timerSlider.value = Mathf.Clamp01(1f - (timer / duration));

        UpdateTimerText();

        if (timer >= duration)
        {
            isRunning = false;
            OnTimerFinished?.Invoke();
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(duration - timer));
        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;

        timerText.text = (minutes > 0) ? $"Time: {minutes:D2}:{seconds:D2}" : $"{seconds:D2}";
    }
}
