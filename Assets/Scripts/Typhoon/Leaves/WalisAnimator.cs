using UnityEngine;
using System.Collections;

public class WalisAnimator : MonoBehaviour
{
    public RectTransform walisImage; // assign your Walis UI here
    public float swingAngle = 45f;   // how much to swing
    public float swingDuration = 0.15f; // swing speed
    public float returnDuration = 0.2f; // return speed

    private Quaternion defaultRotation;

    private void Start()
    {
        if (walisImage == null)
            walisImage = GetComponent<RectTransform>();

        defaultRotation = walisImage.localRotation;
    }

    public void PlaySwingAnimation(Vector2 swipeDir)
    {
        // Check horizontal direction of swipe
        if (swipeDir.x > 0)
        {
            StopAllCoroutines();
            StartCoroutine(SwingRoutine(-swingAngle)); // swipe right → swing clockwise
        }
        else if (swipeDir.x < 0)
        {
            StopAllCoroutines();
            StartCoroutine(SwingRoutine(swingAngle)); // swipe left → swing counter-clockwise
        }
    }

    private IEnumerator SwingRoutine(float targetAngle)
    {
        Quaternion startRot = defaultRotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetAngle);

        float elapsed = 0;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            walisImage.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        // Return to upright
        elapsed = 0;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            walisImage.localRotation = Quaternion.Lerp(endRot, defaultRotation, t);
            yield return null;
        }

        walisImage.localRotation = defaultRotation;
    }
}
