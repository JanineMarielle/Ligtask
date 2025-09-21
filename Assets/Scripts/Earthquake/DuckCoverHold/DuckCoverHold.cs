using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DuckCoverHold : MonoBehaviour
{
    [Header("References")]
    public DuckCoverHoldAnimator animator; // Reference to animation script
    public float shakeDuration = 2f;       // Time allowed to respond
    public int pointsLost = 10;

    [Header("UI")]
    public Text scoreText;

    private int score = 100;
    private bool isShaking = false;
    private bool isHolding = false;

    void Start()
    {
        UpdateScore();
        StartCoroutine(ShakeScreenRoutine());
    }

    void Update()
    {
        if (!isShaking) return;

        // Detect duck/cover (tap)
        if (Input.GetMouseButtonDown(0))
        {
            animator.DuckCover(); // Show duck & cover animation
            isHolding = true;
        }

        // Detect holding (finger stays on screen)
        if (Input.GetMouseButton(0) && isHolding)
        {
            animator.Hold(); // Show hold frame
        }

        // Detect swipe
        if (Input.GetMouseButtonUp(0))
        {
            float swipeDelta = Input.mousePosition.x - animator.startX;
            if (swipeDelta > 50f) // Swipe right threshold
            {
                animator.Run(); // Run animation
                isShaking = false; // End mini-game
            }
            else if (swipeDelta < -50f) // Swipe left
            {
                LosePoints("Swiped left!");
            }
        }
    }

    IEnumerator ShakeScreenRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f)); // Random shake start
        isShaking = true;
        Debug.Log("Screen shaking! Duck, Cover & Hold!");

        float timer = 0f;
        while (timer < shakeDuration)
        {
            if (isHolding)
            {
                yield break; // Success
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // If time ran out and player didn't hold
        LosePoints("Failed to Duck & Hold in time!");
    }

    private void LosePoints(string reason)
    {
        score -= pointsLost;
        UpdateScore();
        Debug.Log(reason + " - Lost " + pointsLost + " points.");
    }

    private void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
}
