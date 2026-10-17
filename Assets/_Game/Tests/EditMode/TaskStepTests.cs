using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    /// <summary>
    /// Acceptance tests of the "child does the work directly" rule: a hands-on task step can only be finished by
    /// placing every item on its right slot; a dialog choice can never finish it.
    /// </summary>
    [TestFixture]
    public class TaskStepTests
    {
        private const string Wrong = "این وسیله برای این درس مناسب نیست؛ دوباره نگاه کن.";

        private static TaskSpec Spec()
        {
            // slot i wants item answer[i]
            return new TaskSpec
            {
                itemsFa = new List<string> { "مهره", "تصویر برگ", "ساعت کاغذی" },
                slotsFa = new List<string> { "درس شناخت طبیعت", "درس ساعت", "درس جمع" },
                answer = new List<int> { 1, 2, 0 },
                wrongFa = new List<string> { Wrong, Wrong + " یک بار دیگر." },
                rightFa = "{item} برای {slot} درست است.",
                doneFa = "همهٔ وسیله‌ها سر جایشان است."
            };
        }

        private class W
        {
            public MissionDatabase Db; public SaveData Save; public MissionService Missions; public ActivityService Acts; public ActivityDatabase Adb;
        }

        private static W Create()
        {
            var db = new MissionDatabase();
            db.jobs.Add(new JobDefinition { id = "teacher", nameFa = "معلم" });
            db.missions.Add(new MissionDefinition
            {
                id = "t1", jobId = "teacher", nameFa = "تدریس", startNpcId = "npc", repeatable = true, repeatCoins = 3,
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "interact", targetId = "teach", count = 1, textFa = "تدریس" },
                    new ObjectiveDefinition { type = "talk", targetId = "npc", count = 1, textFa = "گفتگو" }
                },
                reward = new RewardDefinition { coins = 20, xp = 30 }
            });
            var adb = new ActivityDatabase();
            adb.activities.Add(new ActivityDefinition
            {
                missionId = "t1", targetId = "teach", jobId = "teacher",
                realWorldFa = "معلم وسیلهٔ آموزشی را برای درس آماده می‌کند", childDoesFa = "کودک وسیله را روی درس می‌گذارد", skillFa = "آماده‌سازی",
                scenes = new List<ActivityScene>
                {
                    new ActivityScene { titleFa = "چیدن وسیله‌ها", steps = new List<ActivityStep>
                    {
                        new ActivityStep { promptFa = "هر وسیله را برای درس خودش بگذار.", hintFa = "فکر کن با کدام وسیله می‌شود جمع را نشان داد.", task = Spec() }
                    } }
                }
            });
            var w = new W { Db = db, Adb = adb, Save = new SaveData() };
            var wallet = new Wallet(w.Save);
            var city = new CityProgressService(db, w.Save);
            w.Missions = new MissionService(db, w.Save, wallet, city);
            w.Acts = new ActivityService(adb, w.Save, w.Missions);
            Assert.IsTrue(w.Missions.Start("t1"));
            return w;
        }

        [Test]
        public void ValidatorAcceptsAGoodTask()
        {
            var w = Create();
            Assert.AreEqual(0, ActivityDatabaseValidator.Validate(w.Adb, w.Db).Count);
        }

        [Test]
        public void ValidatorRejectsAlreadyMatchedAndBrokenTasks()
        {
            var w = Create();
            var t = w.Adb.activities[0].scenes[0].steps[0].task;
            t.answer = new List<int> { 0, 1, 2 };
            Assert.IsTrue(ActivityDatabaseValidator.Validate(w.Adb, w.Db).Count > 0, "identity order must be rejected");
            t.answer = new List<int> { 1, 1, 0 };
            Assert.IsTrue(ActivityDatabaseValidator.Validate(w.Adb, w.Db).Count > 0, "answer must be a permutation");
            t.answer = new List<int> { 1, 2, 0 };
            t.wrongFa = new List<string> { "نه" };
            Assert.IsTrue(ActivityDatabaseValidator.Validate(w.Adb, w.Db).Count > 0, "wrong feedback must teach");
        }

        [Test]
        public void ADialogChoiceCanNeverFinishATaskStep()
        {
            var w = Create();
            var s = w.Acts.Begin("t1", "teach", 0);
            for (int i = 0; i < 5; i++) Assert.IsFalse(s.Choose(i).Accepted);
            Assert.IsFalse(s.Finished);
            Assert.AreEqual(0, w.Missions.GetCountByTarget("t1", "teach"));
        }

        [Test]
        public void OnlyPlacingEveryItemFinishesTheStepAndTheMission()
        {
            var w = Create();
            var s = w.Acts.Begin("t1", "teach", 0);
            Assert.IsTrue(s.PerformAction(1, 0).Placed);
            Assert.IsFalse(s.Finished);
            Assert.IsTrue(s.PerformAction(2, 1).Placed);
            var last = s.PerformAction(0, 2);
            Assert.IsTrue(last.Accepted);
            Assert.IsTrue(last.SceneFinished);
            Assert.IsTrue(s.Finished);
            Assert.AreEqual(1, w.Missions.GetCountByTarget("t1", "teach"));
            w.Missions.Notify(ObjectiveType.Talk, "npc");
            Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus("t1"));
        }

        [Test]
        public void WrongPlacementsAreCountedTeachAndCostNothing()
        {
            var w = Create();
            var s = w.Acts.Begin("t1", "teach", 0);
            var r = s.PerformAction(0, 0);                // seed on the nature lesson: wrong
            Assert.IsFalse(r.Correct);
            Assert.IsFalse(r.Accepted);
            StringAssert.Contains("مناسب نیست", r.FeedbackFa);
            Assert.IsFalse(s.IsSlotFilled(0));
            var r2 = s.PerformAction(2, 0);               // second wrong: another line
            Assert.IsTrue(r.FeedbackFa != r2.FeedbackFa);
            var st = w.Save.GetActivityStats("teacher", false);
            Assert.AreEqual(2, st.actions);
            Assert.AreEqual(0, st.actionsRight);
            Assert.AreEqual(0, st.tasksDone);
        }

        [Test]
        public void FilledSlotsAndUsedItemsAreIgnoredAndNotDoubleCounted()
        {
            var w = Create();
            var s = w.Acts.Begin("t1", "teach", 0);
            s.PerformAction(1, 0);
            Assert.IsTrue(s.PerformAction(1, 1).Ignored);
            Assert.IsTrue(s.PerformAction(2, 0).Ignored);
            Assert.AreEqual(1, w.Save.GetActivityStats("teacher", false).actions);
        }

        [Test]
        public void FinishedTaskIsRecordedAsDirectActionsApartFromRewards()
        {
            var w = Create();
            var s = w.Acts.Begin("t1", "teach", 0);
            s.PerformAction(0, 0);                        // one mistake
            s.PerformAction(1, 0); s.PerformAction(2, 1); s.PerformAction(0, 2);
            var st = w.Save.GetActivityStats("teacher", false);
            Assert.AreEqual(4, st.actions);
            Assert.AreEqual(3, st.actionsRight);
            Assert.AreEqual(1, st.tasksDone);
            Assert.AreEqual(0, s.FirstTryCorrect, "a mistake means the step was not a first-try success");
            Assert.AreEqual(0, w.Save.coins, "no coins before the mission itself is completed");

            var report = InterestAnalyzer.Analyze(w.Db, w.Save);
            Assert.AreEqual(4, report.Jobs.Find(j => j.JobId == "teacher").DirectActions);
            Assert.AreEqual(1, report.Jobs.Find(j => j.JobId == "teacher").TasksDone);
        }
    }
}
