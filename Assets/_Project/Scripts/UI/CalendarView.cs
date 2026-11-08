using AmanhaEuEstudo.Core;
using TMPro;
using UnityEngine;

namespace AmanhaEuEstudo.UI
{
    // Empty calendar sprite with the days-left number on top, plus the period and a supporting text.
    public class CalendarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private TMP_Text periodText;
        [SerializeField] private TMP_Text supportText;

        public void SetDay(int day, int totalDays)
        {
            int remaining = DaysRemaining.Compute(totalDays, day);
            if (numberText != null) numberText.text = DaysRemaining.Number(remaining);
            if (supportText != null) supportText.text = DaysRemaining.Format(remaining);
        }

        public void SetPeriod(TimeOfDay period)
        {
            if (periodText != null) periodText.text = DisplayNames.Of(period);
        }
    }
}
