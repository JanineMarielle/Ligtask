using UnityEngine;

[System.Serializable]
public class Questions
{
    [TextArea] 
    public string questionText; // The question
    public string[] options;    // Multiple-choice options
    public int correctAnswerIndex; // Index of correct option
}
