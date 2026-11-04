using System;

namespace AmanhaEuEstudo.UI
{
    // Pure C# fill animation: fixed duration with optional ease-out. Retargeting mid-animation continues
    // from the value currently displayed (no jump), and the value never goes past the target.
    public sealed class BarFillAnimation
    {
        private float start;
        private float target;
        private float elapsed;

        public float Duration { get; set; }
        public bool EaseOut { get; set; }
        public float Current { get; private set; }
        public float Target => target;
        public bool IsAnimating { get; private set; }

        public BarFillAnimation(float duration, bool easeOut = true)
        {
            Duration = duration;
            EaseOut = easeOut;
        }

        // Jumps to the value and cancels any running animation.
        public void Snap(float value)
        {
            start = target = Current = value;
            elapsed = 0f;
            IsAnimating = false;
        }

        public void SetTarget(float value)
        {
            if (Duration <= 0f || value == Current)
            {
                Snap(value);
                return;
            }
            start = Current;
            target = value;
            elapsed = 0f;
            IsAnimating = true;
        }

        // Returns true on the tick in which the animation finishes.
        public bool Tick(float deltaTime)
        {
            if (!IsAnimating) return false;
            elapsed += Math.Max(0f, deltaTime);
            if (elapsed >= Duration)
            {
                Current = target;
                IsAnimating = false;
                return true;
            }
            float t = elapsed / Duration;
            if (EaseOut) t = 1f - (1f - t) * (1f - t) * (1f - t);
            Current = start + (target - start) * t;
            return false;
        }
    }
}
