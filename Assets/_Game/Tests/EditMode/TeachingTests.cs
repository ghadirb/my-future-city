using System;
using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    /// <summary>
    /// The teacher profession must be real teaching: shuffled choices, a second chance with another way
    /// to explain, honest logging of what the child did, and a report that comes only from those events.
    /// </summary>
    [TestFixture]
    public class TeachingTests
    {
        // ---------- helpers ----------

        private static LessonStep Step(string id, string kind, int options, int correct)
        {
            var s = new LessonStep { id = id, kind = kind, promptFa = "سؤال " + id, correctIndex = correct, retryHintFa = "توضیح دیگر", successFa = "آفرین" };
            for (int i = 0; i < options; i++)
            {
                s.options.Add("گزینه " + i);
                s.feedbackFa.Add("بازخورد " + i);
            }
            return s;
        }

        private static LessonDatabase Db()
        {
            var db = new LessonDatabase();
            for (int l = 1; l <= 2; l++)
            {
                var lesson = new LessonDefinition { id = "L" + l, titleFa = "درس " + l, topicFa = "موضوع", conceptFa = "مفهوم " + l };
                lesson.steps.Add(Step("L" + l + "p1", StepKinds.Present, 3, 0));
                lesson.steps.Add(Step("L" + l + "e1", StepKinds.Explain, 3, 1));
                lesson.steps.Add(Step("L" + l + "c1", StepKinds.Check, 3, 2));
                lesson.steps.Add(Step("L" + l + "c2", StepKinds.Check, 3, 0));
                lesson.steps.Add(Step("L" + l + "h1", StepKinds.Help, 3, 1));
                lesson.steps.Add(Step("L" + l + "h2", StepKinds.Help, 3, 2));
                db.lessons.Add(lesson);
            }
            for (int m = 1; m <= 3; m++) db.manageSteps.Add(Step("M" + m, StepKinds.Manage, 3, m - 1));
            return db;
        }

        private static TeachingService Service(SaveData save, int seed)
        {
            return new TeachingService(Db(), save, new ActivityLog(save), new Random(seed));
        }

        private static int WrongIndex(TeachingSession s)
        {
            for (int i = 0; i < s.OptionCount; i++) if (i != s.CorrectDisplayIndex) return i;
            return -1;
        }

        private static TeachingSession AnswerCorrectly(TeachingService svc, LessonStep step)
        {
            var s = svc.Begin(step);
            s.Choose(s.CorrectDisplayIndex);
            return s;
        }

        // ---------- the session: shuffling and two chances ----------

        [Test]
        public void CorrectFirstChoiceFinishesTheStep()
        {
            var s = new TeachingSession(Step("a", StepKinds.Check, 3, 1), new Random(1));
            var r = s.Choose(s.CorrectDisplayIndex);
            Assert.AreEqual(StepOutcome.Correct, r.Outcome);
            Assert.IsTrue(r.FirstTry);
            Assert.IsTrue(s.Finished);
            Assert.IsTrue(s.FirstTryCorrect);
            Assert.IsFalse(s.UsedHint);
        }

        [Test]
        public void WrongChoiceShowsAnotherExplanationAndAllowsRetry()
        {
            var s = new TeachingSession(Step("a", StepKinds.Help, 3, 0), new Random(2));
            var r = s.Choose(WrongIndex(s));
            Assert.AreEqual(StepOutcome.WrongRetry, r.Outcome);
            Assert.IsFalse(s.Finished);
            Assert.IsTrue(s.UsedHint);
            Assert.AreEqual("توضیح دیگر", r.RetryHintFa);
            Assert.AreEqual(2, s.OptionCount, "the wrong option is removed");

            var r2 = s.Choose(s.CorrectDisplayIndex);
            Assert.AreEqual(StepOutcome.Correct, r2.Outcome);
            Assert.IsFalse(r2.FirstTry);
            Assert.IsFalse(s.FirstTryCorrect);
            Assert.AreEqual(2, s.Attempts);
        }

        [Test]
        public void SecondWrongChoiceRevealsTheBestAnswerSoTheChildIsNeverStuck()
        {
            var s = new TeachingSession(Step("a", StepKinds.Manage, 3, 0), new Random(3));
            s.Choose(WrongIndex(s));
            var r = s.Choose(WrongIndex(s));
            Assert.AreEqual(StepOutcome.Revealed, r.Outcome);
            Assert.IsTrue(s.Finished);
            Assert.AreEqual("گزینه 0", r.CorrectTextFa);
            Assert.IsFalse(s.FirstTryCorrect);
        }

        [Test]
        public void TwoOptionStepRevealsAfterOneWrongChoice()
        {
            var s = new TeachingSession(Step("a", StepKinds.Check, 2, 1), new Random(4));
            var r = s.Choose(WrongIndex(s));
            Assert.AreEqual(StepOutcome.Revealed, r.Outcome);
            Assert.IsTrue(s.Finished);
        }

        [Test]
        public void BestAnswerPositionIsRandom()
        {
            var positions = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
                positions.Add(new TeachingSession(Step("a", StepKinds.Check, 3, 0), new Random(seed)).CorrectDisplayIndex);
            Assert.IsTrue(positions.Count >= 2, "the best answer must not always be in the same place");
        }

        [Test]
        public void InvalidOrLateChoicesAreIgnored()
        {
            var s = new TeachingSession(Step("a", StepKinds.Check, 3, 0), new Random(5));
            Assert.IsNull(s.Choose(-1));
            Assert.IsNull(s.Choose(99));
            Assert.AreEqual(0, s.Attempts);
            s.Choose(s.CorrectDisplayIndex);
            Assert.IsNull(s.Choose(0), "a finished step cannot be answered again");
            Assert.AreEqual(1, s.Attempts);
        }

        // ---------- lesson data validation ----------

        [Test]
        public void ValidLessonDataPassesValidation()
        {
            Assert.AreEqual(0, LessonDatabaseValidator.Validate(Db()).Count);
        }

        [Test]
        public void BrokenLessonDataIsReported()
        {
            var db = Db();
            db.lessons[0].steps[0].correctIndex = 9;
            db.lessons[0].steps[1].options.RemoveRange(1, 2);
            db.lessons[0].steps[1].feedbackFa.Clear();
            db.lessons[0].steps[2].retryHintFa = "";
            db.lessons[1].steps.RemoveAll(s => s.kind == StepKinds.Help);
            db.manageSteps.RemoveAt(0);
            var errors = LessonDatabaseValidator.Validate(db);
            Assert.GreaterOrEqual(errors.Count, 5);
            Assert.IsTrue(errors.Exists(e => e.Contains("correctIndex")));
            Assert.IsTrue(errors.Exists(e => e.Contains("2-4 options")));
            Assert.IsTrue(errors.Exists(e => e.Contains("retryHintFa")));
            Assert.IsTrue(errors.Exists(e => e.Contains("'help'")));
            Assert.IsTrue(errors.Exists(e => e.Contains("manage")));
        }

        [Test]
        public void DuplicateStepIdsAreReported()
        {
            var db = Db();
            db.lessons[1].steps[0].id = db.lessons[0].steps[0].id;
            Assert.IsTrue(LessonDatabaseValidator.Validate(db).Exists(e => e.Contains("duplicate step id")));
        }

        // ---------- the service ----------

        [Test]
        public void ChoosingALessonIsSavedAndLogged()
        {
            var save = new SaveData();
            var svc = Service(save, 1);
            Assert.IsNull(svc.CurrentLesson);
            Assert.IsFalse(svc.ChooseLesson("nope", "m"));
            Assert.IsTrue(svc.ChooseLesson("L2", "teach_01"));
            Assert.AreEqual("L2", svc.CurrentLesson.id);
            Assert.AreEqual("L2", save.teachingLessonId);
            Assert.AreEqual(1, svc.Log.Count("teacher", ActivityIds.LessonChosen));
        }

        [Test]
        public void StepsNeedAChosenLessonExceptClassManagement()
        {
            var svc = Service(new SaveData(), 1);
            Assert.IsNull(svc.NextStep(StepKinds.Present));
            Assert.IsNull(svc.NextStep(StepKinds.Help));
            Assert.IsNotNull(svc.NextStep(StepKinds.Manage));
        }

        [Test]
        public void NextStepRotatesThroughTheLessonAsStepsAreDone()
        {
            var svc = Service(new SaveData(), 1);
            svc.ChooseLesson("L1", "m");
            var first = svc.NextStep(StepKinds.Help);
            Assert.AreEqual("L1h1", first.id);
            Assert.IsTrue(svc.RecordStep("m", AnswerCorrectly(svc, first)));
            Assert.AreEqual("L1h2", svc.NextStep(StepKinds.Help).id);
        }

        [Test]
        public void FinishedStepIsLoggedWithHowItWent()
        {
            var save = new SaveData();
            var svc = Service(save, 7);
            svc.ChooseLesson("L1", "m");

            var good = AnswerCorrectly(svc, svc.NextStep(StepKinds.Check));
            Assert.IsTrue(svc.RecordStep("m", good));

            var step = svc.NextStep(StepKinds.Check);
            var shaky = svc.Begin(step);
            shaky.Choose(WrongIndex(shaky));
            shaky.Choose(shaky.CorrectDisplayIndex);
            Assert.IsTrue(svc.RecordStep("m", shaky));

            var counter = save.activity.GetCounter("teacher", ActivityIds.StudentCheck, false);
            Assert.AreEqual(2, counter.count);
            Assert.AreEqual(1, counter.firstTry);
            Assert.AreEqual(1, counter.neededHelp);
        }

        [Test]
        public void UnfinishedStepIsNotLogged()
        {
            var save = new SaveData();
            var svc = Service(save, 1);
            svc.ChooseLesson("L1", "m");
            var s = svc.Begin(svc.NextStep(StepKinds.Present));
            Assert.IsFalse(svc.RecordStep("m", s));
            s.Choose(WrongIndex(s));
            Assert.IsFalse(svc.RecordStep("m", s), "a retry in progress is not a finished activity");
            Assert.AreEqual(0, svc.Log.Count("teacher", ActivityIds.PresentConcept));
        }

        // ---------- the activity log ----------

        [Test]
        public void SameActivityInTheSameRunIsLoggedOnlyOnce()
        {
            var save = new SaveData();
            var log = new ActivityLog(save);
            Assert.IsTrue(log.Record("teacher", "m", ActivityIds.HelpStudent, "h1", ActivityResult.FirstTry));
            Assert.IsFalse(log.Record("teacher", "m", ActivityIds.HelpStudent, "h1", ActivityResult.FirstTry));
            Assert.AreEqual(1, log.Count("teacher", ActivityIds.HelpStudent));
            Assert.AreEqual(1, save.activity.events.Count);
        }

        [Test]
        public void ANewRunMayLogTheSameActivityAgain()
        {
            var save = new SaveData();
            var log = new ActivityLog(save);
            log.Record("teacher", "m", ActivityIds.HelpStudent, "h1", ActivityResult.FirstTry);
            save.attemptSeq++;                      // the run was completed or given up
            Assert.IsTrue(log.Record("teacher", "m", ActivityIds.HelpStudent, "h1", ActivityResult.FirstTry));
            Assert.AreEqual(2, log.Count("teacher", ActivityIds.HelpStudent));
        }

        [Test]
        public void ActivityLogNeverTouchesCoinsOrXp()
        {
            var w = TestFixtures.CreateWorld();
            var log = new ActivityLog(w.Save);
            for (int i = 0; i < 5; i++) log.Record("teacher", "m", ActivityIds.ManageClass, "s" + i, ActivityResult.FirstTry);
            Assert.AreEqual(0, w.Save.coins);
            Assert.AreEqual(0, w.Save.xp);
        }

        [Test]
        public void OldEventsAreTrimmedButTotalsStayExact()
        {
            var save = new SaveData();
            var log = new ActivityLog(save);
            for (int i = 0; i < ActivityLog.MaxEvents + 25; i++)
                log.Record("teacher", "m", ActivityIds.ManageClass, "s" + i, ActivityResult.None);
            Assert.AreEqual(ActivityLog.MaxEvents, save.activity.events.Count);
            Assert.AreEqual(ActivityLog.MaxEvents + 25, log.Count("teacher", ActivityIds.ManageClass));
        }

        [Test]
        public void DamagedActivityDataIsRepaired()
        {
            var save = new SaveData();
            save.activity = null;
            save.teachingLessonId = null;
            save.Sanitize();
            Assert.IsNotNull(save.activity);
            Assert.AreEqual("", save.teachingLessonId);

            save.activity.events.Add(null);
            save.activity.events.Add(new ActivityEvent { jobId = "", activityId = "x" });
            save.activity.counters.Add(new ActivityCounter { jobId = "teacher", activityId = "a", count = -4 });
            save.Sanitize();
            Assert.AreEqual(0, save.activity.events.Count);
            Assert.AreEqual(0, save.activity.counters[0].count);
        }

        [Test]
        public void OldSaveWithoutActivityDataLoads()
        {
            var save = new SaveData { version = 2, characterId = "boy", coins = 5 };
            save.activity = null;
            save.Sanitize();
            Assert.AreEqual(SaveData.CurrentVersion, save.version);
            Assert.AreEqual(5, save.coins);
            Assert.AreEqual(0, ActivityAnalyzer.RealActivityCount(save, "teacher"));
        }

        // ---------- the report ----------

        [Test]
        public void NoReportLinesWithoutRealTeachingActivity()
        {
            var save = new SaveData();
            Assert.AreEqual(0, ActivityAnalyzer.TeacherLinesFa(save, Db()).Count);

            // Choosing a lesson alone is preparation, not teaching.
            var svc = Service(save, 1);
            svc.ChooseLesson("L1", "m");
            Assert.AreEqual(0, ActivityAnalyzer.RealActivityCount(save, "teacher"));
            Assert.AreEqual(0, ActivityAnalyzer.TeacherLinesFa(save, Db()).Count);
        }

        [Test]
        public void ReportLinesComeFromRealEventsAndNameTheChosenLesson()
        {
            var save = new SaveData();
            var svc = Service(save, 3);
            svc.ChooseLesson("L2", "m");
            svc.RecordStep("m", AnswerCorrectly(svc, svc.NextStep(StepKinds.Present)));
            var shaky = svc.Begin(svc.NextStep(StepKinds.Explain));
            shaky.Choose(WrongIndex(shaky));
            shaky.Choose(shaky.CorrectDisplayIndex);
            svc.RecordStep("m", shaky);

            Assert.AreEqual(2, ActivityAnalyzer.RealActivityCount(save, "teacher"));
            var lines = ActivityAnalyzer.TeacherLinesFa(save, Db());
            Assert.AreEqual(3, lines.Count);
            Assert.IsTrue(lines[0].Contains("2 بار"));
            Assert.IsTrue(lines[1].Contains("درس 2"));
            Assert.IsTrue(lines[2].Contains("1 بار"));
        }

        [Test]
        public void ReportNeverMakesAVerdictAboutTheChild()
        {
            var save = new SaveData();
            var svc = Service(save, 3);
            svc.ChooseLesson("L1", "m");
            svc.RecordStep("m", AnswerCorrectly(svc, svc.NextStep(StepKinds.Check)));
            foreach (var line in ActivityAnalyzer.TeacherLinesFa(save, Db()))
            {
                Assert.IsFalse(line.Contains("استعداد"), line);
                Assert.IsFalse(line.Contains("نمره"), line);
            }
            Assert.IsTrue(ActivityAnalyzer.TextNote.Contains("نتیجهٔ علمی نیست"));
        }

        // ---------- the whole profession ----------

        [Test]
        public void ACompleteLessonLogsEveryKindOfRealTeachingWork()
        {
            var save = new SaveData();
            var svc = Service(save, 11);
            Assert.IsTrue(svc.ChooseLesson("L1", "m5"));
            int firstTry = 0, total = 0;
            string[] order = { StepKinds.Present, StepKinds.Explain, StepKinds.Check, StepKinds.Manage, StepKinds.Help };
            foreach (var kind in order)
            {
                var s = AnswerCorrectly(svc, svc.RandomStep(kind));
                Assert.IsTrue(svc.RecordStep("m5", s), kind);
                total++;
                if (s.FirstTryCorrect) firstTry++;
            }
            Assert.IsTrue(svc.RecordFullSession("m5", total, firstTry));

            var done = ActivityAnalyzer.DistinctActivities(save, "teacher");
            Assert.AreEqual(6, done.Count);
            Assert.IsTrue(done.Contains(ActivityIds.PresentConcept));
            Assert.IsTrue(done.Contains(ActivityIds.ExplainConcept));
            Assert.IsTrue(done.Contains(ActivityIds.StudentCheck));
            Assert.IsTrue(done.Contains(ActivityIds.HelpStudent));
            Assert.IsTrue(done.Contains(ActivityIds.ManageClass));
            Assert.IsTrue(done.Contains(ActivityIds.FullSession));
        }

        [Test]
        public void CollectingItemsAloneLogsNoTeachingWork()
        {
            var w = TestFixtures.CreateWorld();
            var log = new ActivityLog(w.Save);
            AttemptTrackingTests.Play(w, "m1", 30);     // an item-collecting stage of another job
            Assert.AreEqual(0, ActivityAnalyzer.RealActivityCount(w.Save, "teacher"));
            Assert.AreEqual(0, ActivityAnalyzer.DistinctActivities(w.Save, "teacher").Count);
            Assert.AreEqual(0, log.Count("teacher", ActivityIds.PresentConcept));
        }

        [Test]
        public void ARunningMissionThatNoLongerExistsIsSwitchedOff()
        {
            var save = new SaveData();
            var ghost = save.GetMission("teach_01_books", true);
            ghost.active = true;
            ghost.timesCompleted = 1;
            var db = TestFixtures.CreateDb();
            var wallet = new Wallet(save);
            var ms = new MissionService(db, save, wallet, new CityProgressService(db, save));
            Assert.IsNull(ms.ActiveMission);
            Assert.IsFalse(ghost.active);
            Assert.AreEqual(1, ghost.timesCompleted, "history is kept");
            Assert.IsTrue(ms.Start("m1"), "a real mission can be started");
        }
    }
}
