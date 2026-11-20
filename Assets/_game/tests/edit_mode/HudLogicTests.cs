using AmanhaEuEstudo.Core;
using AmanhaEuEstudo.UI;
using NUnit.Framework;

namespace AmanhaEuEstudo.Tests
{
    public class BarFillAnimationTests
    {
        [Test]
        public void ReachesTargetAfterDuration()
        {
            var a = new BarFillAnimation(0.4f);
            a.Snap(50);
            a.SetTarget(80);
            Assert.IsTrue(a.IsAnimating);
            Assert.IsFalse(a.Tick(0.2f));
            Assert.Greater(a.Current, 50f);
            Assert.Less(a.Current, 80f);
            Assert.IsTrue(a.Tick(0.2f));
            Assert.AreEqual(80f, a.Current);
            Assert.IsFalse(a.IsAnimating);
        }

        [Test]
        public void EaseOutIsAheadOfLinearAtTheMidpoint()
        {
            var a = new BarFillAnimation(1f, easeOut: true);
            a.Snap(0);
            a.SetTarget(100);
            a.Tick(0.5f);
            Assert.Greater(a.Current, 50f);
        }

        [Test]
        public void NeverOvershootsGoingUp()
        {
            var a = new BarFillAnimation(0.4f);
            a.Snap(10);
            a.SetTarget(90);
            for (int i = 0; i < 50; i++)
            {
                a.Tick(0.03f);
                Assert.LessOrEqual(a.Current, 90f);
            }
            Assert.AreEqual(90f, a.Current);
        }

        [Test]
        public void NeverOvershootsGoingDown()
        {
            var a = new BarFillAnimation(0.4f);
            a.Snap(90);
            a.SetTarget(10);
            for (int i = 0; i < 50; i++)
            {
                a.Tick(0.03f);
                Assert.GreaterOrEqual(a.Current, 10f);
            }
            Assert.AreEqual(10f, a.Current);
        }

        [Test]
        public void HugeDeltaTimeStillStopsAtTarget()
        {
            var a = new BarFillAnimation(0.4f);
            a.Snap(0);
            a.SetTarget(70);
            a.Tick(100f);
            Assert.AreEqual(70f, a.Current);
        }

        [Test]
        public void InterruptionContinuesFromCurrentValue()
        {
            var a = new BarFillAnimation(1f);
            a.Snap(0);
            a.SetTarget(100);
            a.Tick(0.5f);
            float mid = a.Current;
            a.SetTarget(20);
            Assert.AreEqual(mid, a.Current, 0.0001f); // no jump
            a.Tick(0.1f);
            Assert.Less(a.Current, mid);
            Assert.GreaterOrEqual(a.Current, 20f);
            a.Tick(2f);
            Assert.AreEqual(20f, a.Current);
        }

        [Test]
        public void ZeroDurationGoesStraightToTarget()
        {
            var a = new BarFillAnimation(0f);
            a.Snap(10);
            a.SetTarget(60);
            Assert.AreEqual(60f, a.Current);
            Assert.IsFalse(a.IsAnimating);
        }

        [Test]
        public void SnapCancelsTheAnimation()
        {
            var a = new BarFillAnimation(0.4f);
            a.Snap(0);
            a.SetTarget(100);
            a.Tick(0.1f);
            a.Snap(42);
            Assert.AreEqual(42f, a.Current);
            Assert.IsFalse(a.IsAnimating);
            a.Tick(1f);
            Assert.AreEqual(42f, a.Current);
        }

        [Test]
        public void SettingTheCurrentValueDoesNotAnimate()
        {
            var a = new BarFillAnimation(0.4f);
            a.Snap(50);
            a.SetTarget(50);
            Assert.IsFalse(a.IsAnimating);
        }
    }

    public class PixelSnapTests
    {
        [Test]
        public void Edges()
        {
            Assert.AreEqual(0f, PixelSnap.Quantize(0f, 48));
            Assert.AreEqual(1f, PixelSnap.Quantize(1f, 48));
        }

        [Test]
        public void ClampsOutOfRange()
        {
            Assert.AreEqual(0f, PixelSnap.Quantize(-0.5f, 48));
            Assert.AreEqual(1f, PixelSnap.Quantize(1.5f, 48));
        }

        [Test]
        public void RoundsToNearestStep()
        {
            Assert.AreEqual(0.25f, PixelSnap.Quantize(0.26f, 4), 0.0001f);
            Assert.AreEqual(0.5f, PixelSnap.Quantize(0.4f, 4), 0.0001f);
            Assert.AreEqual(0.25f, PixelSnap.Quantize(0.37f, 4), 0.0001f);
            Assert.AreEqual(0.1f, PixelSnap.Quantize(0.104f, 10), 0.0001f);
        }

        [Test]
        public void NonPositiveStepsDisableSnap()
        {
            Assert.AreEqual(0.123f, PixelSnap.Quantize(0.123f, 0), 0.0001f);
            Assert.AreEqual(0.123f, PixelSnap.Quantize(0.123f, -5), 0.0001f);
        }
    }

    public class DaysRemainingTests
    {
        [Test]
        public void FirstAndLastDay()
        {
            Assert.AreEqual(14, DaysRemaining.Compute(14, 1));
            Assert.AreEqual(1, DaysRemaining.Compute(14, 14));
        }

        [Test]
        public void SingularAndPlural()
        {
            Assert.AreEqual("Falta 1 dia", DaysRemaining.Format(1));
            Assert.AreEqual("Faltam 14 dias", DaysRemaining.Format(14));
            Assert.AreEqual("Faltam 2 dias", DaysRemaining.Format(2));
        }

        [Test]
        public void CalendarNumberIsJustTheNumber()
        {
            Assert.AreEqual("14", DaysRemaining.Number(14));
        }
    }

    public class DebtFillTests
    {
        [Test]
        public void Fraction()
        {
            Assert.AreEqual(0.7f, DebtFill.Compute(70, 100), 0.0001f);
        }

        [Test]
        public void AboveMaxIsFull()
        {
            Assert.AreEqual(1f, DebtFill.Compute(150, 100));
        }

        [Test]
        public void NegativeIsEmpty()
        {
            Assert.AreEqual(0f, DebtFill.Compute(-3, 100));
        }

        [Test]
        public void ZeroMaxIsEmpty()
        {
            Assert.AreEqual(0f, DebtFill.Compute(5, 0));
        }
    }

    public class PortraitSelectorTests
    {
        private static readonly int[] Thresholds = { 0, 25, 60 };

        [Test]
        public void LowestStatModeUsesTheSmallestBar()
        {
            int[] stats = { 90, 20, 70 };
            Assert.AreEqual(20, PortraitSelector.SourceValue(stats, PortraitSourceMode.LowestStat, StatType.Health));
            Assert.AreEqual(0, PortraitSelector.Select(stats, PortraitSourceMode.LowestStat, StatType.Health, Thresholds));
        }

        [Test]
        public void SpecificStatModeUsesTheConfiguredBar()
        {
            int[] stats = { 90, 20, 70 };
            Assert.AreEqual(90, PortraitSelector.SourceValue(stats, PortraitSourceMode.SpecificStat, StatType.Health));
            Assert.AreEqual(2, PortraitSelector.Select(stats, PortraitSourceMode.SpecificStat, StatType.Health, Thresholds));
            Assert.AreEqual(0, PortraitSelector.Select(stats, PortraitSourceMode.SpecificStat, StatType.Intelligence, Thresholds));
        }

        [Test]
        public void PicksTheCorrectStateAtTheThresholds()
        {
            Assert.AreEqual(0, Pick(0));
            Assert.AreEqual(0, Pick(24));
            Assert.AreEqual(1, Pick(25));
            Assert.AreEqual(1, Pick(59));
            Assert.AreEqual(2, Pick(60));
            Assert.AreEqual(2, Pick(100));
        }

        [Test]
        public void StateOrderInTheListDoesNotMatter()
        {
            int[] shuffled = { 60, 0, 25 };
            int[] stats = { 30, 30, 30 };
            Assert.AreEqual(2, PortraitSelector.Select(stats, PortraitSourceMode.LowestStat, StatType.Health, shuffled));
        }

        [Test]
        public void BelowEveryThresholdFallsBackToTheLowestState()
        {
            int[] thresholds = { 50, 80 };
            int[] stats = { 10, 10, 10 };
            Assert.AreEqual(0, PortraitSelector.Select(stats, PortraitSourceMode.LowestStat, StatType.Health, thresholds));
        }

        [Test]
        public void NoStatesReturnsMinusOne()
        {
            int[] stats = { 10, 10, 10 };
            Assert.AreEqual(-1, PortraitSelector.Select(stats, PortraitSourceMode.LowestStat, StatType.Health, new int[0]));
        }

        private static int Pick(int value)
        {
            int[] stats = { value, 100, 100 };
            return PortraitSelector.Select(stats, PortraitSourceMode.LowestStat, StatType.Health, Thresholds);
        }
    }
}
