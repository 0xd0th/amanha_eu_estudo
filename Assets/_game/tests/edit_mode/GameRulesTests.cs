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

        // Full B12 choice (debt, low score, gain factors, forced difficulty).
        private ChoiceData Rich(string id, PeriodMask periods, ChoiceAvailability availability, StatDelta[] deltas,
            bool exhausts = false, int debt = 0, float lowThreshold = 0f, StatDelta[] lowDeltas = null,
            bool lowExhausts = false, StatFactor[] factors = null, bool forceMax = false)
        {
            var c = ScriptableObject.CreateInstance<ChoiceData>();
            c.Setup(id, id, periods, availability, deltas, exhausts, debt, lowThreshold, lowDeltas, lowExhausts,
                factors, forceMax);
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

        // ---- Start / restart announce the same sequence ----

        private static List<string> Record(GameRules rules)
        {
            var log = new List<string>();
            rules.OnGameRestarted += () => log.Add("Restarted");
            rules.OnStatChanged += (s, o, n) => log.Add($"Stat:{s}:{o}->{n}");
            rules.OnStudyDebtChanged += (o, n) => log.Add($"Debt:{o}->{n}");
            rules.OnDayStarted += d => log.Add($"Day:{d}");
            rules.OnPeriodChanged += p => log.Add($"Period:{p}");
            return log;
        }

        private static readonly string[] InitialSequence =
        {
            "Stat:Health:60->60", "Stat:Intelligence:60->60", "Stat:Psychological:60->60",
            "Debt:0->0", "Day:1", "Period:Morning",
        };

        private static void AssertInitialState(GameRules rules)
        {
            Assert.AreEqual(1, rules.State.day);
            Assert.AreEqual(TimeOfDay.Morning, rules.State.period);
            Assert.AreEqual(GamePhase.Playing, rules.State.phase);
            Assert.IsFalse(rules.State.exhaustedToday);
            Assert.IsFalse(rules.State.exhaustedTomorrow);
            Assert.IsNull(rules.State.BurnoutStat);
            for (int i = 0; i < EnumExtensions.StatCount; i++)
                Assert.AreEqual(60, rules.State.stats[i]);
        }

        [Test]
        public void StartGame_AnnouncesInitialSequence_WithoutRestartedEvent()
        {
            var rules = Rules(14, NoDecay, Neutral("n", PeriodMask.All));
            var log = Record(rules);

            rules.StartGame();

            CollectionAssert.AreEqual(InitialSequence, log);
        }

        [Test]
        public void Restart_MidDay_AnnouncesSameSequenceAsStart_PrecededByRestarted()
        {
            var rules = Rules(14, NoDecay, Choice("hit", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Health, -10)));
            Pass(rules);
            Assert.AreEqual(TimeOfDay.Afternoon, rules.State.period);
            var log = Record(rules);

            rules.Restart();

            var expected = new List<string> { "Restarted" };
            expected.AddRange(InitialSequence);
            CollectionAssert.AreEqual(expected, log);
            AssertInitialState(rules);
        }

        [Test]
        public void Restart_AfterBurnout_RestoresInitialStateAndAnnouncesIt()
        {
            var deadly = Choice("deadly", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Psychological, -100));
            var rules = Rules(14, NoDecay, deadly);
            rules.ApplyChoice(deadly);
            Assert.AreEqual(GamePhase.GameOver, rules.State.phase);
            var log = Record(rules);

            rules.Restart();

            Assert.AreEqual("Restarted", log[0]);
            CollectionAssert.AreEqual(InitialSequence, log.GetRange(1, log.Count - 1));
            AssertInitialState(rules);
        }

        [Test]
        public void Restart_AfterFinalDayReached_RestoresInitialStateAndAnnouncesIt()
        {
            var rules = Rules(1, NoDecay, Neutral("n", PeriodMask.All));
            Pass(rules); Pass(rules); Pass(rules);
            Assert.AreEqual(GamePhase.FinalDayReached, rules.State.phase);
            var log = Record(rules);

            rules.Restart();

            Assert.AreEqual("Restarted", log[0]);
            CollectionAssert.AreEqual(InitialSequence, log.GetRange(1, log.Count - 1));
            AssertInitialState(rules);
        }

        [Test]
        public void Restart_ClearsExhaustionFlags()
        {
            var tire = Choice("tire", PeriodMask.All, ChoiceAvailability.Always, true);
            var rules = Rules(14, NoDecay, tire);
            Pass(rules); Pass(rules); Pass(rules);
            Assert.IsTrue(rules.State.exhaustedToday);
            rules.ApplyChoice(tire);
            Assert.IsTrue(rules.State.exhaustedTomorrow);

            rules.Restart();

            AssertInitialState(rules);
        }

        [Test]
        public void Restart_AllocatesNewStatsArray()
        {
            var rules = Rules(14, NoDecay, Neutral("n", PeriodMask.All));
            var before = rules.State.stats;

            rules.Restart();

            Assert.AreNotSame(before, rules.State.stats);
        }

        [Test]
        public void Restart_DoesNotMutateTheOldStatsArray()
        {
            var hit = Choice("hit", PeriodMask.All, ChoiceAvailability.Always, false,
                new StatDelta(StatType.Health, -10));
            var rules = Rules(14, NoDecay, hit);
            rules.ApplyChoice(hit);
            var old = rules.State.stats;
            Assert.AreEqual(50, old[(int)StatType.Health]);

            rules.Restart();

            Assert.AreEqual(50, old[(int)StatType.Health]);
            Assert.AreEqual(60, rules.State.GetStat(StatType.Health));
        }
    
        // ================= B12: study debt, low score, gain factors =================

        private static readonly StatDelta[] None = new StatDelta[0];

        private ChoiceData DebtChoice(int debt)
        {
            return Rich("debt" + debt, PeriodMask.All, ChoiceAvailability.Always, None, debt: debt);
        }

        // ---- Study debt ----

        [Test]
        public void Debt_StartsAtZero_AndAdds()
        {
            var plus3 = DebtChoice(3);
            var rules = Rules(14, NoDecay, plus3);
            Assert.AreEqual(0, rules.State.studyDebt);
            rules.ApplyChoice(plus3);
            Assert.AreEqual(3, rules.State.studyDebt);
            rules.ApplyChoice(plus3);
            Assert.AreEqual(6, rules.State.studyDebt);
        }

        [Test]
        public void Debt_NeverGoesBelowZero()
        {
            var minus5 = DebtChoice(-5);
            var plus2 = DebtChoice(2);
            var rules = Rules(14, NoDecay, minus5, plus2);
            rules.ApplyChoice(minus5);
            Assert.AreEqual(0, rules.State.studyDebt);
            rules.ApplyChoice(plus2);
            rules.ApplyChoice(minus5);
            Assert.AreEqual(0, rules.State.studyDebt);
        }

        [Test]
        public void Debt_HasNoMaximum()
        {
            var huge = DebtChoice(1000);
            var rules = Rules(14, NoDecay, huge);
            rules.ApplyChoice(huge);
            Assert.AreEqual(1000, rules.State.studyDebt);
        }

        [Test]
        public void Debt_ResetsOnRestart()
        {
            var plus3 = DebtChoice(3);
            var rules = Rules(14, NoDecay, plus3);
            rules.ApplyChoice(plus3);
            rules.Restart();
            Assert.AreEqual(0, rules.State.studyDebt);
        }

        [Test]
        public void Debt_EventFiresOnlyWhenTheValueChanges()
        {
            var plus2 = DebtChoice(2);
            var zero = DebtChoice(0);
            var minus9 = DebtChoice(-9);
            var rules = Rules(14, NoDecay, plus2, zero, minus9);
            var log = new List<string>();
            rules.OnStudyDebtChanged += (o, n) => log.Add($"{o}->{n}");

            rules.ApplyChoice(zero);   // 0 -> 0: silent
            rules.ApplyChoice(minus9); // clamped at 0: silent
            Assert.IsEmpty(log);

            rules.ApplyChoice(plus2);
            rules.ApplyChoice(minus9); // 2 -> 0
            CollectionAssert.AreEqual(new[] { "0->2", "2->0" }, log);
        }

        [Test]
        public void Debt_BeginNewGame_AnnouncesZeroZero_EvenAfterDebtWasAccumulated()
        {
            var plus3 = DebtChoice(3);
            var rules = Rules(14, NoDecay, plus3);
            rules.ApplyChoice(plus3);
            var log = new List<string>();
            rules.OnStudyDebtChanged += (o, n) => log.Add($"{o}->{n}");
            rules.Restart();
            CollectionAssert.AreEqual(new[] { "0->0" }, log);
        }

        [Test]
        public void Debt_InitialEvent_ComesAfterTheBarsAndBeforeTheFirstDay()
        {
            var rules = Rules(14, NoDecay, Neutral("n", PeriodMask.All));
            var log = Record(rules);
            rules.StartGame();
            int lastStat = log.FindLastIndex(e => e.StartsWith("Stat:"));
            int debt = log.IndexOf("Debt:0->0");
            int day = log.IndexOf("Day:1");
            Assert.Greater(debt, lastStat);
            Assert.Less(debt, day);
        }

        // ---- Burnout in the middle of a choice ----

        [Test]
        public void Burnout_InNormalDeltas_AppliesNeitherDebtNorExhaustion()
        {
            var bad = Rich("bad", PeriodMask.All, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Health, -999) }, exhausts: true, debt: 2,
                lowThreshold: 0.5f, lowExhausts: true);
            var rules = Rules(14, NoDecay, bad);
            rules.ApplyChoice(bad, 0f);
            Assert.AreEqual(GamePhase.GameOver, rules.State.phase);
            Assert.AreEqual(0, rules.State.studyDebt);
            Assert.IsFalse(rules.State.exhaustedTomorrow);
        }

        [Test]
        public void Burnout_InLowScoreDeltas_AppliesNeitherDebtNorExhaustion()
        {
            var bad = Rich("bad", PeriodMask.All, ChoiceAvailability.Always, None, debt: 2,
                lowThreshold: 0.5f, lowDeltas: new[] { new StatDelta(StatType.Intelligence, -999) },
                lowExhausts: true);
            var rules = Rules(14, NoDecay, bad);
            rules.ApplyChoice(bad, 0.3f);
            Assert.AreEqual(GamePhase.GameOver, rules.State.phase);
            Assert.AreEqual(0, rules.State.studyDebt);
            Assert.IsFalse(rules.State.exhaustedTomorrow);
        }

        // ---- Low score ----

        private ChoiceData Memes(float threshold = 0.5f, bool lowExhausts = true)
        {
            return Rich("memes", PeriodMask.All, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Psychological, 8), new StatDelta(StatType.Intelligence, -2) },
                debt: 1, lowThreshold: threshold,
                lowDeltas: new[] { new StatDelta(StatType.Psychological, 6), new StatDelta(StatType.Intelligence, -6) },
                lowExhausts: lowExhausts);
        }

        [Test]
        public void LowScore_BelowThreshold_AddsExtraDeltasWithoutMultiplier_AndMarksExhaustedTomorrow()
        {
            var memes = Memes();
            var rules = Rules(14, NoDecay, memes);
            rules.ApplyChoice(memes, 0.3f);
            // Psychological: 60 + round(8 x 0.3) = 62, + 6 (extra, unscaled) = 68.
            Assert.AreEqual(68, rules.State.GetStat(StatType.Psychological));
            // Intelligence: 60 - 2 (cost, unscaled) - 6 (extra) = 52.
            Assert.AreEqual(52, rules.State.GetStat(StatType.Intelligence));
            Assert.AreEqual(1, rules.State.studyDebt);
            Assert.IsTrue(rules.State.exhaustedTomorrow);
        }

        [Test]
        public void LowScore_AtExactThreshold_DoesNotApply()
        {
            var memes = Memes();
            var rules = Rules(14, NoDecay, memes);
            rules.ApplyChoice(memes, 0.5f);
            Assert.AreEqual(64, rules.State.GetStat(StatType.Psychological)); // 60 + round(8 x 0.5)
            Assert.AreEqual(58, rules.State.GetStat(StatType.Intelligence));   // 60 - 2
            Assert.IsFalse(rules.State.exhaustedTomorrow);
        }

        [Test]
        public void LowScore_AboveThreshold_DoesNotApply()
        {
            var memes = Memes();
            var rules = Rules(14, NoDecay, memes);
            rules.ApplyChoice(memes, 1f);
            Assert.AreEqual(68, rules.State.GetStat(StatType.Psychological)); // 60 + 8
            Assert.AreEqual(58, rules.State.GetStat(StatType.Intelligence));
            Assert.IsFalse(rules.State.exhaustedTomorrow);
        }

        [Test]
        public void LowScore_ThresholdZero_IsOff_EvenWithAZeroScore()
        {
            var memes = Memes(threshold: 0f);
            var rules = Rules(14, NoDecay, memes);
            rules.ApplyChoice(memes, 0f);
            Assert.AreEqual(60, rules.State.GetStat(StatType.Psychological)); // gain scaled to 0, no extra
            Assert.AreEqual(58, rules.State.GetStat(StatType.Intelligence));
            Assert.IsFalse(rules.State.exhaustedTomorrow);
        }

        [Test]
        public void LowScore_WithoutTheExhaustionFlag_DoesNotMarkExhaustedTomorrow()
        {
            var memes = Memes(lowExhausts: false);
            var rules = Rules(14, NoDecay, memes);
            rules.ApplyChoice(memes, 0.3f);
            Assert.AreEqual(68, rules.State.GetStat(StatType.Psychological));
            Assert.IsFalse(rules.State.exhaustedTomorrow);
        }

        [Test]
        public void LowScoreExhaustion_AppliesOnlyTomorrow_AndLastsOneDay()
        {
            var neutral = Neutral("n", PeriodMask.All);
            var memes = Rich("memes", PeriodMask.Night, ChoiceAvailability.Always, None,
                lowThreshold: 0.5f, lowExhausts: true);
            var rules = Rules(14, NoDecay, neutral, memes);

            rules.ApplyChoice(neutral); // morning
            rules.ApplyChoice(neutral); // afternoon
            Assert.IsFalse(rules.State.exhaustedToday);
            rules.ApplyChoice(memes, 0.3f); // night, low score

            Assert.AreEqual(2, rules.State.day);
            Assert.IsTrue(rules.State.exhaustedToday);
            Assert.IsFalse(rules.State.exhaustedTomorrow);

            rules.ApplyChoice(neutral); // morning
            rules.ApplyChoice(neutral); // afternoon
            rules.ApplyChoice(neutral); // night: closes day 2 (neutral is available at night too)
            Assert.AreEqual(3, rules.State.day);
            Assert.IsFalse(rules.State.exhaustedToday);
        }

        // ---- Gain factors ----

        [Test]
        public void GainFactors_ScaleOnlyGains()
        {
            var tired = Rich("tired", PeriodMask.All, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Intelligence, 4), new StatDelta(StatType.Health, -10) },
                factors: new[] { new StatFactor(StatType.Intelligence, 0.5f), new StatFactor(StatType.Health, 0.5f) });
            var rules = Rules(14, NoDecay, tired);
            rules.ApplyChoice(tired);
            Assert.AreEqual(62, rules.State.GetStat(StatType.Intelligence)); // 60 + 4 x 0.5
            Assert.AreEqual(50, rules.State.GetStat(StatType.Health));       // cost untouched by the factor
        }

        [Test]
        public void GainFactors_AbsentStatMeansFactorOne()
        {
            var c = Rich("c", PeriodMask.All, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Health, 10), new StatDelta(StatType.Intelligence, 10) },
                factors: new[] { new StatFactor(StatType.Intelligence, 0.5f) });
            var rules = Rules(14, NoDecay, c);
            rules.ApplyChoice(c);
            Assert.AreEqual(70, rules.State.GetStat(StatType.Health));
            Assert.AreEqual(65, rules.State.GetStat(StatType.Intelligence));
        }

        [Test]
        public void GainFactors_RoundOnce_BasePerformanceAndFactorTogether()
        {
            // 5 x 0.5 x 1.2 = 3.0 -> 3 with a single rounding.
            // Rounding in two steps would give round(2.5) = 2, then 2 x 1.2 = 2.4 -> 2.
            var c = Rich("c", PeriodMask.All, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Intelligence, 5) },
                factors: new[] { new StatFactor(StatType.Intelligence, 1.2f) });
            var rules = Rules(14, NoDecay, c);
            rules.ApplyChoice(c, 0.5f);
            Assert.AreEqual(63, rules.State.GetStat(StatType.Intelligence));
        }

        [Test]
        public void GainFactors_NoFactor_KeepsTheOldBehavior()
        {
            var c = Rich("c", PeriodMask.All, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Health, 7) });
            var rules = Rules(14, NoDecay, c);
            rules.ApplyChoice(c, 0.6f);
            Assert.AreEqual(60 + Mathf.RoundToInt(7 * 0.6f), rules.State.GetStat(StatType.Health));
        }

        // ---- Forced difficulty ----

        [Test]
        public void IsMaxDifficultyForced_FollowsTheFlag()
        {
            var forced = Rich("f", PeriodMask.All, ChoiceAvailability.Always, None, forceMax: true);
            var normal = Rich("n", PeriodMask.All, ChoiceAvailability.Always, None);
            var rules = Rules(14, NoDecay, forced, normal);
            Assert.IsTrue(rules.IsMaxDifficultyForced(forced));
            Assert.IsFalse(rules.IsMaxDifficultyForced(normal));
            Assert.IsFalse(rules.IsMaxDifficultyForced(null));
        }

        // ---- Order of application ----

        [Test]
        public void ApplyChoice_AppliesDeltasThenLowScoreThenDebt_InThatOrder()
        {
            var c = Rich("c", PeriodMask.All, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Health, -1) }, debt: 1,
                lowThreshold: 0.5f, lowDeltas: new[] { new StatDelta(StatType.Health, -2) });
            var rules = Rules(14, NoDecay, c);
            var log = new List<string>();
            rules.OnStatChanged += (s, o, n) => log.Add($"Stat:{o}->{n}");
            rules.OnStudyDebtChanged += (o, n) => log.Add($"Debt:{o}->{n}");
            rules.OnPeriodChanged += p => log.Add($"Period:{p}");
            rules.ApplyChoice(c, 0.1f);
            CollectionAssert.AreEqual(new[] { "Stat:60->59", "Stat:59->57", "Debt:0->1", "Period:Afternoon" }, log);
        }

        // ---- Full flow ----

        [Test]
        public void Flow_MemesWithLowScore_ThenSkipAndSleep_AddsDebtAndHealth()
        {
            var neutral = Rich("n", PeriodMask.Morning | PeriodMask.Afternoon, ChoiceAvailability.Always, None);
            var memes = Rich("memes", PeriodMask.Night, ChoiceAvailability.Always,
                new[] { new StatDelta(StatType.Psychological, 8), new StatDelta(StatType.Intelligence, -2) },
                debt: 1, lowThreshold: 0.5f,
                lowDeltas: new[] { new StatDelta(StatType.Psychological, 6), new StatDelta(StatType.Intelligence, -6) },
                lowExhausts: true);
            var college = Rich("college", PeriodMask.Morning, ChoiceAvailability.RestedOnly, None);
            var goTired = Rich("go_tired", PeriodMask.Morning, ChoiceAvailability.ExhaustedOnly,
                new[] { new StatDelta(StatType.Intelligence, 4) },
                factors: new[] { new StatFactor(StatType.Intelligence, 0.5f) }, forceMax: true);
            var skip = Rich("skip_and_sleep", PeriodMask.Morning, ChoiceAvailability.ExhaustedOnly,
                new[] { new StatDelta(StatType.Health, 15) }, debt: 1);
            var rules = Rules(14, NoDecay, neutral, memes, college, goTired, skip);

            rules.ApplyChoice(neutral);
            rules.ApplyChoice(neutral);
            rules.ApplyChoice(memes, 0.3f);

            Assert.AreEqual(1, rules.State.studyDebt);
            Assert.AreEqual(2, rules.State.day);
            Assert.IsTrue(rules.State.exhaustedToday);

            var available = rules.GetAvailableChoices();
            CollectionAssert.Contains(available, goTired);
            CollectionAssert.Contains(available, skip);
            CollectionAssert.DoesNotContain(available, college);
            Assert.IsTrue(rules.IsMaxDifficultyForced(goTired));

            int healthBefore = rules.State.GetStat(StatType.Health);
            rules.ApplyChoice(skip);
            Assert.AreEqual(healthBefore + 15, rules.State.GetStat(StatType.Health));
            Assert.AreEqual(2, rules.State.studyDebt);
        }
    }
}
