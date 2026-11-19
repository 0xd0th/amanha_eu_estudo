using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Provide input when opened alone; reuse the host scene EventSystem when additive.
public class ChoiceSceneInput : MonoBehaviour
{
    private GameObject owned;
    private void Update()
    {
        var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        bool external = false;
        foreach (var system in systems)
            if (system.isActiveAndEnabled && system.gameObject != owned) { external = true; break; }
        if (external) { if (owned != null) owned.SetActive(false); return; }
        if (owned == null)
        {
            owned = new GameObject("ChoiceEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            owned.transform.SetParent(transform);
        }
        owned.SetActive(true);
    }
}
