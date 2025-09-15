using UnityEngine;
using System;

public class PersonSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject personPrefab;          
    public RectTransform leftSpawnPoint;
    public RectTransform rightSpawnPoint;
    public RectTransform windowTransform;    
    public RectTransform canvasTransform;    

    [Header("Spawning Settings")]
    [SerializeField] private bool allowOverlapping = false;

    [Header("Person Settings")]
    [SerializeField] private float personSpeed = 200f;
    
    [Header("Spawn Timing")]
    [SerializeField] private float minDelay = 0.5f;
    [SerializeField] private float maxDelay = 2f;

    public float MinDelay => minDelay;
    public float MaxDelay => maxDelay;

    private bool isSpawning = false;
    private bool isPaused = false;

    public event Action<Person> onPersonSpawned;

    private void OnEnable()
    {
        SidePanelController.OnPauseStateChanged += OnPauseStateChanged;
    }

    private void OnDisable()
    {
        SidePanelController.OnPauseStateChanged -= OnPauseStateChanged;
    }

    private void OnPauseStateChanged(bool paused)
    {
        isPaused = paused;
    }

    public void SpawnOne()
    {
        if (isPaused)
        {
            Debug.Log("[PersonSpawner] Spawn blocked: Game is paused.");
            return;
        }

        if (!allowOverlapping && isSpawning)
        {
            Debug.Log("[PersonSpawner] Spawn blocked: Already spawning.");
            return;
        }

        if (personPrefab == null || canvasTransform == null)
        {
            Debug.LogError("[PersonSpawner] Missing prefab or canvasTransform!");
            return;
        }

        // Randomize spawn side
        bool spawnLeft = UnityEngine.Random.value > 0.5f;
        RectTransform spawnPoint = spawnLeft ? leftSpawnPoint : rightSpawnPoint;

        if (spawnPoint == null)
        {
            Debug.LogError("[PersonSpawner] Missing spawn point reference!");
            return;
        }

        GameObject personObj = Instantiate(personPrefab, canvasTransform);
        RectTransform personRect = personObj.GetComponent<RectTransform>();
        personRect.anchoredPosition = spawnPoint.anchoredPosition;

        Person personScript = personObj.GetComponent<Person>();
        if (personScript != null)
        {
            personScript.SetSpeed(personSpeed);
            personScript.Init(windowTransform, allowOverlapping, () => OnPersonDone(personScript));

            if (!allowOverlapping)
                isSpawning = true;

            if (onPersonSpawned != null)
            {
                Debug.Log($"[PersonSpawner] onPersonSpawned fired for {personObj.name}");
                onPersonSpawned.Invoke(personScript);
            }
        }
        else
        {
            Debug.LogError("[PersonSpawner] Spawned object missing Person component!");
        }
    }

    private void OnPersonDone(Person person)
    {
        if (!allowOverlapping)
            isSpawning = false;
    }
}
