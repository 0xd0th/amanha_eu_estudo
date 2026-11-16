using System;
using UnityEngine;
using UnityEngine.UI;

public class PhoneChoiceView : MonoBehaviour
{
    public Text contextText;
    public Text promptText;
    public Text resultText;
    public Transform content;
    public ChoiceCardView cardTemplate;

    public void Show(ChoiceDefinition definition, Func<ChoiceOption,bool> available, Action<string> select)
    {
        gameObject.SetActive(true);
        contextText.text = definition.context;
        promptText.text = definition.prompt;
        resultText.text = "";
        foreach (var card in content.GetComponentsInChildren<ChoiceCardView>(true)) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
        foreach (var option in definition.options)
        {
            var card = Instantiate(cardTemplate, content);
            card.gameObject.SetActive(true);
            card.Bind(option, available(option), select);
        }
    }

    public void Resolve(string title)
    {
        foreach (var card in content.GetComponentsInChildren<ChoiceCardView>(true)) card.button.interactable = false;
        resultText.text = "escolha: " + title;
    }
}
