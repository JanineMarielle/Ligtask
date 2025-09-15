using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Use TextMeshPro if you’re using TMP instead of legacy Text

public class NarrationManager : MonoBehaviour
{
    [Header("Narration UI")]
    public Image characterImage;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI tapToContinueText;

    [Header("Countdown UI")]
    public TextMeshProUGUI countdownText;

    [Header("Narration Settings")]
    [TextArea] public string[] dialogueLines;
    private int currentLine = 0;

    private bool waitingForTap = false;

    private void Start()
    {
        countdownText.gameObject.SetActive(false);
        ShowLine();
    }

    private void Update()
    {
        if (waitingForTap && Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
    }

    void ShowLine()
    {
        dialogueText.text = dialogueLines[currentLine];
        waitingForTap = true;
        tapToContinueText.gameObject.SetActive(true);
    }

    void NextLine()
    {
        tapToContinueText.gameObject.SetActive(false);
        currentLine++;

        if (currentLine < dialogueLines.Length)
        {
            ShowLine();
        }
        else
        {
            StartCoroutine(StartCountdown());
        }
    }

    IEnumerator StartCountdown()
    {
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "Go!";
        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);

        // ✅ Level officially starts here
        // Example: call GoBagManager.StartLevel()
    }
}
