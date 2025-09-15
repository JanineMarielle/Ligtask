using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public DialogueData dialogueData;        // Drag the ScriptableObject here
    public TextMeshProUGUI dialogueText;     // UI text box for narration
    public GameObject dialoguePanel;         // Panel that holds the dialogue UI
    public GameObject characterImage;        // Character portrait or sprite
    public GameObject nextLineButton;

    private int currentLine = 0;

    void Start()
    {
        ResizePanel(); // ✅ Make panel 40% of screen height
        StartDialogue();
    }

    void ResizePanel()
    {
        if (dialoguePanel != null)
        {
            RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                float panelHeight = Screen.height * 0.4f; // 40% of screen height
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, panelHeight);
            }
        }
    }

    public void StartDialogue()
    {
        currentLine = 0;
        dialoguePanel.SetActive(true);

        if (characterImage != null)
            characterImage.SetActive(true); // ✅ Show character at start

        ShowLine();
    }

    public void NextLine()
    {
        if (currentLine < dialogueData.lines.Length - 1)
        {
            currentLine++;
            ShowLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void ShowLine()
    {
        if (currentLine >= 0 && currentLine < dialogueData.lines.Length)
        {
            dialogueText.text = dialogueData.lines[currentLine];
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);

        if (characterImage != null)
            characterImage.SetActive(false);

        if (nextLineButton != null)
        {
            nextLineButton.GetComponent<Button>().interactable = false;
            nextLineButton.SetActive(false); // Optional: completely hide it
        }

        CountDownManager countdown = FindObjectOfType<CountDownManager>();
        if (countdown != null)
        {
            countdown.StartCountdown();
        }
        else
        {
            Debug.LogWarning("CountDownManager not found in the scene!");
        }
    }
}
