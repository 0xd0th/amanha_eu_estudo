using System;

namespace AmanhaEuEstudo.UI
{
    public static class DebtFill
    {
        // 0..1. Debt above the maximum is a full pile; maxDebt <= 0 or negative debt is empty.
        public static float Compute(int debt, int maxDebt)
        {
            if (maxDebt <= 0 || debt <= 0) return 0f;
            return Math.Min(1f, (float)debt / maxDebt);
        }
    }
}
