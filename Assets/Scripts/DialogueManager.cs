using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public DialogueData dialogueData;        
    public TextMeshProUGUI dialogueText;     
    public GameObject dialoguePanel;         
    public GameObject characterImage;        
    public GameObject nextLineButton;        

    private int currentLine = 0;
    private bool isTyping = false;
    private float typingSpeed = 0.03f; // typing speed per character

    void Start()
    {
        ResizePanel();
        StartDialogue();
    }

    void ResizePanel()
    {
        if (dialoguePanel != null)
        {
            RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0.45f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }

    public void StartDialogue()
    {
        currentLine = 0;
        dialoguePanel.SetActive(true);

        if (characterImage != null)
            characterImage.SetActive(true);

        ShowLine();
    }

    public void NextLine()
    {
        // If still typing, finish instantly
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = dialogueData.lines[currentLine];
            isTyping = false;

            if (nextLineButton != null)
                nextLineButton.GetComponent<Button>().interactable = true;

            return;
        }

        // Stop current narration when skipping (safe null-check)
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopNarration();

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
            StopAllCoroutines();
            StartCoroutine(TypeLine(dialogueData.lines[currentLine]));

            // Play matching voice clip (if available)
            if (AudioManager.Instance != null &&
                dialogueData.voiceClips != null &&
                currentLine < dialogueData.voiceClips.Length &&
                dialogueData.voiceClips[currentLine] != null)
            {
                AudioManager.Instance.PlayNarration(dialogueData.voiceClips[currentLine]);
            }
            // If voice clip is missing, just skip without any errors
        }
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        if (nextLineButton != null)
            nextLineButton.GetComponent<Button>().interactable = true;
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);

        if (characterImage != null)
            characterImage.SetActive(false);

        if (nextLineButton != null)
        {
            nextLineButton.GetComponent<Button>().interactable = false;
            nextLineButton.SetActive(false);
        }

        // Stop narration when dialogue ends (safe null-check)
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopNarration();

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
