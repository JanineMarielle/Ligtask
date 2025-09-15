using UnityEngine;

[CreateAssetMenu(fileName = "GoBagItem", menuName = "GoBag/Item")]
public class GoBagItemSO : ScriptableObject
{
    public string itemName;       // e.g., "Flashlight"
    public Sprite itemSprite;     // image shown in game
    public bool isNecessary;      // true = should go in bag
}
