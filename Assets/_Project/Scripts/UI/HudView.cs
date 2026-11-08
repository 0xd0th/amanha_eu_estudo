using AmanhaEuEstudo.Core;
using UnityEngine;

namespace AmanhaEuEstudo.UI
{
    // Orchestrates the HUD views. No rules here: everything is driven by GameManager events.
    // Subscribes in OnEnable (before any Start) so it also receives the opening event sequence.
    public class HudView : MonoBehaviour
    {
        [SerializeField] private HudConfig config;
        [SerializeField] private StatBarView[] bars = new StatBarView[0];
        [SerializeField] private DebtPileView debtPile;
        [SerializeField] private CalendarView calendar;
        [SerializeField] private PortraitView portrait;

        private GameManager manager;

        public DebtPileView DebtPile => debtPile;

        public StatBarView GetBar(StatType stat)
        {
            for (int i = 0; i < bars.Length; i++)
                if (bars[i] != null && bars[i].Stat == stat) return bars[i];
            return null;
        }

        private void OnEnable()
        {
            manager = GameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("HudView needs a GameManager in the scene; disabling.", this);
                enabled = false;
                return;
            }

            ConfigureViews();
            manager.OnStatChanged += HandleStatChanged;
            manager.OnDayStarted += HandleDayStarted;
            manager.OnPeriodChanged += HandlePeriodChanged;
            manager.OnGameRestarted += ApplyStateInstant;
            ApplyStateInstant();
        }

        private void OnDisable()
        {
            if (manager == null) return;
            manager.OnStatChanged -= HandleStatChanged;
            manager.OnDayStarted -= HandleDayStarted;
            manager.OnPeriodChanged -= HandlePeriodChanged;
            manager.OnGameRestarted -= ApplyStateInstant;
            manager = null;
        }

        private void ConfigureViews()
        {
            var gameConfig = manager.Config;
            for (int i = 0; i < bars.Length; i++)
                if (bars[i] != null) bars[i].Configure(config, bars[i].Stat, gameConfig.MinValue, gameConfig.MaxValue);
            if (debtPile != null) debtPile.Configure(config);
            if (portrait != null) portrait.Configure(config);
        }

        // Reads the full state once, without animation (enable and restart).
        private void ApplyStateInstant()
        {
            var state = manager != null ? manager.State : null;
            if (state == null) return;

            for (int i = 0; i < bars.Length; i++)
                if (bars[i] != null) bars[i].SetInstant(state.GetStat(bars[i].Stat));
            if (calendar != null)
            {
                calendar.SetDay(state.day, manager.Config.TotalDays);
                calendar.SetPeriod(state.period);
            }
            if (portrait != null) portrait.Refresh(state.stats);
        }

        private void HandleStatChanged(StatType stat, int oldValue, int newValue)
        {
            var bar = GetBar(stat);
            if (bar != null) bar.AnimateTo(newValue);
            if (portrait != null && manager.State != null) portrait.Refresh(manager.State.stats);
        }

        private void HandleDayStarted(int day)
        {
            if (calendar != null) calendar.SetDay(day, manager.Config.TotalDays);
        }

        private void HandlePeriodChanged(TimeOfDay period)
        {
            if (calendar != null) calendar.SetPeriod(period);
        }
    }
}
