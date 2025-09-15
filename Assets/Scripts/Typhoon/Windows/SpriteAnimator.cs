using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 10f;

    private Image image;
    private int currentFrame;
    private float timer;
    private bool isPaused = false;

    private void OnEnable() => SidePanelController.OnPauseStateChanged += OnPauseStateChanged;
    private void OnDisable() => SidePanelController.OnPauseStateChanged -= OnPauseStateChanged;

    private void OnPauseStateChanged(bool paused) => isPaused = paused;

    void Start() => image = GetComponent<Image>();

    void Update()
    {
        if (isPaused || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;
            currentFrame = (currentFrame + 1) % frames.Length;
            image.sprite = frames[currentFrame];
        }
    }
}
