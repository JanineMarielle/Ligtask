using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FireAnimation : MonoBehaviour
{
    [Header("Fire Animation Settings")]
    public Sprite[] fireFrames;        // frames for fire animation
    public float frameRate = 0.15f;    // seconds per frame

    private Image image;
    private int currentFrame = 0;
    private float timer;

    void Awake()
    {
        image = GetComponent<Image>();
        if (fireFrames.Length > 0)
            image.sprite = fireFrames[0];
    }

    void Update()
    {
        if (fireFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % fireFrames.Length;
            image.sprite = fireFrames[currentFrame];
        }
    }
}
