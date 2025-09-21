using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HammerAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 10f;

    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        if (frames.Length > 0)
            image.sprite = frames[0]; // idle frame
    }

    // Call this to play the animation once
    public float PlayOnce()
    {
        if (frames.Length == 0) return 0f;

        StopAllCoroutines();
        StartCoroutine(AnimateOnce());
        return frames.Length / framesPerSecond; // return total animation duration
    }

    private IEnumerator AnimateOnce()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            image.sprite = frames[i];
            yield return new WaitForSeconds(1f / framesPerSecond);
        }

        image.sprite = frames[0]; // return to idle
    }
}
