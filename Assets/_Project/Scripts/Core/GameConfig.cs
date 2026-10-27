using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmanhaEuEstudo.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Amanhã eu estudo/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private int initialValue = 60;
        [SerializeField] private int minValue = 0;
        [SerializeField] private int maxValue = 100;
        [Min(1)]
        [SerializeField] private int totalDays = 14;

        [Tooltip("Applied to each stat when a night ends, indexed by StatType. PLACEHOLDER values, pending team decision (G03).")]
        [SerializeField] private int[] dailyDecay = { -5, -5, -5 };

        [Tooltip("Catalog of every choice in the game. Availability is decided by each ChoiceData.")]
        [SerializeField] private List<ChoiceData> choices = new List<ChoiceData>();

        public int InitialValue => initialValue;
        public int MinValue => minValue;
        public int MaxValue => maxValue;
        public int TotalDays => totalDays;
        public IReadOnlyList<ChoiceData> Choices => choices;

        public int GetDailyDecay(StatType stat)
        {
            int i = (int)stat;
            return dailyDecay != null && i < dailyDecay.Length ? dailyDecay[i] : 0;
        }

        public int Clamp(int value)
        {
            return Mathf.Clamp(value, minValue, maxValue);
        }

        // Used by editor tools and tests, which cannot touch the serialized fields directly.
        public void Setup(int initialValue, int minValue, int maxValue, int totalDays,
            int[] dailyDecay, IEnumerable<ChoiceData> choices)
        {
            this.initialValue = initialValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.totalDays = totalDays;
            this.dailyDecay = dailyDecay ?? new int[EnumExtensions.StatCount];
            this.choices = choices != null ? new List<ChoiceData>(choices) : new List<ChoiceData>();
            OnValidate();
        }

        private void OnValidate()
        {
            if (dailyDecay == null || dailyDecay.Length != EnumExtensions.StatCount)
                Array.Resize(ref dailyDecay, EnumExtensions.StatCount);
            if (maxValue <= minValue) maxValue = minValue + 1;
            initialValue = Mathf.Clamp(initialValue, minValue, maxValue);
            if (totalDays < 1) totalDays = 1;
        }
    }
}
