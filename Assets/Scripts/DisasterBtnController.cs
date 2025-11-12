using UnityEngine;
using UnityEngine.UI;

public class DisasterBtnController : MonoBehaviour
{
    [Header("Disaster Info")]
    public string displayName;        // Name shown on UI
    public string dbName;             // Name used in DB
    [TextArea(3, 6)] public string overviewText;
    public string easySceneName;
    public string hardSceneName;
    public string quizSceneName;
    public Sprite disasterImage;

    [Header("References")]
    public Button disasterButton;
    public OverviewPanel overviewUIManager;

    void Start()
    {
        if (disasterButton != null && overviewUIManager != null)
        {
            disasterButton.onClick.AddListener(() =>
            {
                overviewUIManager.OpenOverview(
                    displayName,
                    dbName,
                    overviewText,
                    easySceneName,
                    hardSceneName,
                    quizSceneName,
                    disasterImage
                );
            });
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Missing button or UI manager reference!");
        }
    }
}
