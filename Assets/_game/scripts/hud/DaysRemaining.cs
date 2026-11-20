using System;

namespace AmanhaEuEstudo.UI
{
    public static class DaysRemaining
    {
        // Day 1 of 14 -> 14; day 14 -> 1.
        public static int Compute(int totalDays, int day)
        {
            return Math.Max(0, totalDays - day + 1);
        }

        // The calendar itself shows only the number.
        public static string Number(int remaining)
        {
            return remaining.ToString();
        }

        // Supporting text next to the calendar.
        public static string Format(int remaining)
        {
            return remaining == 1 ? "Falta 1 dia" : $"Faltam {remaining} dias";
        }
    }
}
