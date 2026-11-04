using AmanhaEuEstudo.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmanhaEuEstudo.DebugTools
{
    // Throwaway buttons to exercise DebtPileView.SetDebt while the Study Debt card does not exist.
    // Needs an EventSystem in the scene (DebugHud creates one).
    public class HudDebugPanel : MonoBehaviour
    {
        private HudView hud;
        private Text label;
        private int debt;

        private void Awake()
        {
            BuildLayout();
        }

        private void Start()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                Debug.LogWarning("HudDebugPanel: no EventSystem in the scene, buttons will not respond.", this);
            RefreshLabel();
        }

        private void ChangeDebt(int delta, bool reset)
        {
            if (hud == null) hud = FindAnyObjectByType<HudView>();
            if (hud == null || hud.DebtPile == null)
            {
                Debug.LogWarning("HudDebugPanel: no HudView/DebtPile found.", this);
                return;
            }
            debt = reset ? 0 : Mathf.Max(0, debt + delta);
            hud.DebtPile.SetDebt(debt);
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            label.text = $"Débito: {debt}";
        }

        private void BuildLayout()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("HudDebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            // Bottom-left strip, away from the HUD (top corners) and the DebugHud (bottom/center).
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(30, 30);
            rt.sizeDelta = new Vector2(240, 0);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label = CreateText(panel.transform, font, "Débito: 0", 26, 40);
            CreateButton(panel.transform, font, "Débito +1", () => ChangeDebt(1, false));
            CreateButton(panel.transform, font, "Débito −1", () => ChangeDebt(-1, false));
            CreateButton(panel.transform, font, "Zerar débito", () => ChangeDebt(0, true));
        }

        private static Text CreateText(Transform parent, Font font, string text, int size, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = Color.white;
            t.text = text;
            t.raycastTarget = false;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return t;
        }

        private static void CreateButton(Transform parent, Font font, string text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);
            go.GetComponent<LayoutElement>().preferredHeight = 44;
            go.GetComponent<Button>().onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 0);
            rt.offsetMax = new Vector2(-12, 0);
            var t = textGo.GetComponent<Text>();
            t.font = font;
            t.fontSize = 22;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.text = text;
            t.raycastTarget = false;
        }
    }
}
