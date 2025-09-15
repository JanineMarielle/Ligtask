using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InGameLogger : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI logText;

    [Header("Settings")]
    [SerializeField] private int maxLines = 15; // how many lines to keep

    private Queue<string> logQueue = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Add new log
        logQueue.Enqueue(logString);

        // Keep only the last X logs
        while (logQueue.Count > maxLines)
            logQueue.Dequeue();

        // Update UI text
        logText.text = string.Join("\n", logQueue.ToArray());
    }
}
