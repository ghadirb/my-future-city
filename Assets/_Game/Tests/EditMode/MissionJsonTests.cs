using MyFutureCity.Core;
using NUnit.Framework;
using UnityEngine;

namespace MyFutureCity.Tests
{
    /// <summary>Unity-dependent checks (JsonUtility, Resources). Run by the Unity test runner only.</summary>
    [TestFixture]
    public class MissionJsonTests
    {
        private static MissionDatabase Load()
        {
            var asset = Resources.Load<TextAsset>("Data/missions");
            Assert.IsNotNull(asset, "Resources/Data/missions.json is missing");
            return JsonUtility.FromJson<MissionDatabase>(asset.text);
        }

        [Test]
        public void ShippedMissionDataIsValid()
        {
            var db = Load();
            var errors = MissionDatabaseValidator.Validate(db);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors.ToArray()));
        }

        [Test]
        public void TenCompleteFiveStageJobsWithRewards()
        {
            var db = Load();
            Assert.GreaterOrEqual(db.missions.Count, 5);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "engineer").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "teacher").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "doctor").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "environment").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "farmer").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "firefighter").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "transport").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "vet").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "merchant").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "tech").Count);
            Assert.AreEqual(10, db.jobs.Count);
            Assert.AreEqual(50, db.missions.Count);
            foreach (var m in db.missions)
            {
                Assert.GreaterOrEqual(m.objectives.Count, 1);
                Assert.IsTrue(m.reward.coins > 0 || m.reward.xp > 0, m.id + " has no reward");
            }
        }

        private static LessonDatabase LoadLessons()
        {
            var asset = Resources.Load<TextAsset>("Data/lessons");
            Assert.IsNotNull(asset, "Resources/Data/lessons.json is missing");
            return JsonUtility.FromJson<LessonDatabase>(asset.text);
        }

        [Test]
        public void ShippedLessonDataIsValid()
        {
            var errors = LessonDatabaseValidator.Validate(LoadLessons());
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors.ToArray()));
        }

        [Test]
        public void TeacherStagesRequireRealTeachingWork()
        {
            var db = Load();
            var teacher = db.missions.FindAll(m => m.jobId == "teacher");
            Assert.AreEqual(5, teacher.Count);
            for (int i = 0; i < teacher.Count; i++)
            {
                var m = teacher[i];
                bool teaches = m.objectives.Exists(o => o.ParsedType() == ObjectiveType.Interact &&
                                                         TeacherLessonInteractable.IsTeachingTarget(o.targetId));
                Assert.IsTrue(teaches, m.id + " has no real teaching activity");
                if (i > 0)
                    Assert.IsFalse(m.objectives.Exists(o => o.ParsedType() == ObjectiveType.Collect),
                                   m.id + " must not be an item hunt");
            }
            Assert.IsTrue(teacher[0].objectives.Exists(o => o.targetId == "lesson_topic"), "stage 1 must let the child choose a lesson");
            Assert.IsTrue(teacher[4].objectives.Exists(o => o.targetId == "teach_full"), "stage 5 must run a whole lesson");
        }

        [Test]
        public void ActivityDataSurvivesJsonRoundTrip()
        {
            var s = new SaveData { teachingLessonId = "lesson_math" };
            new ActivityLog(s).Record("teacher", "m", ActivityIds.HelpStudent, "h1", ActivityResult.NeededHelp);
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(s));
            back.Sanitize();
            Assert.AreEqual("lesson_math", back.teachingLessonId);
            Assert.AreEqual(1, back.activity.events.Count);
            Assert.AreEqual(1, back.activity.counters[0].neededHelp);
        }

        [Test]
        public void SaveDataSurvivesJsonRoundTrip()
        {
            var s = new SaveData { characterId = "girl", coins = 42, xp = 7 };
            s.GetMission("a", true).timesCompleted = 2;
            s.GetMission("a", true).objectiveCounts.Add(3);
            s.GetBuilding("school", true).progress = 75;
            s.badges.Add("b1");
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(s));
            back.Sanitize();
            Assert.AreEqual("girl", back.characterId);
            Assert.AreEqual(42, back.coins);
            Assert.AreEqual(2, back.GetMission("a", false).timesCompleted);
            Assert.AreEqual(3, back.GetMission("a", false).objectiveCounts[0]);
            Assert.AreEqual(75, back.GetBuilding("school", false).progress);
            Assert.AreEqual(1, back.badges.Count);
        }

        [Test]
        public void GarbageJsonDoesNotThrowAfterSanitize()
        {
            SaveData d = null;
            try { d = JsonUtility.FromJson<SaveData>("{\"coins\": -9, \"missions\": null}"); }
            catch (System.Exception) { }
            if (d == null) d = new SaveData();
            d.Sanitize();
            Assert.AreEqual(0, d.coins);
            Assert.IsNotNull(d.missions);
        }

        [Test]
        public void PersianFontIsBundledInResources()
        {
            Assert.IsNotNull(Resources.Load<Font>("Fonts/Vazirmatn-Regular"));
        }
    }
}
