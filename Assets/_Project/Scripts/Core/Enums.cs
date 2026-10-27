using System;

namespace AmanhaEuEstudo.Core
{
    // Names are provisional (team still deciding). Values index GameState.stats and
    // GameConfig.dailyDecay, so keep them contiguous from 0 and update StatCount if you add one.
    public enum StatType
    {
        Health = 0,
        Intelligence = 1,
        Psychological = 2,
    }

    public enum TimeOfDay
    {
        Morning = 0,
        Afternoon = 1,
        Night = 2,
    }

    [Flags]
    public enum PeriodMask
    {
        None = 0,
        Morning = 1 << 0,
        Afternoon = 1 << 1,
        Night = 1 << 2,
        All = Morning | Afternoon | Night,
    }

    public enum ChoiceAvailability
    {
        Always,
        RestedOnly,
        ExhaustedOnly,
    }

    public enum GamePhase
    {
        Playing,
        GameOver,
        FinalDayReached,
    }

    public static class EnumExtensions
    {
        public const int StatCount = 3;

        public static PeriodMask ToMask(this TimeOfDay period)
        {
            return (PeriodMask)(1 << (int)period);
        }
    }
}
