using UnityEngine;

public class LeafSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public RectTransform spawnArea;
    public GameObject leafPrefab;
    public int leafCount = 10;

    [Header("Leaf Sprites")]
    public Sprite[] leafSprites; // Assign multiple sprites here

    private SwipeLeafManager manager;

    void Start()
    {
        // Find the SwipeLeafManager in the scene
        manager = FindObjectOfType<SwipeLeafManager>();

        for (int i = 0; i < leafCount; i++)
        {
            SpawnLeaf();
        }
    }

    void SpawnLeaf()
    {
        GameObject leaf = Instantiate(leafPrefab, spawnArea);
        RectTransform rt = leaf.GetComponent<RectTransform>();

        // Random position
        Vector2 randomPos = new Vector2(
            Random.Range(-spawnArea.rect.width / 2, spawnArea.rect.width / 2),
            Random.Range(-spawnArea.rect.height / 2, spawnArea.rect.height / 2)
        );
        rt.anchoredPosition = randomPos;

        // Random rotation (upright looks too uniform otherwise)
        float randomAngle = Random.Range(0f, 360f);
        rt.rotation = Quaternion.Euler(0f, 0f, randomAngle);

        // Random sprite
        var img = leaf.GetComponent<UnityEngine.UI.Image>();
        if (img != null && leafSprites.Length > 0)
        {
            img.sprite = leafSprites[Random.Range(0, leafSprites.Length)];
        }

    }
}
