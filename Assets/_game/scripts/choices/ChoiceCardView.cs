using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceCardView : MonoBehaviour
{
    public Button button;
    public Text titleText;
    public Text descriptionText;
    public Text reasonText;
    public Transform impactRow;
    public GameObject impactTemplate;

    public void Bind(ChoiceOption option, bool available, Action<string> select)
    {
        titleText.text = option.title;
        descriptionText.text = option.description;
        reasonText.text = available ? "" : (string.IsNullOrEmpty(option.unavailableReason) ? "indisponível" : option.unavailableReason);
        button.interactable = available;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select(option.id));
        var seen = new HashSet<string>();
        foreach (var impact in option.impacts)
        {
            if (impact == null || impact.amount == 0 || string.IsNullOrEmpty(impact.statId) || !seen.Add(impact.statId)) continue;
            var badge = Instantiate(impactTemplate, impactRow);
            badge.SetActive(true);
            var image = badge.transform.Find("Icon").GetComponent<Image>();
            image.sprite = impact.icon;
            image.color = new Color(.85f,.88f,.94f);
            badge.GetComponentInChildren<Text>().text = string.IsNullOrEmpty(impact.label) ? impact.statId : impact.label;
        }
        impactTemplate.SetActive(false);
    }
}
