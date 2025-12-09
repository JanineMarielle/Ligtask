using UnityEngine;

public class AshSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public RectTransform spawnArea;
    public GameObject ashPrefab;
    public int ashCount = 100;

    [Header("Spawn Padding")]
    public float padding = 50f; 

    [Header("Ash Sprites")]
    public Sprite[] ashSprites;

    [Header("Dustpan Reference")]
    public RectTransform dustpanArea;

    [Header("Ash Size")]
    public float minScale = 0.5f;
    public float maxScale = 1.2f;

    private SwipeAshManager manager;

    void Start()
    {
        manager = FindObjectOfType<SwipeAshManager>();

        if (manager == null)
        {
            Debug.LogWarning("SwipeAshManager not found in scene!");
        }
    }

    public void SpawnAshes(int count)
    {
        for (int i = 0; i < count; i++)
            SpawnAsh();
    }

    private void SpawnAsh()
    {
        if (ashPrefab == null || spawnArea == null) return;

        GameObject ash = Instantiate(ashPrefab, spawnArea);
        RectTransform rt = ash.GetComponent<RectTransform>();

        // Center pivot
        Vector2 spawnPivotOffset = new Vector2(spawnArea.rect.width, spawnArea.rect.height) * 0.5f;

        Vector2 randomPos = Vector2.zero;
        int attempts = 0;
        bool validPos = false;

        // Keep trying until we find a position outside the dustpan
        while (!validPos && attempts < 100)
        {
            randomPos = new Vector2(
                Random.Range(padding, spawnArea.rect.width - padding) - spawnPivotOffset.x,
                Random.Range(padding, spawnArea.rect.height - padding) - spawnPivotOffset.y
            );

            rt.anchoredPosition = randomPos;

            // Check if it overlaps the dustpan
            validPos = dustpanArea == null ||
                        !RectTransformUtility.RectangleContainsScreenPoint(
                            dustpanArea,
                            rt.position,
                            null // Screen Space Overlay doesn't need a camera
                        );

            attempts++;
        }

        rt.anchoredPosition = randomPos;

        // Random rotation
        rt.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // Random scale
        float scale = Random.Range(minScale, maxScale);
        rt.localScale = new Vector3(scale, scale, 1);

        // Random sprite
        var img = ash.GetComponent<UnityEngine.UI.Image>();
        if (img != null && ashSprites.Length > 0)
            img.sprite = ashSprites[Random.Range(0, ashSprites.Length)];

        // Assign the swipe manager
        var ashController = ash.GetComponent<AshController>();
        if (ashController != null)
            ashController.swipeManager = manager;
    }
}
