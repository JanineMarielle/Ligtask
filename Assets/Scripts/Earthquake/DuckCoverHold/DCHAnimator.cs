using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DCHAnimator : MonoBehaviour
{
    [Header("References")]
    public Image characterImage;        // UI Image reference
    public Sprite standFrame;           // Default standing sprite
    public Sprite duckFrame;            // Duck frame
    public Sprite coverFrame;           // Cover frame
    public Sprite holdFrame;            // Hold frame
    public Sprite[] runFrames;
    public float runFrameRate = 0.1f;
    [Range(0.5f, 1f)]
    public float runScaleMultiplier = 0.9f; // Scale down run sprites

    [HideInInspector]
    public float startX;

    private bool isRunning = false;
    private bool hasDucked = false;
    private bool hasCovered = false;
    private Coroutine runCoroutine;
    private Vector3 originalScale;

    void Start()
    {
        startX = Input.mousePosition.x;

        // Initialize with stand sprite
        if (characterImage != null && standFrame != null)
        {
            characterImage.sprite = standFrame;
            originalScale = characterImage.rectTransform.localScale;
        }
    }

    public bool IsRunning => isRunning;

    // Trigger duck frame
    public void Duck()
    {
        if (isRunning || hasDucked) return;

        if (characterImage != null && duckFrame != null)
        {
            characterImage.sprite = duckFrame;
            hasDucked = true;
        }
    }

    // Trigger cover frame
    public void Cover()
    {
        if (isRunning || !hasDucked || hasCovered) return;

        if (characterImage != null && coverFrame != null)
        {
            characterImage.sprite = coverFrame;
            hasCovered = true;
        }
    }

    // Trigger hold frame while touching
    public void Hold(bool holding)
    {
        if (isRunning || !hasCovered) return;

        if (characterImage != null)
        {
            characterImage.sprite = holding ? holdFrame : coverFrame;
        }
    }

    // Run animation loop with smaller, centered sprites
    public void Run()
    {
        if (!isRunning)
        {
            if (runCoroutine != null) StopCoroutine(runCoroutine);
            runCoroutine = StartCoroutine(RunAnimationLoop());
        }
    }

    private IEnumerator RunAnimationLoop()
    {
        if (characterImage != null && standFrame != null)
            characterImage.sprite = standFrame; // Stand sprite before run

        // Reduce size for run animation
        if (characterImage != null)
            characterImage.rectTransform.localScale = originalScale * runScaleMultiplier;

        isRunning = true;
        int index = 0;

        while (isRunning)
        {
            if (characterImage != null && runFrames.Length > 0)
            {
                characterImage.sprite = runFrames[index];
                index = (index + 1) % runFrames.Length;
            }
            yield return new WaitForSeconds(runFrameRate);
        }

        // Reset to stand sprite and original scale after run
        if (characterImage != null && standFrame != null)
        {
            characterImage.sprite = standFrame;
            characterImage.rectTransform.localScale = originalScale;
        }

        ResetDuckCover(); // ready for next round
    }

    // Stop run manually (e.g., after slide)
    public void StopRun()
    {
        isRunning = false;
    }

    // Reset duck and cover for next round
    public void ResetDuckCover()
    {
        hasDucked = false;
        hasCovered = false;

        if (characterImage != null && standFrame != null)
            characterImage.sprite = standFrame;
    }
}
