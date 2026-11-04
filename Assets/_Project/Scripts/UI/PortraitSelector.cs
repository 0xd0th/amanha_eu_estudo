using System;
using System.Collections.Generic;
using AmanhaEuEstudo.Core;

namespace AmanhaEuEstudo.UI
{
    public enum PortraitSourceMode
    {
        LowestStat,
        SpecificStat,
    }

    public static class PortraitSelector
    {
        // Value the portrait reacts to: the lowest bar, or one configured bar.
        public static int SourceValue(IReadOnlyList<int> stats, PortraitSourceMode mode, StatType specific)
        {
            if (stats == null || stats.Count == 0) return 0;
            if (mode == PortraitSourceMode.SpecificStat)
            {
                int index = (int)specific;
                return index >= 0 && index < stats.Count ? stats[index] : 0;
            }
            int lowest = stats[0];
            for (int i = 1; i < stats.Count; i++) lowest = Math.Min(lowest, stats[i]);
            return lowest;
        }

        // Picks the state with the highest minValue that is still <= the source value.
        // If none qualifies (value below every minValue), falls back to the state with the lowest minValue.
        // Returns -1 when there are no states.
        public static int Select(IReadOnlyList<int> stats, PortraitSourceMode mode, StatType specific,
            IReadOnlyList<int> stateMinValues)
        {
            if (stateMinValues == null || stateMinValues.Count == 0) return -1;
            int value = SourceValue(stats, mode, specific);

            int best = -1;
            int lowest = 0;
            for (int i = 0; i < stateMinValues.Count; i++)
            {
                int min = stateMinValues[i];
                if (min < stateMinValues[lowest]) lowest = i;
                if (min <= value && (best < 0 || min > stateMinValues[best])) best = i;
            }
            return best >= 0 ? best : lowest;
        }
    }
}
