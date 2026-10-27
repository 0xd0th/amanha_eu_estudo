using System.Collections.Generic;
using AmanhaEuEstudo.Core;
using NUnit.Framework;
using UnityEngine;

namespace AmanhaEuEstudo.Tests
{
    public class GameRulesTests
    {
        private readonly List<Object> created = new List<Object>();

        private ChoiceData Choice(string id, PeriodMask periods, ChoiceAvailability availability,
            bool exhausts = false, params StatDelta[] deltas)
        {
            var c = ScriptableObject.CreateInstance<ChoiceData>();
            c.Setup(id, id, periods, availability, deltas, exhausts);
            created.Add(c);
            return c;
        }

        private ChoiceData Neutral(string id, PeriodMask periods)
        {
            return Choice(id, periods, ChoiceAvailability.Always);
        }

        private GameRules Rules(int totalDays, int[] decay, params ChoiceData[] choices)
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var decayDeltas = new List<StatDelta>();
            for (int i = 0; i < decay.Length; i++) decayDeltas.Add(new StatDelta((StatType)i, decay[i]));
            config.Setup(60, 0, 100, totalDays, decayDeltas.ToArray(), choices);
            created.Add(config);
            return new GameRules(config);
        }

        private static readonly int[] NoDecay = { 0, 0, 0 };

        // Plays a neutral choice available in the current period.
        private static void Pass(GameRules rules)
        {
            var choices = rules.GetAvailableChoices();
            Assert.IsNotEmpty(choices);
            Assert.IsTrue(rules.ApplyChoice(choices[0]));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void InitialState_AllStatsAtInitialValue()
        {
            var rules = Rules(14, NoDecay, Neutral("n", PeriodMask.All));
            Assert.AreEqual(1, rules.State.day);
            Assert.AreEqual(TimeOfDay.Morning, rules.State.period);
            Assert.IsFalse(rules.State.exhaustedToday);
            Assert.AreEqual(GamePhase.Playing, rules.State.phase);
            for (int i = 0; i < EnumExtensions.StatCount; i++)
                Assert.AreEqual(60, rules.State.stats[i]);
        }

        [Test]
        public void Clamp_UpperBoundIs100()
        {
            var big = Choice("big", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Health, 999));
            var rules = Rules(14, NoDecay, big);
            rules.ApplyChoice(big);
            Assert.AreEqual(100, rules.State.GetStat(StatType.Health));
        }

        [Test]
        public void Clamp_LowerBoundIs0_AndCausesBurnout()
        {
            var huge = Choice("huge", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Intelligence, -999));
            var rules = Rules(14, NoDecay, huge);
            rules.ApplyChoice(huge);
            Assert.AreEqual(0, rules.State.GetStat(StatType.Intelligence));
        }

        [Test]
        public void Burnout_IsImmediate_AndBlocksFurtherChoices()
        {
            var deadly = Choice("deadly", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Health, -60),
                new StatDelta(StatType.Intelligence, 10));
            var other = Neutral("other", PeriodMask.All);
            var rules = Rules(14, NoDecay, deadly, other);

            var burnouts = new List<StatType>();
            rules.OnBurnout += burnouts.Add;

            Assert.IsTrue(rules.ApplyChoice(deadly));

            CollectionAssert.AreEqual(new[] { StatType.Health }, burnouts);
            Assert.AreEqual(GamePhase.GameOver, rules.State.phase);
            Assert.AreEqual(StatType.Health, rules.State.BurnoutStat);
            Assert.AreEqual(60, rules.State.GetStat(StatType.Intelligence), "later deltas must not apply");
            Assert.AreEqual(TimeOfDay.Morning, rules.State.period, "period must not advance");

            Assert.IsEmpty(rules.GetAvailableChoices());
            Assert.IsFalse(rules.ApplyChoice(other));
            Assert.AreEqual(0, rules.State.GetStat(StatType.Health));
        }

        [Test]
        public void Period_FlowsMorningAfternoonNightThenMorningOfDay2()
        {
            var rules = Rules(14, NoDecay, Neutral("n", PeriodMask.All));
            var periods = new List<TimeOfDay>();
            var daysStarted = new List<int>();
            var daysEnded = new List<int>();
            rules.OnPeriodChanged += periods.Add;
            rules.OnDayStarted += daysStarted.Add;
            rules.OnDayEnded += daysEnded.Add;

            Pass(rules);
            Assert.AreEqual(TimeOfDay.Afternoon, rules.State.period);
            Pass(rules);
            Assert.AreEqual(TimeOfDay.Night, rules.State.period);
            Pass(rules);

            Assert.AreEqual(2, rules.State.day);
            Assert.AreEqual(TimeOfDay.Morning, rules.State.period);
            CollectionAssert.AreEqual(
                new[] { TimeOfDay.Afternoon, TimeOfDay.Night, TimeOfDay.Morning }, periods);
            CollectionAssert.AreEqual(new[] { 1 }, daysEnded);
            CollectionAssert.AreEqual(new[] { 2 }, daysStarted);
        }

        [Test]
        public void DailyDecay_AppliedWhenDayTurns()
        {
            var rules = Rules(14, new[] { -5, -10, -1 }, Neutral("n", PeriodMask.All));
            Pass(rules); Pass(rules);
            Assert.AreEqual(60, rules.State.GetStat(StatType.Health), "no decay mid-day");
            Pass(rules);

            Assert.AreEqual(55, rules.State.GetStat(StatType.Health));
            Assert.AreEqual(50, rules.State.GetStat(StatType.Intelligence));
            Assert.AreEqual(59, rules.State.GetStat(StatType.Psychological));
        }

        [Test]
        public void DailyDecay_MissingStatIsZero_DuplicatesAdd()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            created.Add(config);
            config.Setup(60, 0, 100, 14, new[]
            {
                new StatDelta(StatType.Health, -3),
                new StatDelta(StatType.Health, -2),
            }, null);

            Assert.AreEqual(-5, config.GetDailyDecay(StatType.Health));
            Assert.AreEqual(0, config.GetDailyDecay(StatType.Intelligence));
        }

        [Test]
        public void DailyDecay_CanCauseBurnout_AndDayDoesNotStart()
        {
            var rules = Rules(14, new[] { 0, -5, 0 }, Neutral("n", PeriodMask.All));
            rules.State.stats[(int)StatType.Intelligence] = 5;
            var daysStarted = new List<int>();
            var burnouts = new List<StatType>();
            rules.OnDayStarted += daysStarted.Add;
            rules.OnBurnout += burnouts.Add;

            Pass(rules); Pass(rules); Pass(rules);

            CollectionAssert.AreEqual(new[] { StatType.Intelligence }, burnouts);
            Assert.AreEqual(GamePhase.GameOver, rules.State.phase);
            Assert.IsEmpty(daysStarted);
            Assert.AreEqual(1, rules.State.day);
            Assert.IsEmpty(rules.GetAvailableChoices());
        }

        [Test]
        public void Morning_IsLockedWhenRested_AndHasMoreOptionsWhenExhausted()
        {
            var college = Choice("college", PeriodMask.Morning, ChoiceAvailability.RestedOnly);
            var tired = Choice("tired", PeriodMask.Morning, ChoiceAvailability.ExhaustedOnly);
            var skip = Choice("skip", PeriodMask.Morning, ChoiceAvailability.ExhaustedOnly);
            var day = Neutral("day", PeriodMask.Afternoon);
            var allNighter = Choice("allNighter", PeriodMask.Night, ChoiceAvailability.Always, true);
            var sleep = Choice("sleep", PeriodMask.Night, ChoiceAvailability.Always);
            var rules = Rules(14, NoDecay, college, tired, skip, day, allNighter, sleep);

            CollectionAssert.AreEqual(new[] { college }, rules.GetAvailableChoices());

            // Day 1 -> exhausting night.
            rules.ApplyChoice(college);
            rules.ApplyChoice(day);
            rules.ApplyChoice(allNighter);

            Assert.IsTrue(rules.State.exhaustedToday);
            CollectionAssert.AreEquivalent(new[] { tired, skip }, rules.GetAvailableChoices());
            Assert.IsFalse(rules.ApplyChoice(college), "rested-only choice must be refused when exhausted");

            // Exhaustion lasts one day only.
            rules.ApplyChoice(tired);
            rules.ApplyChoice(day);
            rules.ApplyChoice(sleep);
            Assert.IsFalse(rules.State.exhaustedToday);
            CollectionAssert.AreEqual(new[] { college }, rules.GetAvailableChoices());
        }

        [Test]
        public void Multiplier_ScalesOnlyPositiveDeltas()
        {
            var mixed = Choice("mixed", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Intelligence, 10),
                new StatDelta(StatType.Health, -10));
            var rules = Rules(14, NoDecay, mixed);
            rules.ApplyChoice(mixed, 0f);
            Assert.AreEqual(60, rules.State.GetStat(StatType.Intelligence), "gain cancelled");
            Assert.AreEqual(50, rules.State.GetStat(StatType.Health), "cost still applied");

            rules.ApplyChoice(mixed, 2f);
            Assert.AreEqual(80, rules.State.GetStat(StatType.Intelligence));
            Assert.AreEqual(40, rules.State.GetStat(StatType.Health));
        }

        [Test]
        public void Restart_RestoresEverything()
        {
            var tire = Choice("tire", PeriodMask.All, ChoiceAvailability.Always, true,
                new StatDelta(StatType.Health, -10));
            var rules = Rules(14, new[] { -5, -5, -5 }, tire);
            var restarted = 0;
            rules.OnGameRestarted += () => restarted++;

            rules.ApplyChoice(tire); rules.ApplyChoice(tire); rules.ApplyChoice(tire);
            Assert.AreEqual(2, rules.State.day);
            Assert.IsTrue(rules.State.exhaustedToday);

            rules.Restart();

            Assert.AreEqual(1, restarted);
            Assert.AreEqual(1, rules.State.day);
            Assert.AreEqual(TimeOfDay.Morning, rules.State.period);
            Assert.IsFalse(rules.State.exhaustedToday);
            Assert.IsFalse(rules.State.exhaustedTomorrow);
            Assert.AreEqual(GamePhase.Playing, rules.State.phase);
            Assert.IsNull(rules.State.BurnoutStat);
            for (int i = 0; i < EnumExtensions.StatCount; i++)
                Assert.AreEqual(60, rules.State.stats[i]);
        }

        [Test]
        public void Restart_AfterBurnout_AllowsPlayingAgain()
        {
            var deadly = Choice("deadly", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Psychological, -100));
            var rules = Rules(14, NoDecay, deadly);
            rules.ApplyChoice(deadly);
            Assert.AreEqual(GamePhase.GameOver, rules.State.phase);

            rules.Restart();

            Assert.AreEqual(GamePhase.Playing, rules.State.phase);
            Assert.IsNotEmpty(rules.GetAvailableChoices());
        }

        [Test]
        public void FinalDay_RaisedOnlyAfterLastDay()
        {
            var rules = Rules(2, NoDecay, Neutral("n", PeriodMask.All));
            var fired = 0;
            rules.OnFinalDayReached += () => fired++;

            for (int i = 0; i < 3; i++) Pass(rules);
            Assert.AreEqual(0, fired, "day 1 is not the last");
            Assert.AreEqual(2, rules.State.day);

            for (int i = 0; i < 2; i++) Pass(rules);
            Assert.AreEqual(0, fired, "still on night of last day");
            Pass(rules);

            Assert.AreEqual(1, fired);
            Assert.AreEqual(GamePhase.FinalDayReached, rules.State.phase);
            Assert.IsEmpty(rules.GetAvailableChoices());
        }

        [Test]
        public void FinalDay_BurnoutFromLastDecayTakesPriority()
        {
            var rules = Rules(1, new[] { -100, 0, 0 }, Neutral("n", PeriodMask.All));
            var fired = 0;
            rules.OnFinalDayReached += () => fired++;

            Pass(rules); Pass(rules); Pass(rules);

            Assert.AreEqual(0, fired);
            Assert.AreEqual(GamePhase.GameOver, rules.State.phase);
        }

        [Test]
        public void StatChanged_ReportsOldAndNewValues()
        {
            var gain = Choice("gain", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Health, 70));
            var rules = Rules(14, NoDecay, gain);
            var calls = 0;
            StatType seenStat = StatType.Psychological;
            int seenOld = -1, seenNew = -1;
            rules.OnStatChanged += (s, o, n) => { calls++; seenStat = s; seenOld = o; seenNew = n; };

            rules.ApplyChoice(gain);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(StatType.Health, seenStat);
            Assert.AreEqual(60, seenOld);
            Assert.AreEqual(100, seenNew);
        }
    }
}
