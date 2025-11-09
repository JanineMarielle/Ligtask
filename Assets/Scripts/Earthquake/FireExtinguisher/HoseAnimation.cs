using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HoseAnimation : MonoBehaviour
{
    [Header("Hose Animation Settings")]
    public Sprite hoseIdleSprite;
    public Sprite[] hoseSprayFrames;
    public float frameRate = 0.1f;

    private Image image;
    private int currentFrame = 0;
    private float timer;
    private bool spraying = false;

    void Awake()
    {
        image = GetComponent<Image>();
        image.sprite = hoseIdleSprite;
    }

    void Update()
    {
        if (!spraying || hoseSprayFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % hoseSprayFrames.Length;
            image.sprite = hoseSprayFrames[currentFrame];
        }
    }

    public void StartSpraying()
    {
        spraying = true;
        currentFrame = 0;
    }

    public void StopSpraying()
    {
        spraying = false;
        image.sprite = hoseIdleSprite;
    }
}
