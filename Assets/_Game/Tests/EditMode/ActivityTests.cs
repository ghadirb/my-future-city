using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    /// <summary>
    /// Acceptance tests of the master addendum (section 10), written against a small in-code teacher job that has the
    /// same structure as the shipped data: collecting is only preparation, the real work is a set of decisions.
    /// </summary>
    [TestFixture]
    public class ActivityTests
    {
        private static ActivityOption O(string t, bool ok) { return new ActivityOption { textFa = t, correct = ok, feedbackFa = "بازخورد " + t }; }

        private static ActivityStep Step(bool free, params ActivityOption[] opts)
        {
            return new ActivityStep { promptFa = "سؤال", hintFa = "راهنما", free = free, options = new List<ActivityOption>(opts) };
        }

        private class TeacherWorld
        {
            public MissionDatabase Db;
            public SaveData Save;
            public Wallet Wallet;
            public CityProgressService City;
            public MissionService Missions;
            public ActivityDatabase Acts;
            public ActivityService Activities;
        }

        private static TeacherWorld Create()
        {
            var db = new MissionDatabase();
            db.jobs.Add(new JobDefinition { id = "teacher", nameFa = "معلم", badgeId = "badge_t" });
            db.jobs.Add(new JobDefinition { id = "farmer", nameFa = "کشاورز" });
            db.missions.Add(new MissionDefinition
            {
                id = "t1", jobId = "teacher", nameFa = "آماده کردن کلاس", startNpcId = "npc", repeatable = true, repeatCoins = 3,
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "collect", targetId = "book", count = 2, textFa = "کتاب" },
                    new ObjectiveDefinition { type = "interact", targetId = "prep", count = 1, textFa = "آماده‌سازی" },
                    new ObjectiveDefinition { type = "talk", targetId = "npc", count = 1, textFa = "گفتگو" }
                },
                reward = new RewardDefinition { coins = 10, xp = 20, badgeId = "badge_t" }
            });
            db.missions.Add(new MissionDefinition
            {
                id = "t2", jobId = "teacher", nameFa = "تدریس", startNpcId = "npc", requiresMissionId = "t1", repeatable = true, repeatCoins = 3,
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "interact", targetId = "teach", count = 2, textFa = "تدریس" },
                    new ObjectiveDefinition { type = "talk", targetId = "npc", count = 1, textFa = "گفتگو" }
                },
                reward = new RewardDefinition { coins = 20, xp = 30 }
            });
            // A job that has NOT been redesigned yet: it can still be finished by collecting only.
            db.missions.Add(new MissionDefinition
            {
                id = "f1", jobId = "farmer", nameFa = "بذر", startNpcId = "farmer",
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "collect", targetId = "seed", count = 2, textFa = "بذر" },
                    new ObjectiveDefinition { type = "talk", targetId = "farmer", count = 1, textFa = "گفتگو" }
                },
                reward = new RewardDefinition { coins = 5, xp = 5 }
            });

            var acts = new ActivityDatabase();
            acts.activities.Add(new ActivityDefinition
            {
                missionId = "t1", targetId = "prep", jobId = "teacher",
                realWorldFa = "معلم درس را آماده می‌کند", childDoesFa = "کودک موضوع را انتخاب می‌کند", skillFa = "برنامه‌ریزی",
                scenes = new List<ActivityScene>
                {
                    new ActivityScene { titleFa = "آماده‌سازی", steps = new List<ActivityStep>
                    {
                        Step(true, O("جمع و تفریق", true), O("علوم", true), O("طبیعت", true)),
                        Step(false, O("غلط", false), O("درست", true), O("غلط۲", false))
                    } }
                }
            });
            acts.activities.Add(new ActivityDefinition
            {
                missionId = "t2", targetId = "teach", jobId = "teacher",
                realWorldFa = "معلم درس می‌دهد", childDoesFa = "کودک توضیح را انتخاب می‌کند", skillFa = "توضیح دادن",
                scenes = new List<ActivityScene>
                {
                    new ActivityScene { titleFa = "درس ۱", steps = new List<ActivityStep> { Step(false, O("درست", true), O("غلط", false)) } },
                    new ActivityScene { titleFa = "درس ۲", steps = new List<ActivityStep> { Step(false, O("غلط", false), O("درست", true)) } }
                }
            });

            var w = new TeacherWorld { Db = db, Acts = acts, Save = new SaveData() };
            w.Wallet = new Wallet(w.Save);
            w.City = new CityProgressService(db, w.Save);
            w.Missions = new MissionService(db, w.Save, w.Wallet, w.City);
            w.Activities = new ActivityService(acts, w.Save, w.Missions);
            return w;
        }

        private static void CollectBooks(TeacherWorld w) { w.Missions.Notify(ObjectiveType.Collect, "book", 2); }

        /// <summary>Plays every step of a session correctly (always the first accepted option).</summary>
        private static void Solve(ActivitySession s)
        {
            while (s.CurrentStep != null)
            {
                int pick = 0;
                for (int i = 0; i < s.CurrentStep.options.Count; i++)
                    if (s.CurrentStep.IsAccepted(i)) { pick = i; break; }
                Assert.IsTrue(s.Choose(pick).Accepted);
            }
        }

        private static int Wrong(ActivitySession s)
        {
            for (int i = 0; i < s.CurrentStep.options.Count; i++)
                if (!s.CurrentStep.IsAccepted(i)) return i;
            return -1;
        }

        // ---------- 1. collecting alone can never finish the profession ----------

        [Test]
        public void CollectingAndTalkingAloneNeverCompletesATeacherMission()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            Assert.IsFalse(w.Missions.Notify(ObjectiveType.Talk, "npc"), "talking before the activity must not count");
            Assert.AreEqual(MissionStatus.Active, w.Missions.GetStatus("t1"));
            Assert.AreEqual(0, w.Missions.TimesCompleted("t1"));
        }

        private static TeacherWorld CreateStartedT1()
        {
            var w = Create();
            Assert.IsTrue(w.Missions.Start("t1"));
            return w;
        }

        [Test]
        public void ActivityCannotStartBeforeTheCollectingPreparationIsDone()
        {
            var w = CreateStartedT1();
            Assert.IsNull(w.Activities.Begin("t1", "prep", 0), "the objective order must be respected");
            CollectBooks(w);
            Assert.IsNotNull(w.Activities.Begin("t1", "prep", 0));
        }

        // ---------- 2-5. a real teaching activity is performed with decisions ----------

        [Test]
        public void FinishingEveryDecisionAdvancesTheMissionAndItCompletesAfterTalking()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            var s = w.Activities.Begin("t1", "prep", 0);
            Solve(s);
            Assert.IsTrue(s.Finished);
            Assert.AreEqual(1, w.Missions.GetCountByTarget("t1", "prep"));
            Assert.AreEqual(MissionStatus.Active, w.Missions.GetStatus("t1"));
            w.Missions.Notify(ObjectiveType.Talk, "npc");
            Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus("t1"));
            Assert.AreEqual(10, w.Save.coins);
        }

        [Test]
        public void WrongAnswerNeverAdvancesAndAllowsAnotherTry()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            var s = w.Activities.Begin("t1", "prep", 0);
            Assert.IsTrue(s.Choose(0).Accepted);                       // free preference step
            int wrong = Wrong(s);
            var r = s.Choose(wrong);
            Assert.IsFalse(r.Accepted);
            Assert.IsFalse(r.Correct);
            Assert.IsFalse(string.IsNullOrEmpty(r.FeedbackFa));         // friendly educational feedback
            Assert.AreEqual(1, s.StepIndex);                            // still on the same step
            Assert.AreEqual(0, w.Missions.GetCountByTarget("t1", "prep"));
            Solve(s);                                                   // the child may pick another way
            Assert.IsTrue(s.Finished);
            Assert.AreEqual(1, w.Missions.GetCountByTarget("t1", "prep"));
        }

        [Test]
        public void FreeStepAcceptsEveryOptionAndIsLoggedAsAChoice()
        {
            for (int option = 0; option < 3; option++)
            {
                var w = CreateStartedT1();
                CollectBooks(w);
                var s = w.Activities.Begin("t1", "prep", 0);
                Assert.IsTrue(s.Choose(option).Accepted);
                Assert.AreEqual(1, w.Save.activityChoices.Count);
                Assert.AreEqual(option, w.Save.activityChoices[0].option);
                Assert.AreEqual("t1", w.Save.activityChoices[0].missionId);
            }
        }

        [Test]
        public void TheChildCanChooseADifferentExplanationAfterAWrongOne()
        {
            var w = Create();
            CompleteT1(w);
            w.Missions.Start("t2");
            int firstTryBefore = w.Save.GetActivityStats("teacher", false).firstTry;
            var s = w.Activities.Begin("t2", "teach", 0);
            Assert.IsFalse(s.Choose(1).Accepted);       // explanation that does not help
            Assert.IsTrue(s.Choose(0).Accepted);        // another way works
            var st = w.Save.GetActivityStats("teacher", false);
            Assert.AreEqual(1, st.wrongChoices);
            Assert.AreEqual(firstTryBefore, st.firstTry);   // the corrected step did not count as a first try
        }

        private static void CompleteT1(TeacherWorld w)
        {
            w.Missions.Start("t1");
            CollectBooks(w);
            Solve(w.Activities.Begin("t1", "prep", 0));
            w.Missions.Notify(ObjectiveType.Talk, "npc");
            Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus("t1"));
        }

        [Test]
        public void EachSpotPlaysItsOwnSceneAndMustBeDoneInOrder()
        {
            var w = Create();
            CompleteT1(w);
            w.Missions.Start("t2");
            Assert.IsNull(w.Activities.Begin("t2", "teach", 1), "spot 1 is locked until spot 0 is done");
            var a = w.Activities.Begin("t2", "teach", 0);
            Assert.AreEqual("درس ۱", a.Scene.titleFa);
            Solve(a);
            var b = w.Activities.Begin("t2", "teach", 1);
            Assert.AreEqual("درس ۲", b.Scene.titleFa);
            Solve(b);
            Assert.AreEqual(2, w.Missions.GetCountByTarget("t2", "teach"));
        }

        // ---------- 6. progress is saved ----------

        [Test]
        public void ProgressSurvivesSaveRoundTripFields()
        {
            var w = Create();
            CompleteT1(w);
            var rec = w.Save.GetActivityRecord("t1", "prep", 0, false);
            Assert.IsNotNull(rec);
            Assert.AreEqual(1, rec.completions);
            Assert.AreEqual("teacher", rec.jobId);
            w.Save.Sanitize();
            Assert.IsNotNull(w.Save.GetActivityRecord("t1", "prep", 0, false));
            Assert.AreEqual(1, w.Save.GetActivityStats("teacher", false).completed);
            Assert.AreEqual(SaveData.CurrentVersion, w.Save.version);
        }

        [Test]
        public void OldSavesWithoutActivityDataLoadAndWork()
        {
            var old = new SaveData { version = 2 };
            old.activityStats = null; old.activityRecords = null; old.activityChoices = null;
            old.Sanitize();
            Assert.IsNotNull(old.activityStats);
            Assert.AreEqual(SaveData.CurrentVersion, old.version);
        }

        // ---------- 7. voluntary repeat is recorded ----------

        [Test]
        public void VoluntaryRepeatIsRecordedAsOptional()
        {
            var w = Create();
            CompleteT1(w);
            Assert.AreEqual(0, w.Save.GetActivityStats("teacher", false).optionalRuns);
            Assert.IsTrue(w.Missions.Start("t1"));      // the child chooses to play it again
            CollectBooks(w);
            Solve(w.Activities.Begin("t1", "prep", 0));
            Assert.AreEqual(1, w.Save.GetActivityStats("teacher", false).optionalRuns);
            Assert.AreEqual(2, w.Save.GetActivityRecord("t1", "prep", 0, false).completions);
        }

        // ---------- no double rewards / duplicate events ----------

        [Test]
        public void ASpotCannotBeRewardedTwiceInOneRun()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            Solve(w.Activities.Begin("t1", "prep", 0));
            Assert.IsNull(w.Activities.Begin("t1", "prep", 0), "the same spot is finished for this run");
            Assert.AreEqual(1, w.Save.GetActivityStats("teacher", false).completed);
        }

        [Test]
        public void ChoosingAfterTheSceneEndedDoesNothing()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            var s = w.Activities.Begin("t1", "prep", 0);
            Solve(s);
            Assert.IsFalse(s.Choose(0).Accepted);
            Assert.AreEqual(1, w.Save.GetActivityStats("teacher", false).completed);
        }

        [Test]
        public void AbandonedSceneIsCountedAndPenaltyFree()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            int coins = w.Save.coins;
            var s = w.Activities.Begin("t1", "prep", 0);
            s.Choose(0);
            s.Abandon();
            Assert.AreEqual(1, w.Save.GetActivityStats("teacher", false).abandoned);
            Assert.AreEqual(coins, w.Save.coins);
            Assert.IsNotNull(w.Activities.Begin("t1", "prep", 0), "the child may start again later");
        }

        [Test]
        public void HintsAreCountedAndTheHintedStepIsNotAFirstTry()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            var s = w.Activities.Begin("t1", "prep", 0);
            s.Choose(0);
            Assert.AreEqual("راهنما", s.UseHint());
            Solve(s);
            var st = w.Save.GetActivityStats("teacher", false);
            Assert.AreEqual(1, st.hints);
            Assert.AreEqual(2, st.stepsDone);
            Assert.AreEqual(1, st.firstTry);      // only the free step counted as first try
        }

        // ---------- interest log is separate from rewards ----------

        [Test]
        public void ActivityLogNeverChangesCoinsOrXp()
        {
            var w = CreateStartedT1();
            CollectBooks(w);
            int coins = w.Save.coins, xp = w.Save.xp;
            var s = w.Activities.Begin("t1", "prep", 0);
            s.Choose(0); s.Choose(Wrong(s)); s.UseHint(); Solve(s);
            Assert.AreEqual(coins, w.Save.coins);
            Assert.AreEqual(xp, w.Save.xp);
        }

        // ---------- report built from real events ----------

        [Test]
        public void ReportCountsRealActivitiesAndNeverClaimsTalent()
        {
            var w = Create();
            CompleteT1(w);
            w.Missions.Start("t2");
            Solve(w.Activities.Begin("t2", "teach", 0));
            Solve(w.Activities.Begin("t2", "teach", 1));
            w.Missions.Notify(ObjectiveType.Talk, "npc");

            var report = InterestAnalyzer.Analyze(w.Db, w.Save);
            var t = report.Find("teacher");
            Assert.AreEqual(3, t.ActivitiesDone);
            Assert.AreEqual(4, t.ActivityStepsDone);
            Assert.AreEqual("teacher", report.MostActivitiesJobId);
            var text = string.Join(" ", report.LinesFa.ToArray());
            StringAssert.Contains("فعالیت واقعی", text);
            StringAssert.DoesNotContain("استعداد", text);
            Assert.AreEqual(0, report.Find("farmer").ActivitiesDone);
        }

        [Test]
        public void ReportKeepsParticipationSuccessAndRepeatsSeparate()
        {
            var w = Create();
            CompleteT1(w);
            w.Missions.Start("t1");
            CollectBooks(w);
            var s = w.Activities.Begin("t1", "prep", 0);
            s.Choose(0);
            s.Choose(Wrong(s));                 // a mistake: success drops, participation does not
            Solve(s);
            var t = InterestAnalyzer.Analyze(w.Db, w.Save).Find("teacher");
            Assert.AreEqual(2, t.ActivitiesDone);               // participation
            Assert.AreEqual(1, t.OptionalActivityRuns);         // voluntary repeat
            Assert.Less(t.ActivitySuccessRate, 1f);             // success is its own number
            Assert.AreEqual(1, t.WrongChoices);
        }

        [Test]
        public void NoActivitiesMeansNoActivitySentence()
        {
            var w = Create();
            var report = InterestAnalyzer.Analyze(w.Db, w.Save);
            Assert.IsFalse(report.HasData);
            StringAssert.DoesNotContain("فعالیت واقعی", string.Join(" ", report.LinesFa.ToArray()));
        }

        // ---------- validator ----------

        [Test]
        public void ValidatorAcceptsTheWellFormedActivities()
        {
            var w = Create();
            var errors = ActivityDatabaseValidator.Validate(w.Acts, w.Db);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors.ToArray()));
        }

        [Test]
        public void ValidatorRejectsStepsWithoutADecision()
        {
            var w = Create();
            w.Acts.activities[0].scenes[0].steps[1].options = new List<ActivityOption> { O("فقط یکی", true) };
            Assert.Greater(ActivityDatabaseValidator.Validate(w.Acts, w.Db).Count, 0);
        }

        [Test]
        public void ValidatorRejectsStepWhereEverythingIsCorrectUnlessFree()
        {
            var w = Create();
            w.Acts.activities[1].scenes[0].steps[0] = Step(false, O("الف", true), O("ب", true));
            var errors = ActivityDatabaseValidator.Validate(w.Acts, w.Db);
            Assert.IsTrue(errors.Exists(e => e.Contains("every option is correct")));
        }

        [Test]
        public void ValidatorRejectsActivityWithoutMatchingObjectiveOrEnoughScenes()
        {
            var w = Create();
            w.Acts.activities[1].scenes.RemoveAt(1);           // objective needs 2 spots
            Assert.IsTrue(ActivityDatabaseValidator.Validate(w.Acts, w.Db).Exists(e => e.Contains("scene(s)")));
            w.Acts.activities[0].targetId = "unknown";
            Assert.IsTrue(ActivityDatabaseValidator.Validate(w.Acts, w.Db).Exists(e => e.Contains("no interact objective")));
        }

        [Test]
        public void ValidatorRequiresTheRealWorldDesignFields()
        {
            var w = Create();
            w.Acts.activities[0].realWorldFa = "";
            w.Acts.activities[0].childDoesFa = "";
            w.Acts.activities[0].skillFa = "";
            Assert.AreEqual(3, ActivityDatabaseValidator.Validate(w.Acts, w.Db).FindAll(e => e.Contains("missing")).Count);
        }

        [Test]
        public void ValidatorFlagsJobsThatCanStillBeFinishedByCollectingOnly()
        {
            var w = Create();
            var jobs = ActivityDatabaseValidator.JobsWithMissionsWithoutRealActivity(w.Db, w.Acts);
            Assert.IsFalse(jobs.Contains("teacher"));
            Assert.IsTrue(jobs.Contains("farmer"));
            Assert.AreEqual(0, ActivityDatabaseValidator.MissionsWithoutRealActivity(w.Db, w.Acts, "teacher").Count);
            Assert.AreEqual(1, ActivityDatabaseValidator.MissionsWithoutRealActivity(w.Db, w.Acts, "farmer").Count);
        }
    }
}
