using System;
using UnityEngine;

namespace AmanhaEuEstudo.Core
{
    [Serializable]
    public struct StatDelta
    {
        public StatType stat;
        public int amount;

        public StatDelta(StatType stat, int amount)
        {
            this.stat = stat;
            this.amount = amount;
        }

        // Editor-time hint only: nothing is changed or removed. Duplicates are legal (amounts add up)
        // but usually a data-entry mistake.
        public static void WarnDuplicateStats(StatDelta[] deltas, UnityEngine.Object owner, string field)
        {
            if (deltas == null) return;
            for (int i = 0; i < deltas.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (deltas[j].stat != deltas[i].stat) continue;
                    Debug.LogWarning($"{owner.name}: '{field}' lists {DisplayNames.Of(deltas[i].stat)} more than once.", owner);
                    break;
                }
            }
        }
    }

    [CreateAssetMenu(fileName = "Choice_New", menuName = "Amanhã eu estudo/Choice Data")]
    public class ChoiceData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private PeriodMask periods = PeriodMask.All;
        [SerializeField] private ChoiceAvailability availability = ChoiceAvailability.Always;
        [SerializeField] private StatDelta[] deltas = new StatDelta[0];
        [Tooltip("If set, the player wakes up exhausted on the next day (resolved when the day turns).")]
        [SerializeField] private bool causesExhaustionNextDay;

        [Header("Study Debt")]
        [Tooltip("Study Debt added by this choice (consequence, not a choice). Debt never goes below 0. Not applied if the choice causes Burnout.")]
        [SerializeField] private int debtDelta;

        [Header("Low score")]
        [Tooltip("When > 0 and the performance multiplier is below this value, the low score deltas apply. 0 = off. PLACEHOLDER, pending team decision (G03).")]
        [Min(0f)]
        [SerializeField] private float lowScoreThreshold;
        [Tooltip("Added on top of the normal deltas when the score is low. Never scaled by performance.")]
        [SerializeField] private StatDelta[] lowScoreDeltas = new StatDelta[0];
        [Tooltip("If set, a low score also makes the player exhausted on the next day.")]
        [SerializeField] private bool lowScoreCausesExhaustion;

        [Header("Gain factors and difficulty")]
        [Tooltip("Multiplies only the positive deltas of the listed stat (stat x performance x factor, rounded once). Unlisted stat = 1.")]
        [SerializeField] private StatFactor[] gainFactors = new StatFactor[0];
        [Tooltip("When set, the minigame of this choice must run at maximum difficulty (see GameRules.IsMaxDifficultyForced).")]
        [SerializeField] private bool forceMaxDifficulty;

        public string Id => id;
        public string DisplayName => displayName;
        public PeriodMask Periods => periods;
        public ChoiceAvailability Availability => availability;
        public StatDelta[] Deltas => deltas;
        public bool CausesExhaustionNextDay => causesExhaustionNextDay;
        public int DebtDelta => debtDelta;
        public float LowScoreThreshold => lowScoreThreshold;
        public StatDelta[] LowScoreDeltas => lowScoreDeltas;
        public bool LowScoreCausesExhaustion => lowScoreCausesExhaustion;
        public StatFactor[] GainFactors => gainFactors;
        public bool ForceMaxDifficulty => forceMaxDifficulty;

        private void OnValidate()
        {
            StatDelta.WarnDuplicateStats(deltas, this, nameof(deltas));
            StatDelta.WarnDuplicateStats(lowScoreDeltas, this, nameof(lowScoreDeltas));
            StatFactor.WarnInvalid(gainFactors, this, nameof(gainFactors));
        }

        // Factor applied to the positive deltas of a stat: 1 when the stat is not listed. A stat listed
        // twice multiplies (the Inspector warns); a negative factor counts as 0.
        public float GetGainFactor(StatType stat)
        {
            float result = 1f;
            if (gainFactors == null) return result;
            foreach (var f in gainFactors)
            {
                if (f.stat == stat) result *= Mathf.Max(0f, f.factor);
            }
            return result;
        }

        public bool AppearsIn(TimeOfDay period)
        {
            return (periods & period.ToMask()) != 0;
        }

        public bool IsAvailable(TimeOfDay period, bool exhausted)
        {
            if (!AppearsIn(period)) return false;
            switch (availability)
            {
                case ChoiceAvailability.RestedOnly: return !exhausted;
                case ChoiceAvailability.ExhaustedOnly: return exhausted;
                default: return true;
            }
        }

        // Used by editor tools and tests, which cannot touch the serialized fields directly.
        public void Setup(string id, string displayName, PeriodMask periods,
            ChoiceAvailability availability, StatDelta[] deltas, bool causesExhaustionNextDay = false,
            int debtDelta = 0, float lowScoreThreshold = 0f, StatDelta[] lowScoreDeltas = null,
            bool lowScoreCausesExhaustion = false, StatFactor[] gainFactors = null, bool forceMaxDifficulty = false)
        {
            this.id = id;
            this.displayName = displayName;
            this.periods = periods;
            this.availability = availability;
            this.deltas = deltas ?? new StatDelta[0];
            this.causesExhaustionNextDay = causesExhaustionNextDay;
            this.debtDelta = debtDelta;
            this.lowScoreThreshold = Mathf.Max(0f, lowScoreThreshold);
            this.lowScoreDeltas = lowScoreDeltas ?? new StatDelta[0];
            this.lowScoreCausesExhaustion = lowScoreCausesExhaustion;
            this.gainFactors = gainFactors ?? new StatFactor[0];
            this.forceMaxDifficulty = forceMaxDifficulty;
        }
    }
}
