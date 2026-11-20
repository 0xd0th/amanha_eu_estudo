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
        // (old, new). Fires only when the debt changes, plus (0, 0) once per BeginNewGame as the initial value.
        public event Action<int, int> OnStudyDebtChanged;
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
        // Order: normal deltas (gains scaled by performance and gain factors) -> low score deltas (no
        // multiplier) -> study debt -> exhaustion -> advance period. A stat reaching the minimum triggers
        // burnout right there and everything after it (remaining deltas, debt, exhaustion) is skipped.
        public bool ApplyChoice(ChoiceData choice, float performanceMultiplier = 1f)
        {
            if (!IsAvailable(choice)) return false;

            float multiplier = Mathf.Max(0f, performanceMultiplier);
            foreach (var delta in choice.Deltas)
            {
                // Only gains scale with performance; costs are the time spent and always apply in full.
                // Single rounding: base x performance x factor.
                int amount = delta.amount > 0
                    ? Mathf.RoundToInt(delta.amount * multiplier * choice.GetGainFactor(delta.stat))
                    : delta.amount;
                if (ChangeStat(delta.stat, amount)) return true;
            }

            bool lowScore = choice.LowScoreThreshold > 0f && multiplier < choice.LowScoreThreshold;
            if (lowScore)
            {
                foreach (var delta in choice.LowScoreDeltas)
                {
                    if (ChangeStat(delta.stat, delta.amount)) return true;
                }
            }

            ChangeStudyDebt(choice.DebtDelta);

            if (choice.CausesExhaustionNextDay || (lowScore && choice.LowScoreCausesExhaustion))
                State.exhaustedTomorrow = true;
            AdvancePeriod();
            return true;
        }

        // Hook for the minigames: true when the choice must be played at maximum difficulty.
        public bool IsMaxDifficultyForced(ChoiceData choice)
        {
            return choice != null && choice.ForceMaxDifficulty;
        }

        // First game start. The constructor stays silent because nobody can be subscribed yet;
        // GameManager calls this from Start so listeners (subscribed in OnEnable) get the initial values.
        public void StartGame()
        {
            BeginNewGame(restarted: false);
        }

        // Works from any phase (Playing, GameOver, FinalDayReached).
        public void Restart()
        {
            BeginNewGame(restarted: true);
        }

        // Single path for start and restart, so both always announce the same sequence:
        // [OnGameRestarted] -> OnStatChanged (old == new) per stat -> OnStudyDebtChanged(0, 0)
        // -> OnDayStarted(1) -> OnPeriodChanged(Morning).
        private void BeginNewGame(bool restarted)
        {
            State.Reset(Config);
            if (restarted) OnGameRestarted?.Invoke();

            for (int i = 0; i < EnumExtensions.StatCount; i++)
            {
                int value = State.stats[i];
                OnStatChanged?.Invoke((StatType)i, value, value);
            }
            OnStudyDebtChanged?.Invoke(State.studyDebt, State.studyDebt);
            OnDayStarted?.Invoke(State.day);
            OnPeriodChanged?.Invoke(State.period);
        }

        private void ChangeStudyDebt(int amount)
        {
            int oldValue = State.studyDebt;
            int newValue = Math.Max(0, oldValue + amount);
            if (newValue == oldValue) return;
            State.studyDebt = newValue;
            OnStudyDebtChanged?.Invoke(oldValue, newValue);
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
