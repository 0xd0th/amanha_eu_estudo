using System;
using AmanhaEuEstudo.Core;
using UnityEngine;

namespace AmanhaEuEstudo.UI
{
    [Serializable]
    public struct StatBarVisual
    {
        public StatType stat;
        public Sprite icon;
        public Color fillColor;
        [Tooltip("Fill is rounded to this many steps (pixels of the bar). <= 0 turns the snap off. Placeholder.")]
        public int pixelSteps;

        public StatBarVisual(StatType stat, Sprite icon, Color fillColor, int pixelSteps)
        {
            this.stat = stat;
            this.icon = icon;
            this.fillColor = fillColor;
            this.pixelSteps = pixelSteps;
        }
    }

    [Serializable]
    public struct PortraitState
    {
        [Tooltip("Used when the source value is >= this and no state with a higher minValue also qualifies.")]
        public int minValue;
        public Sprite sprite;
        public Color tint;

        public PortraitState(int minValue, Sprite sprite, Color tint)
        {
            this.minValue = minValue;
            this.sprite = sprite;
            this.tint = tint;
        }
    }

    // All HUD tuning lives here. Swapping placeholder art for final art = swapping sprites in this asset.
    [CreateAssetMenu(fileName = "HudConfig", menuName = "Amanhã eu estudo/Hud Config")]
    public class HudConfig : ScriptableObject
    {
        [Header("Animação das barras")]
        [SerializeField] private float fillDuration = 0.4f;
        [SerializeField] private bool easeOut = true;

        [Header("Alerta de barra baixa")]
        [Tooltip("Bar below this value blinks/darkens.")]
        [SerializeField] private int lowThreshold = 25;
        [Tooltip("Blink cycles per second.")]
        [SerializeField] private float blinkSpeed = 2f;
        [Range(0f, 1f)]
        [SerializeField] private float lowMinBrightness = 0.45f;

        [Header("Barras")]
        [SerializeField] private Color barBackgroundColor = new Color(0.12f, 0.12f, 0.15f, 1f);
        [SerializeField] private Color barFrameColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private StatBarVisual[] bars = new StatBarVisual[0];

        [Header("Débito de Estudo (G03: valores placeholder)")]
        [SerializeField] private int maxDebt = 14;
        [Range(0f, 1f)]
        [SerializeField] private float emptyAlpha = 0.3f;
        [SerializeField] private int debtPixelSteps = 32;

        [Header("Retrato (G03: origem placeholder)")]
        [SerializeField] private PortraitSourceMode portraitSource = PortraitSourceMode.LowestStat;
        [SerializeField] private StatType portraitStat = StatType.Health;
        [SerializeField] private PortraitState[] portraitStates = new PortraitState[0];

        public float FillDuration => fillDuration;
        public bool EaseOut => easeOut;
        public int LowThreshold => lowThreshold;
        public float BlinkSpeed => blinkSpeed;
        public float LowMinBrightness => lowMinBrightness;
        public Color BarBackgroundColor => barBackgroundColor;
        public Color BarFrameColor => barFrameColor;
        public StatBarVisual[] Bars => bars;
        public int MaxDebt => maxDebt;
        public float EmptyAlpha => emptyAlpha;
        public int DebtPixelSteps => debtPixelSteps;
        public PortraitSourceMode PortraitSource => portraitSource;
        public StatType PortraitStat => portraitStat;
        public PortraitState[] PortraitStates => portraitStates;

        public bool TryGetBar(StatType stat, out StatBarVisual visual)
        {
            if (bars != null)
            {
                for (int i = 0; i < bars.Length; i++)
                {
                    if (bars[i].stat != stat) continue;
                    visual = bars[i];
                    return true;
                }
            }
            visual = default;
            return false;
        }

        // Used by editor tools, which cannot touch the serialized fields directly.
        public void Setup(StatBarVisual[] bars, PortraitState[] portraitStates)
        {
            this.bars = bars ?? new StatBarVisual[0];
            this.portraitStates = portraitStates ?? new PortraitState[0];
            OnValidate();
        }

        private void OnValidate()
        {
            if (fillDuration < 0f) fillDuration = 0f;
            if (blinkSpeed < 0f) blinkSpeed = 0f;
            if (maxDebt < 0) maxDebt = 0;

            // Hint only: nothing is changed or removed.
            if (bars == null) return;
            for (int i = 0; i < bars.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (bars[j].stat != bars[i].stat) continue;
                    Debug.LogWarning($"{name}: 'bars' lists {DisplayNames.Of(bars[i].stat)} more than once.", this);
                    break;
                }
            }
        }
    }
}
