using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SealWindows : MonoBehaviour
{
    [Header("Scene Panels")]
    public List<GameObject> backgrounds;
    public Button leftArrow;
    public Button rightArrow;
    public Button backButton;

    [Header("Zoom UI")]
    public GameObject zoomPanel;
    public Image zoomImage;
    public RectTransform tapeContainer;   // Empty container to hold placed tapes
    public GameObject tapePrefab;         // Prefab for the tape image (UI Image)
    
    [Header("Sprites")]
    public Sprite doorOpen;
    public Sprite doorClosed;
    public Sprite[] windowOpenVariants;
    public Sprite windowClosed;

    [Header("Tape Settings")]
    public float swipeThreshold = 100f; // pixels
    public float tapeOffset = 20f;      // how far tape overlaps the window edge

    private int currentIndex = 0;
    private Button currentUIButton;
    private bool isZoomed = false;
    private bool isClosed = false;
    private bool isDoor = false;

    private Vector2 swipeStart;
    private bool isSwiping = false;
    private HashSet<string> tapedEdges = new HashSet<string>();

    void Start()
    {
        SetupScenes();
        ShowScene(0);

        leftArrow.onClick.AddListener(() => ShowScene(currentIndex - 1));
        rightArrow.onClick.AddListener(() => ShowScene(currentIndex + 1));
        backButton.onClick.AddListener(ExitZoom);

        zoomPanel.SetActive(false);
        backButton.gameObject.SetActive(false);
    }

    void SetupScenes()
    {
        foreach (GameObject bg in backgrounds)
        {
            foreach (Transform child in bg.transform)
            {
                Button ui = child.GetComponent<Button>();
                if (ui == null) continue;

                bool isDoor = Random.value > 0.5f;
                bool isOpen = Random.value > 0.5f;
                Image img = ui.GetComponent<Image>();

                if (isDoor)
                    img.sprite = isOpen ? doorOpen : doorClosed;
                else
                {
                    img.sprite = isOpen
                        ? windowOpenVariants[Random.Range(0, windowOpenVariants.Length)]
                        : windowClosed;
                }

                ui.onClick.RemoveAllListeners();
                if (isOpen)
                    ui.onClick.AddListener(() => OnUISelected(ui, isDoor));
            }
        }
    }

    void ShowScene(int index)
    {
        if (index < 0 || index >= backgrounds.Count) return;
        for (int i = 0; i < backgrounds.Count; i++)
            backgrounds[i].SetActive(i == index);
        currentIndex = index;
        leftArrow.interactable = currentIndex > 0;
        rightArrow.interactable = currentIndex < backgrounds.Count - 1;
    }

    void OnUISelected(Button ui, bool door)
    {
        if (isZoomed) return;

        isZoomed = true;
        isClosed = false;
        isDoor = door;
        currentUIButton = ui;
        tapedEdges.Clear();
        foreach (Transform t in tapeContainer) Destroy(t.gameObject);

        zoomPanel.SetActive(true);
        zoomImage.sprite = ui.GetComponent<Image>().sprite;
        leftArrow.gameObject.SetActive(false);
        rightArrow.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!isZoomed || isDoor) return; // tape mechanic is only for windows

        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            isSwiping = true;
        }
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            Vector2 swipeEnd = Input.mousePosition;
            Vector2 diff = swipeEnd - swipeStart;
            HandleSwipe(diff);
            isSwiping = false;
        }
    }

    void HandleSwipe(Vector2 diff)
    {
        if (diff.magnitude < swipeThreshold) return;

        string edge = "";
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            edge = diff.x > 0 ? "Right" : "Left";
        else
            edge = diff.y > 0 ? "Top" : "Bottom";

        if (tapedEdges.Contains(edge)) return;
        tapedEdges.Add(edge);
        PlaceTape(edge);

        if (tapedEdges.Count == 4)
            FinishTask();
    }

    void PlaceTape(string edge)
    {
        GameObject tape = Instantiate(tapePrefab, tapeContainer);
        RectTransform tr = tape.GetComponent<RectTransform>();
        tr.anchoredPosition = Vector2.zero;
        tr.localScale = Vector3.one;

        Vector2 pos = Vector2.zero;
        float rot = 0f;

        switch (edge)
        {
            case "Top":
                pos = new Vector2(0, zoomImage.rectTransform.rect.height / 2 - tapeOffset);
                rot = 0f;
                break;
            case "Bottom":
                pos = new Vector2(0, -zoomImage.rectTransform.rect.height / 2 + tapeOffset);
                rot = 0f;
                break;
            case "Left":
                pos = new Vector2(-zoomImage.rectTransform.rect.width / 2 + tapeOffset, 0);
                rot = 90f;
                break;
            case "Right":
                pos = new Vector2(zoomImage.rectTransform.rect.width / 2 - tapeOffset, 0);
                rot = 90f;
                break;
        }

        tr.anchoredPosition = pos;
        tr.localRotation = Quaternion.Euler(0, 0, rot);
    }

    void FinishTask()
    {
        // Replace original window sprite with a taped indicator if desired
        currentUIButton.GetComponent<Image>().color = Color.gray; // example feedback
        ExitZoom();
    }

    void ExitZoom()
    {
        zoomPanel.SetActive(false);
        leftArrow.gameObject.SetActive(true);
        rightArrow.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);
        isZoomed = false;
        currentUIButton = null;
    }
}
