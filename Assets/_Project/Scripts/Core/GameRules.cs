using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmanhaEuEstudo.Core
{
    // Pure game rules, no scene needed. GameManager is a thin MonoBehaviour wrapper around this.
    public sealed class GameRules
    {
        public GameConfig Config { get; }
        public GameState State { get; }

        public event Action<StatType, int, int> OnStatChanged;
        public event Action<TimeOfDay> OnPeriodChanged;
        public event Action<int> OnDayStarted;
        public event Action<int> OnDayEnded;
        public event Action<StatType> OnBurnout;
        public event Action OnFinalDayReached;
        public event Action OnGameRestarted;

        public GameRules(GameConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            State = new GameState();
            State.Reset(config);
        }

        public bool IsAvailable(ChoiceData choice)
        {
            return choice != null
                && State.phase == GamePhase.Playing
                && choice.IsAvailable(State.period, State.exhaustedToday);
        }

        public List<ChoiceData> GetAvailableChoices()
        {
            var result = new List<ChoiceData>();
            foreach (var choice in Config.Choices)
            {
                if (IsAvailable(choice)) result.Add(choice);
            }
            return result;
        }

        // Returns false (and changes nothing) if the game is over or the choice is not available now.
        // Deltas are applied in order; a stat reaching the minimum triggers burnout right there and
        // the remaining deltas are skipped.
        public bool ApplyChoice(ChoiceData choice, float performanceMultiplier = 1f)
        {
            if (!IsAvailable(choice)) return false;

            float multiplier = Mathf.Max(0f, performanceMultiplier);
            foreach (var delta in choice.Deltas)
            {
                // Only gains scale with performance; costs are the time spent and always apply in full.
                int amount = delta.amount > 0
                    ? Mathf.RoundToInt(delta.amount * multiplier)
                    : delta.amount;
                if (ChangeStat(delta.stat, amount)) return true;
            }

            if (choice.CausesExhaustionNextDay) State.exhaustedTomorrow = true;
            AdvancePeriod();
            return true;
        }

        public void Restart()
        {
            State.Reset(Config);
            OnGameRestarted?.Invoke();
        }

        private void AdvancePeriod()
        {
            if (State.period != TimeOfDay.Night)
            {
                State.period = State.period + 1;
                OnPeriodChanged?.Invoke(State.period);
                return;
            }
            EndDay();
        }

        private void EndDay()
        {
            OnDayEnded?.Invoke(State.day);

            for (int i = 0; i < EnumExtensions.StatCount; i++)
            {
                var stat = (StatType)i;
                if (ChangeStat(stat, Config.GetDailyDecay(stat))) return;
            }

            State.exhaustedToday = State.exhaustedTomorrow;
            State.exhaustedTomorrow = false;

            // The day counter stays at TotalDays when the final day is reached.
            if (State.day >= Config.TotalDays)
            {
                State.phase = GamePhase.FinalDayReached;
                OnFinalDayReached?.Invoke();
                return;
            }

            State.day++;
            State.period = TimeOfDay.Morning;
            OnDayStarted?.Invoke(State.day);
            OnPeriodChanged?.Invoke(State.period);
        }

        // Returns true if this change caused burnout.
        private bool ChangeStat(StatType stat, int amount)
        {
            int index = (int)stat;
            int oldValue = State.stats[index];
            int newValue = Config.Clamp(oldValue + amount);

            if (newValue != oldValue)
            {
                State.stats[index] = newValue;
                OnStatChanged?.Invoke(stat, oldValue, newValue);
            }

            if (newValue <= Config.MinValue)
            {
                State.phase = GamePhase.GameOver;
                State.burnoutStat = index;
                OnBurnout?.Invoke(stat);
                return true;
            }
            return false;
        }
    }
}
