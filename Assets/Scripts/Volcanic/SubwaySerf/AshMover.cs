using UnityEngine;

public class AshMover : MonoBehaviour
{
    public float fallSpeed = 350f;
    public LaneDodgerController controller;
    public RectTransform rect;

    void Update()
    {
        if (rect == null) return;

        rect.anchoredPosition -= new Vector2(0, fallSpeed * Time.deltaTime);

        // If ash passed below screen, delete it
        if (rect.anchoredPosition.y < -Screen.height)
        {
            controller.RemoveAsh(rect);
            Destroy(gameObject);
        }
    }
}
