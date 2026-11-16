using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChoiceImpact
{
    public string statId;
    public string label;
    public Sprite icon;
    public float amount;
}

[Serializable]
public class ChoiceOption
{
    public string id;
    public string title;
    [TextArea] public string description;
    public bool available = true;
    public string unavailableReason;
    public List<ChoiceImpact> impacts = new List<ChoiceImpact>();
}

[CreateAssetMenu(menuName = "Game/Choices/Choice", fileName = "new_choice")]
public class ChoiceDefinition : ScriptableObject
{
    public string id;
    public string context;
    [TextArea] public string prompt;
    public List<ChoiceOption> options = new List<ChoiceOption>();
}
