using UnityEngine;
using System.Collections;

public class TapToCont : MonoBehaviour
{
    [Header("Tap To Continue UI")]
    public CanvasGroup tapToContinue;

    private Coroutine fadeRoutine;

    void Awake()
    {
        if (tapToContinue != null)
            tapToContinue.alpha = 0;
    }

    public void ShowTapToContinue()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeLoop());
    }

    public void HideTapToContinue()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (tapToContinue != null)
            tapToContinue.alpha = 0;
    }

    IEnumerator FadeLoop()
    {
        while (true)
        {
            // Fade in
            for (float a = 0; a <= 1; a += Time.deltaTime)
            {
                tapToContinue.alpha = a;
                yield return null;
            }

            // Fade out
            for (float a = 1; a >= 0; a -= Time.deltaTime)
            {
                tapToContinue.alpha = a;
                yield return null;
            }
        }
    }
}
