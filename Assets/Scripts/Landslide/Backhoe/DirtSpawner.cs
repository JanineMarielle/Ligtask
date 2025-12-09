using UnityEngine;

public class DirtSpawner : MonoBehaviour
{
    [Header("References")]
    public RectTransform spawnArea;   // The area where dirt can spawn
    public Canvas dirtCanvas;         // The canvas where dirt will be added
    public GameObject dirtPrefab;     // Dirt prefab

    [Header("Settings")]
    public int dirtCount = 80;
    public float minSizePercent = 0.5f; // 50% of original size
    public float maxSizePercent = 1f;   // 100% of original size

    void Start()
    {
        SpawnDirt();
    }

    void SpawnDirt()
    {
        for (int i = 0; i < dirtCount; i++)
        {
            GameObject dirt = Instantiate(dirtPrefab, dirtCanvas.transform);
            RectTransform rt = dirt.GetComponent<RectTransform>();

            // Random position inside canvas
            float x = Random.Range(-spawnArea.rect.width / 2, spawnArea.rect.width / 2);
            float y = Random.Range(-spawnArea.rect.height / 2, spawnArea.rect.height / 2);
            rt.anchoredPosition = new Vector2(x, y);

            // Random rotation
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            // Random flip
            Vector3 scale = Vector3.one;
            if (Random.value > 0.5f) scale.x = -1;
            if (Random.value > 0.5f) scale.y = -1;

            // Random size percentage
            float sizePercent = Random.Range(minSizePercent, maxSizePercent);
            scale *= sizePercent;

            rt.localScale = scale;
        }
    }
}
