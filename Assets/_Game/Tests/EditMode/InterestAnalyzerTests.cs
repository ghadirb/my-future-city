using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class InterestAnalyzerTests
    {
        private static TestFixtures.World World()
        {
            var w = TestFixtures.CreateWorld();
            // Every stage may be replayed (this is how the shipped data is configured).
            foreach (var m in w.Db.missions) { m.repeatable = true; m.repeatCoins = 1; }
            return w;
        }

        private static InsightReport Report(TestFixtures.World w)
        {
            return InterestAnalyzer.Analyze(w.Db, w.Save);
        }

        [Test]
        public void NewPlayerHasNoDataAndGetsAnInvitation()
        {
            var r = Report(World());
            Assert.AreEqual(DataConfidence.None, r.Confidence);
            Assert.IsFalse(r.HasData);
            Assert.IsNull(r.TopInterestJobId);
            Assert.AreEqual(InterestAnalyzer.TextNoData, r.LinesFa[0]);
        }

        [Test]
        public void NullInputsDoNotThrow()
        {
            Assert.IsNotNull(InterestAnalyzer.Analyze(null, null));
            Assert.IsNotNull(InterestAnalyzer.Analyze(TestFixtures.CreateDb(), null));
        }

        [Test]
        public void SingleJobPlayedGetsAllTheInterest()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 30);
            var r = Report(w);
            Assert.AreEqual("engineer", r.TopInterestJobId);
            Assert.AreEqual(1.0, r.Find("engineer").InterestShare, 0.001);
            Assert.AreEqual(0.0, r.Find("farmer").InterestShare, 0.001);
        }

        [Test]
        public void InterestSharesAlwaysAddUpToOne()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 40);
            AttemptTrackingTests.Play(w, "f1", 90);
            AttemptTrackingTests.Play(w, "f1", 80);
            double sum = 0;
            foreach (var j in Report(w).Jobs) sum += j.InterestShare;
            Assert.AreEqual(1.0, sum, 0.001);
        }

        [Test]
        public void ChoosingTheSameJobAgainAndAgainWinsOverATastingOfOthers()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 60);              // tried engineering once
            AttemptTrackingTests.Play(w, "f1", 50);              // and farming ...
            AttemptTrackingTests.Play(w, "f1", 45);              // ... which the child chose to repeat twice more
            AttemptTrackingTests.Play(w, "f1", 40);
            var r = Report(w);
            Assert.AreEqual("farmer", r.TopInterestJobId);
            Assert.AreEqual(2, r.Find("farmer").Replays);
            Assert.AreEqual("farmer", r.Jobs[0].JobId);
        }

        [Test]
        public void ReportCanBeBuiltFromAnOldSaveWithoutAttemptHistory()
        {
            var w = World();
            w.Save.GetMission("m1", true).timesCompleted = 1;
            w.Save.GetMission("f1", true).timesCompleted = 3;    // no attempts / jobStats: made before reports existed
            var r = Report(w);
            Assert.AreEqual(4, r.TotalAttempts);
            Assert.AreEqual("farmer", r.TopInterestJobId);
            Assert.AreEqual(2, r.Find("farmer").Replays);
        }

        [Test]
        public void GivingUpLowersTheCompletionRate()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 20);
            w.Missions.Start("m2");
            for (int i = 0; i < 30; i++) w.Missions.Tick(1f);
            w.Missions.AbandonActive();
            var j = Report(w).Find("engineer");
            Assert.AreEqual(1, j.Completed);
            Assert.AreEqual(1, j.Abandoned);
            Assert.AreEqual(50.0, j.CompletionPercent, 0.01);
        }

        [Test]
        public void MostSuccessfulJobNeedsAtLeastTwoAttemptsAndPrefersReliableFinishing()
        {
            var w = World();
            // engineer: 1 done, 1 given up   -> 50 %
            AttemptTrackingTests.Play(w, "m1", 20);
            w.Missions.Start("m2");
            for (int i = 0; i < 30; i++) w.Missions.Tick(1f);
            w.Missions.AbandonActive();
            // farmer: 2 done                 -> 100 %
            AttemptTrackingTests.Play(w, "f1", 20);
            AttemptTrackingTests.Play(w, "f1", 20);
            var r = Report(w);
            Assert.AreEqual("farmer", r.TopSuccessJobId);
        }

        [Test]
        public void SuccessIsNotNamedWhenNothingWasTriedTwice()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 20);
            AttemptTrackingTests.Play(w, "f1", 20);
            Assert.IsNull(Report(w).TopSuccessJobId);
        }

        [Test]
        public void GettingClearlyFasterWithRepeatsIsDetected()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 10);
            AttemptTrackingTests.Play(w, "m2", 100);
            AttemptTrackingTests.Play(w, "m2", 60);              // 40 % quicker
            var r = Report(w);
            Assert.AreEqual(1, r.Find("engineer").FasterStages);
            Assert.AreEqual(1, r.Find("engineer").ComparedStages);
            Assert.AreEqual("engineer", r.MostImprovedJobId);
        }

        [Test]
        public void SlightlyFasterOrSlowerRunsDoNotCountAsImprovement()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 10);
            AttemptTrackingTests.Play(w, "m2", 100);
            AttemptTrackingTests.Play(w, "m2", 95);
            Assert.IsNull(Report(w).MostImprovedJobId);
        }

        [Test]
        public void ConfidenceGrowsWithMoreRuns()
        {
            Assert.AreEqual(DataConfidence.None, InterestAnalyzer.ConfidenceFor(0));
            Assert.AreEqual(DataConfidence.Low, InterestAnalyzer.ConfidenceFor(3));
            Assert.AreEqual(DataConfidence.Medium, InterestAnalyzer.ConfidenceFor(4));
            Assert.AreEqual(DataConfidence.High, InterestAnalyzer.ConfidenceFor(10));
        }

        [Test]
        public void ExplainsWhenMostTimeAndMostSuccessPointToDifferentJobs()
        {
            var w = World();
            // engineer: long sessions, two stages finished and the third given up (lots of time, 67 % finished)
            AttemptTrackingTests.Play(w, "m1", 300);
            AttemptTrackingTests.Play(w, "m2", 100);
            w.Missions.Start("m3");
            for (int i = 0; i < 200; i++) w.Missions.Tick(1f);
            w.Missions.AbandonActive();
            // farmer: short but always finished
            AttemptTrackingTests.Play(w, "f1", 20);
            AttemptTrackingTests.Play(w, "f1", 20);
            var r = Report(w);
            Assert.AreEqual("engineer", r.TopInterestJobId);
            Assert.AreEqual("farmer", r.TopSuccessJobId);
            bool mentionsBoth = false;
            foreach (var line in r.LinesFa)
                if (line.Contains("ولی در") && line.Contains("موفق‌تر")) mentionsBoth = true;
            Assert.IsTrue(mentionsBoth, "expected a line contrasting interest and success");
        }

        [Test]
        public void WordingIsSoftAndNeverDiagnosesTalent()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 120);
            AttemptTrackingTests.Play(w, "m2", 120);
            AttemptTrackingTests.Play(w, "m2", 60);
            AttemptTrackingTests.Play(w, "f1", 30);
            var r = Report(w);
            bool soft = false;
            foreach (var line in r.LinesFa)
            {
                Assert.IsFalse(line.Contains("استعداد"));
                Assert.IsFalse(line.Contains("هوش"));
                Assert.IsFalse(line.Contains("شخصیت"));
                Assert.IsFalse(line.Contains("ضعیف"));
                if (line.Contains("به نظر می‌رسد")) soft = true;
            }
            Assert.IsTrue(soft);
        }

        [Test]
        public void UntriedJobsAreMentioned()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 30);
            bool found = false;
            foreach (var line in Report(w).LinesFa) if (line.Contains("هنوز امتحان نکرده")) found = true;
            Assert.IsTrue(found);
        }

        [Test]
        public void RecommenderFollowsTheJobTheChildKeepsChoosing()
        {
            var w = World();
            var rec = new Recommender(w.Db, w.Missions);
            AttemptTrackingTests.Play(w, "m1", 30);
            AttemptTrackingTests.Play(w, "m2", 30);
            AttemptTrackingTests.Play(w, "m2", 30);
            var s = rec.Suggest(null);
            Assert.AreEqual("m3", s.MissionId);                  // next new stage of the favourite job
            Assert.AreEqual(Recommender.ReasonFavorite, s.ReasonFa);
        }
    }
}
