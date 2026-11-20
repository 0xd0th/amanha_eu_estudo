using System;

namespace AmanhaEuEstudo.UI
{
    // Pixel art cannot show half a pixel: a fill is rounded to a whole number of "steps" (pixels of the bar).
    public static class PixelSnap
    {
        // steps <= 0 disables the snap (the value is only clamped to 0..1).
        public static float Quantize(float value01, int steps)
        {
            float v = Math.Min(1f, Math.Max(0f, value01));
            if (steps <= 0) return v;
            return (float)Math.Round(v * steps, MidpointRounding.AwayFromZero) / steps;
        }
    }
}
