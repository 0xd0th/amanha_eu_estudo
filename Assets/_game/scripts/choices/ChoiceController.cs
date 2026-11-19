using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChoiceController : MonoBehaviour
{
    [SerializeField] private ChoiceDefinition initialChoice;
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private PhoneChoiceView view;
    [SerializeField] private UnityEvent<string> onSelected = new UnityEvent<string>();
    public event Action<ChoiceDefinition, ChoiceOption> Selected;
    public Func<ChoiceOption, bool> CanChoose;
    public ChoiceDefinition Current { get; private set; }
    private bool resolved;

    private void Start()
    {
        if (showOnStart && initialChoice != null) Show(initialChoice);
    }

    public void Show(ChoiceDefinition definition)
    {
        if (definition == null || view == null) throw new ArgumentException("Choice definition and view are required.");
        var ids = new HashSet<string>();
        foreach (var option in definition.options)
            if (option == null || string.IsNullOrWhiteSpace(option.id) || !ids.Add(option.id))
                throw new ArgumentException("Every choice option must have a unique, nonempty id.");
        Current = definition;
        resolved = false;
        view.Show(definition, IsAvailable, Choose);
    }

    private bool IsAvailable(ChoiceOption option) => option.available && (CanChoose == null || CanChoose(option));

    public void Choose(string id)
    {
        if (resolved || Current == null) return;
        var option = Current.options.Find(item => item.id == id);
        if (option == null || !IsAvailable(option)) return;
        resolved = true;
        var definition = Current;
        // Lock before notifying listeners, including listeners that open another choice.
        view.Resolve(option.title);
        onSelected.Invoke(id);
        Selected?.Invoke(definition, option);
    }

    public void Close()
    {
        resolved = true;
        view.gameObject.SetActive(false);
    }
}
