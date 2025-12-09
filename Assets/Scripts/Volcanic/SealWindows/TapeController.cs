using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class TapeController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    public RectTransform canvas;
    public GameObject tapePrefab;

    [Header("Tape Sprites")]
    public Sprite horizontalTapeSprite;

    [Header("Settings")]
    public float tapeThickness = 25f;
    public float minSwipeDistance = 30f;

    [Header("SealWindows Reference")]
    public SealWindows sealWindows;

    [Header("UI")]
    public TMP_Text instructionsText;       
    public float messageDuration = 2f;      
    public float messageCooldown = 3f; 
    
    private float lastMessageTime = -999f;

    private RectTransform currentGrid;
    private bool canTape = false;
    private Vector2 dragStart;

    [HideInInspector]
    public Dictionary<RectTransform, Dictionary<string, bool>> windowEdges = new Dictionary<RectTransform, Dictionary<string, bool>>();

    [HideInInspector]
    public List<RectTransform> allWindows = new List<RectTransform>();

    public void ActivateForGrid(RectTransform grid)
    {
        currentGrid = grid;
        canTape = true;
    }

    public void Deactivate()
    {
        canTape = false;
        currentGrid = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canTape) return;
        dragStart = eventData.position;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canTape || currentGrid == null) return;

        Vector2 dragEnd = eventData.position;
        Vector2 delta = dragEnd - dragStart;
        if (delta.magnitude < minSwipeDistance) return;

        RectTransform window = GetClosestWindowInGrid(dragStart);
        if (window == null) return;

        string closestEdge = GetClosestEdge(window, dragStart);
        TapeEdge(window, closestEdge);
        CheckNeighbors(window, closestEdge);
        DebugAllWindows();

        if (sealWindows != null)
        {
            sealWindows.CheckEndGame();
        }
    }

    private RectTransform GetClosestWindowInGrid(Vector2 screenPoint)
    {
        RectTransform closest = null;
        float minDist = float.MaxValue;

        foreach (Transform child in currentGrid)
        {
            RectTransform win = child as RectTransform;
            if (win == null) continue;

            Vector2 winCenter = RectTransformUtility.WorldToScreenPoint(null, win.position);
            float d = Vector2.Distance(winCenter, screenPoint);
            if (d < minDist)
            {
                minDist = d;
                closest = win;
            }
        }

        return closest;
    }

    private string GetClosestEdge(RectTransform window, Vector2 screenPoint)
    {
        Vector3[] corners = new Vector3[4];
        window.GetWorldCorners(corners);

        Vector3 topCenter = (corners[1] + corners[2]) / 2f;
        Vector3 bottomCenter = (corners[0] + corners[3]) / 2f;
        Vector3 leftCenter = (corners[0] + corners[1]) / 2f;
        Vector3 rightCenter = (corners[2] + corners[3]) / 2f;

        Dictionary<string, float> distances = new Dictionary<string, float>()
        {
            { "Top", Vector2.Distance(screenPoint, RectTransformUtility.WorldToScreenPoint(null, topCenter)) },
            { "Bottom", Vector2.Distance(screenPoint, RectTransformUtility.WorldToScreenPoint(null, bottomCenter)) },
            { "Left", Vector2.Distance(screenPoint, RectTransformUtility.WorldToScreenPoint(null, leftCenter)) },
            { "Right", Vector2.Distance(screenPoint, RectTransformUtility.WorldToScreenPoint(null, rightCenter)) }
        };

        float minDist = float.MaxValue;
        string closest = "Top";
        foreach (var kvp in distances)
        {
            if (kvp.Value < minDist)
            {
                minDist = kvp.Value;
                closest = kvp.Key;
            }
        }
        return closest;
    }

    private void TapeEdge(RectTransform window, string edge)
    {
        if (!windowEdges.ContainsKey(window))
        {
            Debug.Log($"⚠ Skipping non-window object: {window.name}");
            return; // Ignore shelves or other visuals
        }

        if (windowEdges[window][edge]) return;

        CreateTape(window, edge);
        windowEdges[window][edge] = true;
    }

    private void CheckNeighbors(RectTransform window, string edge)
    {
        if (!windowEdges.ContainsKey(window)) return; // ignore shelves

        if (edge != "Left" && edge != "Right") return;

        Transform parent = window.parent;
        int index = window.GetSiblingIndex();
        RectTransform neighbor = null;

        if (edge == "Left" && index > 0)
            neighbor = parent.GetChild(index - 1) as RectTransform;
        if (edge == "Right" && index < parent.childCount - 1)
            neighbor = parent.GetChild(index + 1) as RectTransform;

        if (neighbor == null || !windowEdges.ContainsKey(neighbor)) return;

        string oppositeEdge = (edge == "Left") ? "Right" : "Left";
        windowEdges[neighbor][oppositeEdge] = true;
    }

    private void DebugAllWindows()
    {
        foreach (var kvp in windowEdges)
        {
            string debugMsg = $"{kvp.Key.name} taped edges: ";
            foreach (var edge in kvp.Value)
                debugMsg += $"{edge.Key}={(edge.Value ? "Taped" : "Open")} ";
            Debug.Log(debugMsg);
        }
    }

    private void CreateTape(RectTransform window, string edge)
    {
        if (!IsWindowClosed(window))
        {
            ShowInstruction("Close the window before taping!");
            return;
        }

        Transform grid = window.parent.parent;
        GameObject tapeObj = Instantiate(tapePrefab, grid);
        RectTransform rt = tapeObj.GetComponent<RectTransform>();
        Image tapeImage = tapeObj.GetComponent<Image>();

        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        Vector3 localPos = grid.InverseTransformPoint(window.position);
        float width = window.rect.width;
        float height = window.rect.height;
        float overlap = 20f;
        Vector2 size = Vector2.zero;

        if (edge == "Top")
        {
            localPos += new Vector3(0, height / 2f, 0);
            size = new Vector2(width + overlap, tapeThickness);
            tapeImage.sprite = horizontalTapeSprite;
        }
        else if (edge == "Bottom")
        {
            localPos += new Vector3(0, -height / 2f, 0);
            size = new Vector2(width + overlap, tapeThickness);
            tapeImage.sprite = horizontalTapeSprite;
        }
        else if (edge == "Left")
        {
            localPos += new Vector3(-width / 2f, 0, 0);
            size = new Vector2(tapeThickness, height + overlap);
        }
        else if (edge == "Right")
        {
            localPos += new Vector3(width / 2f, 0, 0);
            size = new Vector2(tapeThickness, height + overlap);
        }

        rt.localPosition = localPos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
    }

    private void ShowInstruction(string message)
    {
        if (Time.time - lastMessageTime < messageCooldown) return;

        lastMessageTime = Time.time;
        if (instructionsText != null)
        {
            instructionsText.text = message;
            instructionsText.gameObject.SetActive(true);

            // Stop any previous fade coroutine
            StopCoroutine("FadeInstruction");
            StartCoroutine(FadeInstruction());
        }
    }

    private IEnumerator FadeInstruction()
    {
        instructionsText.alpha = 1f;

        float timer = 0f;
        while (timer < messageDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        float fadeTime = 0.5f;
        timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            instructionsText.alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
            yield return null;
        }

        instructionsText.gameObject.SetActive(false);
    }

    private bool IsWindowClosed(RectTransform window)
    {
        if (sealWindows == null) return true;
        Image img = window.GetComponent<Image>();
        return img != null && img.sprite == sealWindows.windowClosed;
    }

    private bool AllEdgesTapedAllGrids()
    {
        foreach (var kvp in windowEdges)
        {
            foreach (var edge in kvp.Value)
            {
                if (!edge.Value) return false; // If any edge is untaped, return false
            }
        }
        return true; 
    }

}
