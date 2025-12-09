using UnityEngine;
using UnityEngine.EventSystems;

public class TrashDropZone : MonoBehaviour, IDropHandler
{
    public CleanDrainManager gameManager;

    public void OnDrop(PointerEventData eventData)
    {
    }
}
