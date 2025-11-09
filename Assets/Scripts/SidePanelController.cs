using UnityEngine;
using UnityEngine.UI;
using System; // Needed for Action

public class SidePanelController : MonoBehaviour
{
    [Header("References")]
    public RectTransform sidePanel;
    public GameObject backgroundBlocker;
    public RectTransform menuButton;

    [Header("Settings")]
    public float slideSpeed = 800f;     
    public Vector2 buttonPadding = new Vector2(20f, -20f); // X = right padding, Y = down padding

    private bool isOpen = false;
    private Vector2 targetPosition;
    private float panelWidth;
    private int lastScreenWidth;
    private int lastScreenHeight;

    // --- PAUSE SIGNAL ---
    public static event Action<bool> OnPauseStateChanged; 
    // true = pause, false = resume

    void Start()
    {
        UpdatePanelWidth();
        UpdateMenuButtonPosition();

        targetPosition = new Vector2(-panelWidth, sidePanel.anchoredPosition.y);
        sidePanel.anchoredPosition = targetPosition;

        if (backgroundBlocker != null)
            backgroundBlocker.SetActive(false);
    }

    void Update()
    {
        sidePanel.anchoredPosition = Vector2.MoveTowards(
            sidePanel.anchoredPosition,
            targetPosition,
            slideSpeed * Time.deltaTime
        );

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdatePanelWidth();
            UpdateMenuButtonPosition();

            if (isOpen)
                targetPosition = new Vector2(0, sidePanel.anchoredPosition.y);
            else
                targetPosition = new Vector2(-panelWidth, sidePanel.anchoredPosition.y);
        }
    }

    private void UpdatePanelWidth()
    {
        // Get the canvas scaler if available
        Canvas canvas = sidePanel.GetComponentInParent<Canvas>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        float scaleFactor = 1f;

        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // Calculate scaling based on reference resolution
            float logWidth = Mathf.Log(Screen.width / scaler.referenceResolution.x, 2);
            float logHeight = Mathf.Log(Screen.height / scaler.referenceResolution.y, 2);
            float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight);
            scaleFactor = Mathf.Pow(2, logWeightedAverage);
        }

        // Adjust width using the scale factor
        float newWidth = (Screen.width * 0.3f) / scaleFactor;
        sidePanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        panelWidth = newWidth;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void UpdateMenuButtonPosition()
    {
        if (menuButton != null)
        {
            menuButton.anchorMin = new Vector2(0, 1);
            menuButton.anchorMax = new Vector2(0, 1);
            menuButton.pivot = new Vector2(0, 1);

            menuButton.anchoredPosition = buttonPadding;
        }
    }

    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        targetPosition = new Vector2(0, sidePanel.anchoredPosition.y);
        isOpen = true;

        if (backgroundBlocker != null)
            backgroundBlocker.SetActive(true);

        // Trigger pause signal
        OnPauseStateChanged?.Invoke(true);
    }

    public void ClosePanel()
    {
        targetPosition = new Vector2(-panelWidth, sidePanel.anchoredPosition.y);
        isOpen = false;

        if (backgroundBlocker != null)
            backgroundBlocker.SetActive(false);

        // Trigger resume signal
        OnPauseStateChanged?.Invoke(false);
    }
}
