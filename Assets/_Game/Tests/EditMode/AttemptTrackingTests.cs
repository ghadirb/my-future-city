using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class AttemptTrackingTests
    {
        /// <summary>Starts a mission, lets <paramref name="seconds"/> of play pass and finishes every objective.</summary>
        public static void Play(TestFixtures.World w, string id, int seconds)
        {
            Assert.IsTrue(w.Missions.Start(id), "could not start " + id);
            for (int i = 0; i < seconds; i++) w.Missions.Tick(1f);
            var def = w.Db.FindMission(id);
            foreach (var o in def.objectives)
                w.Missions.Notify(o.ParsedType(), o.targetId, o.RequiredCount);
        }

        [Test]
        public void CompletedRunIsRecordedWithItsPlayTime()
        {
            var w = TestFixtures.CreateWorld();
            Play(w, "m1", 42);
            Assert.AreEqual(1, w.Save.attempts.Count);
            var a = w.Save.attempts[0];
            Assert.AreEqual("m1", a.missionId);
            Assert.AreEqual("engineer", a.jobId);
            Assert.AreEqual(42, a.seconds);
            Assert.IsTrue(a.completed);
            Assert.IsFalse(a.replay);
            Assert.AreEqual(42.0, w.Save.GetJobStats("engineer", false).playSeconds, 0.01);
        }

        [Test]
        public void RepeatingAStageIsMarkedAsReplay()
        {
            var w = TestFixtures.CreateWorld();
            Play(w, "m1", 10);
            Play(w, "m2", 20);
            Play(w, "m2", 15);
            Assert.AreEqual(3, w.Save.attempts.Count);
            Assert.IsFalse(w.Save.attempts[1].replay);
            Assert.IsTrue(w.Save.attempts[2].replay);
            Assert.AreEqual(1, w.Save.GetJobStats("engineer", false).replays);
        }

        [Test]
        public void GivingUpAfterRealEffortIsRecordedWithoutPenalty()
        {
            var w = TestFixtures.CreateWorld();
            w.Missions.Start("m1");
            for (int i = 0; i < 25; i++) w.Missions.Tick(1f);
            int coinsBefore = w.Save.coins;
            Assert.IsTrue(w.Missions.AbandonActive());
            Assert.AreEqual(1, w.Save.attempts.Count);
            Assert.IsFalse(w.Save.attempts[0].completed);
            Assert.AreEqual(1, w.Save.GetJobStats("engineer", false).abandoned);
            Assert.AreEqual(coinsBefore, w.Save.coins);
            Assert.IsNull(w.Missions.ActiveMission);
        }

        [Test]
        public void AccidentalStartAndImmediateQuitIsNotRecorded()
        {
            var w = TestFixtures.CreateWorld();
            w.Missions.Start("m1");
            w.Missions.Tick(2f);
            w.Missions.AbandonActive();
            Assert.AreEqual(0, w.Save.attempts.Count);
        }

        [Test]
        public void SwitchToAnotherJobAbandonsTheCurrentRunAndStartsTheNew()
        {
            var w = TestFixtures.CreateWorld();
            w.Missions.Start("m1");
            for (int i = 0; i < 30; i++) w.Missions.Tick(1f);
            Assert.IsTrue(w.Missions.SwitchTo("f1"));
            Assert.AreEqual("f1", w.Missions.ActiveMission.id);
            Assert.AreEqual(1, w.Save.attempts.Count);
            Assert.AreEqual("m1", w.Save.attempts[0].missionId);
        }

        [Test]
        public void SwitchToAnUnavailableMissionKeepsTheCurrentRun()
        {
            var w = TestFixtures.CreateWorld();
            w.Missions.Start("m1");
            Assert.IsFalse(w.Missions.SwitchTo("m3"));          // locked
            Assert.AreEqual("m1", w.Missions.ActiveMission.id);
            Assert.AreEqual(0, w.Save.attempts.Count);
        }

        [Test]
        public void TickOnlyCountsWhileARunIsActiveAndIgnoresHitches()
        {
            var w = TestFixtures.CreateWorld();
            w.Missions.Tick(5f);                                 // nothing active
            w.Missions.Start("m1");
            w.Missions.Tick(100f);                               // app-resume spike is clamped to 1 s
            Assert.AreEqual(1.0, w.Save.GetMission("m1", false).activeSeconds, 0.001);
            w.Missions.Tick(-3f);
            Assert.AreEqual(1.0, w.Save.GetMission("m1", false).activeSeconds, 0.001);
        }

        [Test]
        public void RestartingAStageResetsItsRunTimer()
        {
            var w = TestFixtures.CreateWorld();
            Play(w, "m1", 30);
            w.Db.FindMission("m1").repeatable = true;
            w.Missions.Start("m1");
            Assert.AreEqual(0.0, w.Save.GetMission("m1", false).activeSeconds, 0.001);
        }

        [Test]
        public void OfferableListsNewStagesBeforeReplays()
        {
            var w = TestFixtures.CreateWorld();
            Play(w, "m1", 5);                                    // m1 done (not repeatable), m2 now new
            var list = w.Missions.OfferableForNpc("npc");
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("m2", list[0].id);
            Play(w, "m2", 5);                                    // m2 repeatable, m3 new
            list = w.Missions.OfferableForNpc("npc");
            Assert.AreEqual("m3", list[0].id);
            Assert.AreEqual("m2", list[1].id);
        }

        [Test]
        public void AttemptHistoryIsCappedButJobTotalsStayExact()
        {
            var w = TestFixtures.CreateWorld();
            Play(w, "m1", 1);
            for (int i = 0; i < SaveData.MaxAttempts + 25; i++) Play(w, "m2", 1);
            Assert.AreEqual(SaveData.MaxAttempts, w.Save.attempts.Count);
            Assert.AreEqual(SaveData.MaxAttempts + 25 + 1.0, w.Save.GetJobStats("engineer", false).playSeconds, 0.01);
            Assert.AreEqual(SaveData.MaxAttempts + 25 - 1, w.Save.GetJobStats("engineer", false).replays);
        }

        [Test]
        public void OldVersionOneSaveLoadsWithEmptyReportData()
        {
            var s = new SaveData { version = 1, characterId = "boy", coins = 12 };
            s.attempts = null;
            s.jobStats = null;
            s.GetMission("m1", true).timesCompleted = 2;
            s.Sanitize();
            Assert.AreEqual(SaveData.CurrentVersion, s.version);
            Assert.IsNotNull(s.attempts);
            Assert.IsNotNull(s.jobStats);
            Assert.AreEqual(12, s.coins);
            Assert.AreEqual(2, s.GetMission("m1", false).timesCompleted);
        }

        [Test]
        public void SanitizeDropsBrokenAttemptRecords()
        {
            var s = new SaveData();
            s.attempts.Add(null);
            s.attempts.Add(new AttemptRecord { missionId = "", jobId = "x" });
            s.attempts.Add(new AttemptRecord { seq = 7, missionId = "m1", jobId = "engineer", seconds = -5 });
            s.jobStats.Add(new JobStats { jobId = "engineer", playSeconds = -1f, abandoned = -2 });
            s.jobStats.Add(new JobStats { jobId = "engineer" });
            s.Sanitize();
            Assert.AreEqual(1, s.attempts.Count);
            Assert.AreEqual(0, s.attempts[0].seconds);
            Assert.AreEqual(1, s.jobStats.Count);
            Assert.AreEqual(0, s.jobStats[0].abandoned);
            Assert.AreEqual(7, s.attemptSeq);
        }
    }
}
