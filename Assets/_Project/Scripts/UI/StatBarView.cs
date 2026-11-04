using System;
using AmanhaEuEstudo.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmanhaEuEstudo.UI
{
    // One bar: icon + fill + number. Reusable on its own (HUD, pause menu, minigame result): put the
    // StatBar prefab anywhere, call Configure(...) (or set config/stat in the inspector), then SetInstant/AnimateTo.
    // The number follows the animated value, so bar and number never disagree.
    public class StatBarView : MonoBehaviour
    {
        [SerializeField] private HudConfig config;
        [SerializeField] private StatType stat;
        [SerializeField] private int minValue = 0;
        [SerializeField] private int maxValue = 100;

        [SerializeField] private Image icon;
        [SerializeField] private Image frame;
        [SerializeField] private Image background;
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text valueText;
        [Tooltip("Optional. When assigned it shows DisplayNames.Of(stat).")]
        [SerializeField] private TMP_Text nameText;

        private BarFillAnimation animation;
        private Color fillColor = Color.white;
        private int pixelSteps;
        private bool low;
        private int shownValue = int.MinValue;

        // Fires when an AnimateTo finishes (also immediately when there is nothing to animate).
        public event Action OnFillAnimationCompleted;

        public StatType Stat => stat;
        public int DisplayedValue => Mathf.RoundToInt(Anim.Current);

        private BarFillAnimation Anim
        {
            get
            {
                if (animation == null)
                    animation = new BarFillAnimation(config != null ? config.FillDuration : 0.4f,
                        config == null || config.EaseOut);
                return animation;
            }
        }

        private void OnEnable()
        {
            ApplyConfig();
            Render();
        }

        public void Configure(HudConfig newConfig, StatType newStat, int min, int max)
        {
            config = newConfig;
            stat = newStat;
            minValue = min;
            maxValue = max <= min ? min + 1 : max;
            ApplyConfig();
            Render();
        }

        private void ApplyConfig()
        {
            if (nameText != null) nameText.text = DisplayNames.Of(stat);
            if (config == null) return;

            Anim.Duration = config.FillDuration;
            Anim.EaseOut = config.EaseOut;
            if (background != null) background.color = config.BarBackgroundColor;
            if (frame != null) frame.color = config.BarFrameColor;
            if (config.TryGetBar(stat, out var visual))
            {
                fillColor = visual.fillColor;
                pixelSteps = visual.pixelSteps;
                if (icon != null)
                {
                    icon.sprite = visual.icon;
                    icon.enabled = visual.icon != null;
                }
            }
            else
            {
                Debug.LogWarning($"HudConfig has no visual for {DisplayNames.Of(stat)}.", this);
            }
            if (fill != null)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        public void SetInstant(int value)
        {
            Anim.Snap(value);
            Render();
        }

        public void AnimateTo(int value)
        {
            Anim.SetTarget(value);
            Render();
            if (!Anim.IsAnimating) OnFillAnimationCompleted?.Invoke();
        }

        private void Update()
        {
            bool finished = Anim.Tick(Time.unscaledDeltaTime);
            if (!Anim.IsAnimating && !low && !finished) return;
            Render();
            if (finished) OnFillAnimationCompleted?.Invoke();
        }

        private void Render()
        {
            int shown = Mathf.RoundToInt(Anim.Current);
            float range = Mathf.Max(1, maxValue - minValue);
            float amount = PixelSnap.Quantize((Anim.Current - minValue) / range, pixelSteps);

            low = config != null && shown < config.LowThreshold;
            Color color = fillColor;
            if (low)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * config.BlinkSpeed * Mathf.PI * 2f);
                float brightness = Mathf.Lerp(config.LowMinBrightness, 1f, wave);
                color = new Color(fillColor.r * brightness, fillColor.g * brightness, fillColor.b * brightness, fillColor.a);
            }

            if (fill != null)
            {
                fill.fillAmount = amount;
                fill.color = color;
            }
            if (valueText != null && shown != shownValue)
            {
                shownValue = shown;
                valueText.text = shown.ToString();
            }
        }
    }
}
