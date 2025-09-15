using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BucketAnimation : MonoBehaviour
{
    [Header("UI Image Target")]
    public Image bucketImage; // Assign your UI Image here

    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite[] startPourFrames;
    public Sprite[] pouringFrames;
    public Sprite[] endPourFrames;

    [Header("Timings")]
    public float frameRate = 0.1f; // time between frames

    private Coroutine animationRoutine;
    private bool isPouring = false;

    void Start()
    {
        bucketImage.sprite = idleSprite; // start at idle
    }

    public void StartPour()
    {
        if (isPouring) return;
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(PourRoutine());
    }

    public void StopPour()
    {
        if (!isPouring) return;
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(EndPourRoutine());
    }

    private IEnumerator PourRoutine()
    {
        isPouring = true;

        // play startPour frames
        for (int i = 0; i < startPourFrames.Length; i++)
        {
            bucketImage.sprite = startPourFrames[i];
            yield return new WaitForSeconds(frameRate);
        }

        // loop pouring frames until StopPour is called
        while (isPouring)
        {
            for (int i = 0; i < pouringFrames.Length; i++)
            {
                bucketImage.sprite = pouringFrames[i];
                yield return new WaitForSeconds(frameRate);
            }
        }
    }

    private IEnumerator EndPourRoutine()
    {
        isPouring = false;

        // play endPour frames
        for (int i = 0; i < endPourFrames.Length; i++)
        {
            bucketImage.sprite = endPourFrames[i];
            yield return new WaitForSeconds(frameRate);
        }

        // return to idle
        bucketImage.sprite = idleSprite;
    }
}
