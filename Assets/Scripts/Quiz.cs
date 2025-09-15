using UnityEngine;

[CreateAssetMenu(fileName = "Quiz", menuName = "Ligtask/Quiz")]
public class Quiz : ScriptableObject
{
    public string disasterName;           // e.g., "Typhoon", "Earthquake"
    public Questions[] questions;  // Array of questions
}
