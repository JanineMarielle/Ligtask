using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "PowerUp")]
public class PowerUpData : ScriptableObject
{
    public string powerUpID; 
    public string displayName; 
    public string description;
    public int cost;
    public Sprite icon;
}
