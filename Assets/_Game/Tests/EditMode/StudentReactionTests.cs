using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    /// <summary>
    /// P4: the student's reactions depend on what the child really chooses, and repeated mistakes change the path of
    /// the lesson (a support step is played before the original step is tried again).
    /// </summary>
    [TestFixture]
    public class StudentReactionTests
    {
        private static ActivityOption O(string t, bool ok) { return new ActivityOption { textFa = t, correct = ok, feedbackFa = "بازخورد " + t }; }

        private static ActivityStep Step(string prompt, params ActivityOption[] opts)
        {
            return new ActivityStep { promptFa = prompt, hintFa = "راهنما", options = new List<ActivityOption>(opts) };
        }

        private static StudentReactions Reactions()
        {
            return new StudentReactions
            {
                unsureFa = new List<string> { "نگران {name}" },
                discouragedFa = new List<string> { "دل‌سرد {name}" },
                reliefFa = new List<string> { "آسوده {name}" },
                confidentFa = new List<string> { "مطمئن {name}" },
                endingGoodFa = new List<string> { "پایان خوب {name}" },
                endingReviewFa = new List<string> { "پایان مرور {name}" }
            };
        }

        private class W
        {
            public SaveData Save;
            public MissionService Missions;
            public ActivityService Activities;
            public ActivityDatabase Acts;
            public MissionDatabase Db;
        }

        private static W Create(bool reactions, bool support)
        {
            var db = new MissionDatabase();
            db.jobs.Add(new JobDefinition { id = "teacher", nameFa = "معلم" });
            db.missions.Add(new MissionDefinition
            {
                id = "s1", jobId = "teacher", nameFa = "کمک", startNpcId = "npc", repeatable = true, repeatCoins = 1,
                objectives = new List<ObjectiveDefinition> { new ObjectiveDefinition { type = "interact", targetId = "help", count = 1, textFa = "کمک" } },
                reward = new RewardDefinition { coins = 5, xp = 5 }
            });
            var def = new ActivityDefinition
            {
                missionId = "s1", targetId = "help", jobId = "teacher",
                realWorldFa = "معلم به دانش‌آموز کمک می‌کند", childDoesFa = "کودک کمک می‌کند", skillFa = "همدلی",
                scenes = new List<ActivityScene>
                {
                    new ActivityScene { titleFa = "کمک به علی", studentFa = "علی", steps = new List<ActivityStep>
                    {
                        Step("سؤال ۱", O("غلط", false), O("درست", true), O("غلط۲", false)),
                        Step("سؤال ۲", O("غلط", false), O("غلط۲", false), O("درست", true))
                    } }
                }
            };
            if (reactions) def.reactions = Reactions();
            if (support) def.supportStep = Step("دل‌گرمی", O("عجله", false), O("دلداری", true));
            var acts = new ActivityDatabase();
            acts.activities.Add(def);
            var w = new W { Db = db, Acts = acts, Save = new SaveData() };
            var wallet = new Wallet(w.Save);
            var city = new CityProgressService(db, w.Save);
            w.Missions = new MissionService(db, w.Save, wallet, city);
            w.Activities = new ActivityService(acts, w.Save, w.Missions);
            return w;
        }

        private static ActivitySession Begin(W w)
        {
            Assert.IsTrue(w.Missions.Start("s1"));
            var s = w.Activities.Begin("s1", "help", 0);
            Assert.IsNotNull(s);
            return s;
        }

        private static int Wrong(ActivitySession s) { return s.WrongIndex(); }
        private static int Right(ActivitySession s) { return s.NextAcceptedIndex(); }

        [Test]
        public void RightFirstTimeMakesTheStudentConfidentAndNeedsNoSupport()
        {
            var s = Begin(Create(true, true));
            var r = s.Choose(Right(s));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual("مطمئن علی", r.ReactionFa);
            Assert.IsFalse(s.InSupport);
        }

        [Test]
        public void OneMistakeMakesTheStudentUnsureAndTheCorrectionBringsRelief()
        {
            var s = Begin(Create(true, true));
            var bad = s.Choose(Wrong(s));
            Assert.IsFalse(bad.Accepted);
            Assert.AreEqual("نگران علی", bad.ReactionFa);
            Assert.AreEqual(StudentMood.Unsure, s.Mood);
            Assert.IsFalse(bad.SupportStarted);
            var good = s.Choose(Right(s));
            Assert.AreEqual("آسوده علی", good.ReactionFa);
            Assert.AreEqual(StudentMood.Calm, s.Mood);
        }

        [Test]
        public void TwoMistakesInOneStepDiscourageTheStudentAndChangeThePath()
        {
            var s = Begin(Create(true, true));
            s.Choose(Wrong(s));
            var second = s.Choose(Wrong(s));
            Assert.IsTrue(second.SupportStarted);
            Assert.AreEqual("دل‌سرد علی", second.ReactionFa);
            Assert.IsTrue(s.InSupport);
            Assert.AreEqual("دل‌گرمی", s.CurrentStep.promptFa, "the detour step replaces the original step");
            Assert.AreEqual(0, s.StepIndex, "the lesson did not move on");
            Assert.AreEqual(0, s.StepsDone);

            // a wrong way of encouraging does not end the detour
            var rude = s.Choose(Wrong(s));
            Assert.IsFalse(rude.Accepted);
            Assert.IsTrue(s.InSupport);

            var kind = s.Choose(Right(s));
            Assert.IsTrue(kind.Accepted);
            Assert.IsTrue(kind.SupportDone);
            Assert.AreEqual("آسوده علی", kind.ReactionFa);
            Assert.IsFalse(s.InSupport);
            Assert.AreEqual("سؤال ۱", s.CurrentStep.promptFa, "the original step is tried again");
            Assert.AreEqual(0, s.StepsDone, "the detour is not a lesson step");
            Assert.AreEqual(0, s.FirstTryCorrect);
        }

        [Test]
        public void SupportDecisionsAreLoggedAsRealDecisionsAndCountAsMistakesWhenWrong()
        {
            var w = Create(true, true);
            var s = Begin(w);
            s.Choose(Wrong(s)); s.Choose(Wrong(s));
            s.Choose(Wrong(s));           // wrong support pick
            s.Choose(Right(s));           // right support pick
            int supportLogs = 0, supportWrong = 0;
            foreach (var c in w.Save.activityChoices)
                if (c.step == ActivitySession.SupportLogStep) { supportLogs++; if (!c.correct) supportWrong++; }
            Assert.AreEqual(2, supportLogs);
            Assert.AreEqual(1, supportWrong);
            Assert.AreEqual(3, s.WrongChoices);
        }

        [Test]
        public void SupportIsOfferedOnlyOncePerScene()
        {
            var s = Begin(Create(true, true));
            s.Choose(Wrong(s)); s.Choose(Wrong(s));
            s.Choose(Right(s));                       // support done
            s.Choose(Right(s));                       // step 1 right
            Assert.AreEqual("سؤال ۲", s.CurrentStep.promptFa);
            s.Choose(Wrong(s));
            var again = s.Choose(Wrong(s));
            Assert.IsFalse(again.SupportStarted);
            Assert.IsFalse(s.InSupport);
        }

        [Test]
        public void TheEndingOfTheSceneFollowsWhatHappened()
        {
            var clean = Begin(Create(true, true));
            clean.Choose(Right(clean));
            var endClean = clean.Choose(Right(clean));
            Assert.IsTrue(endClean.SceneFinished);
            Assert.AreEqual("پایان خوب علی", endClean.EndingFa);

            var rough = Begin(Create(true, true));
            rough.Choose(Wrong(rough)); rough.Choose(Wrong(rough)); rough.Choose(Right(rough));   // detour
            rough.Choose(Right(rough));
            var endRough = rough.Choose(Right(rough));
            Assert.IsTrue(endRough.SceneFinished);
            Assert.AreEqual("پایان مرور علی", endRough.EndingFa);
            Assert.IsFalse(endRough.EndingFa.Contains("{name}"));
        }

        [Test]
        public void ActivitiesWithoutReactionsBehaveAsBefore()
        {
            var s = Begin(Create(false, false));
            var bad = s.Choose(Wrong(s));
            Assert.AreEqual("", bad.ReactionFa);
            s.Choose(Wrong(s));
            Assert.IsFalse(s.InSupport);
            var ok = s.Choose(Right(s));
            Assert.AreEqual("", ok.ReactionFa);
        }

        [Test]
        public void EmptyObjectsFromJsonUtilityCountAsMissing()
        {
            // Unity's JsonUtility fills a missing nested object with an empty one, never null.
            var w = Create(false, false);
            w.Acts.activities[0].reactions = new StudentReactions();
            w.Acts.activities[0].supportStep = new ActivityStep();
            Assert.IsFalse(w.Acts.activities[0].HasReactions);
            Assert.IsFalse(w.Acts.activities[0].HasSupport);
            var s = Begin(w);
            s.Choose(Wrong(s)); s.Choose(Wrong(s));
            Assert.IsFalse(s.InSupport);
            Assert.AreEqual(0, ActivityDatabaseValidator.Validate(w.Acts, w.Db).Count, "empty placeholders are not errors");
        }

        [Test]
        public void ValidatorRejectsASupportStepWithoutReactionsOrWithABrokenPool()
        {
            var w = Create(false, true);
            Assert.IsTrue(ActivityDatabaseValidator.Validate(w.Acts, w.Db).Exists(e => e.Contains("reactions")));
            var w2 = Create(true, true);
            w2.Acts.activities[0].reactions.reliefFa = new List<string>();
            Assert.IsTrue(ActivityDatabaseValidator.Validate(w2.Acts, w2.Db).Exists(e => e.Contains("reliefFa")));
            var w3 = Create(true, true);
            Assert.AreEqual(0, ActivityDatabaseValidator.Validate(w3.Acts, w3.Db).Count);
        }
    }
}
