using System.Text;
using AmanhaEuEstudo.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AmanhaEuEstudo.DebugTools
{
    // Throwaway test UI built entirely from code. Not the real HUD: it only listens to GameManager events.
    public class DebugHud : MonoBehaviour
    {
        private GameManager manager;
        private Text statusText;
        private Text messageText;
        private RectTransform choicesRoot;
        private bool dirty = true;
        private Font font;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildLayout();
        }

        private void OnEnable()
        {
            manager = GameManager.Instance;
            if (manager == null)
            {
                Debug.LogError("DebugHud needs a GameManager in the scene.", this);
                return;
            }
            manager.OnStatChanged += OnStatChanged;
            manager.OnPeriodChanged += OnAnyChange;
            manager.OnDayStarted += OnAnyChange;
            manager.OnBurnout += OnBurnout;
            manager.OnFinalDayReached += OnFinalDay;
            manager.OnGameRestarted += OnRestarted;
            dirty = true;
        }

        private void OnDisable()
        {
            if (manager == null) return;
            manager.OnStatChanged -= OnStatChanged;
            manager.OnPeriodChanged -= OnAnyChange;
            manager.OnDayStarted -= OnAnyChange;
            manager.OnBurnout -= OnBurnout;
            manager.OnFinalDayReached -= OnFinalDay;
            manager.OnGameRestarted -= OnRestarted;
            manager = null;
        }

        private void LateUpdate()
        {
            if (!dirty || manager == null || manager.State == null) return;
            dirty = false;
            Refresh();
        }

        private void OnStatChanged(StatType s, int o, int n) { dirty = true; }
        private void OnAnyChange(TimeOfDay p) { dirty = true; }
        private void OnAnyChange(int d) { dirty = true; }
        private void OnFinalDay() { dirty = true; }

        private void OnBurnout(StatType stat)
        {
            messageText.text = $"BURNOUT! {DisplayNames.Of(stat)} chegou a 0. Fim de jogo.";
            dirty = true;
        }

        private void OnRestarted()
        {
            messageText.text = "";
            dirty = true;
        }

        private void Refresh()
        {
            var state = manager.State;
            var sb = new StringBuilder();
            sb.AppendLine($"Dia {state.day} / {manager.Config.TotalDays}   |   {DisplayNames.Of(state.period)}   |   {(state.exhaustedToday ? "EXAUSTO" : "Descansado")}   |   Fase: {state.phase}");
            for (int i = 0; i < EnumExtensions.StatCount; i++)
            {
                var stat = (StatType)i;
                sb.AppendLine($"{DisplayNames.Of(stat)}: {state.GetStat(stat)}");
            }
            statusText.text = sb.ToString();

            if (state.phase == GamePhase.FinalDayReached)
                messageText.text = "Dia final alcançado (OnFinalDayReached). O combate entra aqui.";

            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
            {
                var old = choicesRoot.GetChild(i);
                old.SetParent(null); // Destroy is deferred; detach so layout ignores it this frame
                Destroy(old.gameObject);
            }

            foreach (var choice in manager.GetAvailableChoices())
            {
                var captured = choice;
                CreateButton(choicesRoot, Describe(choice), () => manager.ApplyChoice(captured));
            }
        }

        private static string Describe(ChoiceData choice)
        {
            var sb = new StringBuilder(choice.DisplayName);
            foreach (var d in choice.Deltas)
                sb.Append($"  [{DisplayNames.Of(d.stat)} {d.amount:+#;-#;0}]");
            if (choice.CausesExhaustionNextDay) sb.Append("  (exausto amanhã)");
            return sb.ToString();
        }

        private static void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
                es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            // The project uses the new Input System only; the legacy module would throw.
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null) Destroy(legacy);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildLayout()
        {
            var canvasGo = new GameObject("DebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = new Vector2(30, 30);
            panelRt.offsetMax = new Vector2(-30, -30);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            statusText = CreateText(panel.transform, 26, Color.white, 130);
            messageText = CreateText(panel.transform, 28, new Color(1f, 0.4f, 0.4f), 50);

            var choices = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            choices.transform.SetParent(panel.transform, false);
            choicesRoot = (RectTransform)choices.transform;
            var cl = choices.GetComponent<VerticalLayoutGroup>();
            cl.spacing = 8;
            cl.childControlWidth = true;
            cl.childControlHeight = true;
            cl.childForceExpandWidth = true;
            cl.childForceExpandHeight = false;
            choices.AddComponent<LayoutElement>().flexibleHeight = 1;

            CreateButton(panel.transform, "Reiniciar", () => manager?.RestartGame());
        }

        private Text CreateText(Transform parent, int size, Color color, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return t;
        }

        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);
            go.GetComponent<LayoutElement>().preferredHeight = 48;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

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
            t.text = label;
            return button;
        }
    }
}
