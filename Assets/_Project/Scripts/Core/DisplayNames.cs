namespace AmanhaEuEstudo.Core
{
    // Single place for user-facing names of stats and periods.
    public static class DisplayNames
    {
        public static string Of(StatType stat)
        {
            switch (stat)
            {
                case StatType.Health: return "Saúde";
                case StatType.Intelligence: return "Inteligência";
                case StatType.Psychological: return "Psicológico";
                default: return stat.ToString();
            }
        }

        public static string Of(TimeOfDay period)
        {
            switch (period)
            {
                case TimeOfDay.Morning: return "Manhã";
                case TimeOfDay.Afternoon: return "Tarde";
                case TimeOfDay.Night: return "Noite";
                default: return period.ToString();
            }
        }
    }
}
