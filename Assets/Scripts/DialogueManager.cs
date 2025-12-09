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

    [Header("Tap To Continue Controller (Auto-Detected)")]
    public TapToCont tapController;

    private int currentLine = 0;
    private bool isTyping = false;
    private float typingSpeed = 0.03f;

    void Start()
    {
        if (tapController == null && dialoguePanel != null)
        {
            tapController = dialoguePanel.GetComponentInChildren<TapToCont>(true);

            if (tapController == null)
                Debug.LogWarning("DialogueManager: No TapToCont found in dialoguePanel!");
        }

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

        if (tapController != null)
            tapController.HideTapToContinue();

        ShowLine();
    }

    public void NextLine()
    {
        if (tapController != null)
            tapController.HideTapToContinue();

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = dialogueData.lines[currentLine];
            isTyping = false;

            if (nextLineButton != null)
                nextLineButton.GetComponent<Button>().interactable = true;

            if (tapController != null)
                tapController.ShowTapToContinue();

            return;
        }

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
        StopAllCoroutines();

        if (tapController != null)
            tapController.HideTapToContinue();

        if (currentLine >= 0 && currentLine < dialogueData.lines.Length)
        {
            StartCoroutine(TypeLine(dialogueData.lines[currentLine]));

            if (AudioManager.Instance != null &&
                dialogueData.voiceClips != null &&
                currentLine < dialogueData.voiceClips.Length &&
                dialogueData.voiceClips[currentLine] != null)
            {
                AudioManager.Instance.PlayNarration(dialogueData.voiceClips[currentLine]);
            }
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

        if (tapController != null)
            tapController.ShowTapToContinue();
    }

    void EndDialogue()
    {
        if (tapController != null)
            tapController.HideTapToContinue();

        dialoguePanel.SetActive(false);

        if (characterImage != null)
            characterImage.SetActive(false);

        if (nextLineButton != null)
        {
            nextLineButton.GetComponent<Button>().interactable = false;
            nextLineButton.SetActive(false);
        }

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
