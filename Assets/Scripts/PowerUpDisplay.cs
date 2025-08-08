using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpDisplay : MonoBehaviour
{
    public PowerUpData data;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    //public TextMeshProUGUI ownedText;
    public GameObject detailPanel;

    public GameObject selectedBorder; 
    private static PowerUpDisplay currentlySelected = null;

    void Start()
    {
        selectedBorder.SetActive(false);
    }

    public void OnClick()
    {
        if (currentlySelected != null)
            currentlySelected.selectedBorder.SetActive(false);

        selectedBorder.SetActive(true);
        currentlySelected = this;

        detailPanel.SetActive(true);
        nameText.text = data.name;
        costText.text = "<b>Cost:</b> " + data.cost.ToString();
        descriptionText.text = data.description;
        //ownedText.text = "Owned: " + data.owned;
    }
}
