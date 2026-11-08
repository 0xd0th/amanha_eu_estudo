using AmanhaEuEstudo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AmanhaEuEstudo.UI
{
    // Study Debt book pile. The debt rules do not exist yet (another card): SetDebt is the hook for them.
    // Two images with the same sprite: the back one is the dimmed "empty" pile, the front one is filled
    // from the bottom up.
    public class DebtPileView : MonoBehaviour
    {
        [SerializeField] private HudConfig config;
        [SerializeField] private Image back;
        [SerializeField] private Image front;

        private BarFillAnimation animation;
        private int debt;

        public int Debt => debt;

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

        public void Configure(HudConfig newConfig)
        {
            config = newConfig;
            ApplyConfig();
            Render();
        }

        private void ApplyConfig()
        {
            if (config != null)
            {
                Anim.Duration = config.FillDuration;
                Anim.EaseOut = config.EaseOut;
            }
            if (back != null)
            {
                var c = back.color;
                c.a = config != null ? config.EmptyAlpha : 0.3f;
                back.color = c;
            }
            if (front != null)
            {
                front.type = Image.Type.Filled;
                front.fillMethod = Image.FillMethod.Vertical;
                front.fillOrigin = (int)Image.OriginVertical.Bottom;
            }
        }

        // Hook for the Study Debt card. Animates the pile to the new level.
        public void SetDebt(int newDebt)
        {
            debt = Mathf.Max(0, newDebt);
            Anim.SetTarget(DebtFill.Compute(debt, MaxDebt));
            Render();
        }

        public void SetDebtInstant(int newDebt)
        {
            debt = Mathf.Max(0, newDebt);
            Anim.Snap(DebtFill.Compute(debt, MaxDebt));
            Render();
        }

        private int MaxDebt => config != null ? config.MaxDebt : 0;

        private void Update()
        {
            if (!Anim.IsAnimating) return;
            Anim.Tick(Time.unscaledDeltaTime);
            Render();
        }

        private void Render()
        {
            if (front != null)
                front.fillAmount = PixelSnap.Quantize(Anim.Current, config != null ? config.DebtPixelSteps : 0);
        }
    }
}
