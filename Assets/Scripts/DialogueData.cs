using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Narration/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [TextArea(3, 5)]
    public string[] lines;

    [Header("Optional Voice Clips")]
    public AudioClip[] voiceClips; 
}
