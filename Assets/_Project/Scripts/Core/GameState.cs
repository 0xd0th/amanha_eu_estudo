using System;

namespace AmanhaEuEstudo.Core
{
    // Plain serializable data (JsonUtility friendly: no Dictionary). Rules live in GameRules.
    [Serializable]
    public class GameState
    {
        public int day = 1;
        public TimeOfDay period = TimeOfDay.Morning;
        public int[] stats = new int[EnumExtensions.StatCount];
        public bool exhaustedToday;
        // Set by a choice during the day, becomes exhaustedToday when the day turns.
        public bool exhaustedTomorrow;
        public GamePhase phase = GamePhase.Playing;
        // -1 while there is no burnout; otherwise a StatType value.
        public int burnoutStat = -1;

        public int GetStat(StatType stat)
        {
            return stats[(int)stat];
        }

        public StatType? BurnoutStat => burnoutStat >= 0 ? (StatType?)burnoutStat : null;

        public void Reset(GameConfig config)
        {
            day = 1;
            period = TimeOfDay.Morning;
            if (stats == null || stats.Length != EnumExtensions.StatCount)
                stats = new int[EnumExtensions.StatCount];
            for (int i = 0; i < stats.Length; i++) stats[i] = config.InitialValue;
            exhaustedToday = false;
            exhaustedTomorrow = false;
            phase = GamePhase.Playing;
            burnoutStat = -1;
        }
    }
}
