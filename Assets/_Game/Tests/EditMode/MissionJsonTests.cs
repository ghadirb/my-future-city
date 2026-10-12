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
        public void MvpHasThreeCompleteFiveStageJobsWithRewards()
        {
            var db = Load();
            Assert.GreaterOrEqual(db.missions.Count, 5);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "engineer").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "teacher").Count);
            Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == "doctor").Count);
            foreach (var m in db.missions)
            {
                Assert.GreaterOrEqual(m.objectives.Count, 1);
                Assert.IsTrue(m.reward.coins > 0 || m.reward.xp > 0, m.id + " has no reward");
            }
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
