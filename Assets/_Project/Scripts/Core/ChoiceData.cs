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

        public string Id => id;
        public string DisplayName => displayName;
        public PeriodMask Periods => periods;
        public ChoiceAvailability Availability => availability;
        public StatDelta[] Deltas => deltas;
        public bool CausesExhaustionNextDay => causesExhaustionNextDay;

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
            ChoiceAvailability availability, StatDelta[] deltas, bool causesExhaustionNextDay = false)
        {
            this.id = id;
            this.displayName = displayName;
            this.periods = periods;
            this.availability = availability;
            this.deltas = deltas ?? new StatDelta[0];
            this.causesExhaustionNextDay = causesExhaustionNextDay;
        }
    }
}
