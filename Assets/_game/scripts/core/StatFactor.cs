using System;
using UnityEngine;

namespace AmanhaEuEstudo.Core
{
    // Multiplier applied to the positive deltas of one stat. A stat that is not listed has factor 1.
    [Serializable]
    public struct StatFactor
    {
        public StatType stat;
        public float factor;

        public StatFactor(StatType stat, float factor)
        {
            this.stat = stat;
            this.factor = factor;
        }

        // Editor-time hint only: nothing is changed or removed. Duplicates multiply each other and a
        // negative factor is treated as 0 by the rules; both are usually data-entry mistakes.
        public static void WarnInvalid(StatFactor[] factors, UnityEngine.Object owner, string field)
        {
            if (factors == null) return;
            for (int i = 0; i < factors.Length; i++)
            {
                if (factors[i].factor < 0f)
                    Debug.LogWarning($"{owner.name}: '{field}' has a negative factor for {DisplayNames.Of(factors[i].stat)}.", owner);
                for (int j = 0; j < i; j++)
                {
                    if (factors[j].stat != factors[i].stat) continue;
                    Debug.LogWarning($"{owner.name}: '{field}' lists {DisplayNames.Of(factors[i].stat)} more than once.", owner);
                    break;
                }
            }
        }
    }
}
