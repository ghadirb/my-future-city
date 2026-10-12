using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class SaveDataTests
    {
        [Test]
        public void SanitizeRepairsNullsAndNegativeValues()
        {
            var s = new SaveData
            {
                coins = -5, xp = -1, characterId = null, lastScene = null,
                missions = null, badges = null, buildings = null, settings = null
            };
            s.Sanitize();
            Assert.AreEqual(0, s.coins);
            Assert.AreEqual(0, s.xp);
            Assert.AreEqual("", s.characterId);
            Assert.IsNotNull(s.missions);
            Assert.IsNotNull(s.badges);
            Assert.IsNotNull(s.buildings);
            Assert.IsNotNull(s.settings);
        }

        [Test]
        public void SanitizeRemovesBrokenAndDuplicateEntries()
        {
            var s = new SaveData();
            s.missions.Add(null);
            s.missions.Add(new MissionProgress { id = "" });
            s.missions.Add(new MissionProgress { id = "m1", timesCompleted = 2 });
            s.missions.Add(new MissionProgress { id = "m1", timesCompleted = 9 });
            s.buildings.Add(new BuildingProgress { id = "school", progress = 5000 });
            s.badges.Add("a"); s.badges.Add("a"); s.badges.Add(null);
            s.Sanitize();
            Assert.AreEqual(1, s.missions.Count);
            Assert.AreEqual(2, s.missions[0].timesCompleted);
            Assert.AreEqual(CityProgressService.MaxProgress, s.buildings[0].progress);
            Assert.AreEqual(1, s.badges.Count);
        }

        [Test]
        public void SanitizeKeepsOnlyOneActiveMission()
        {
            var s = new SaveData();
            s.missions.Add(new MissionProgress { id = "a", active = true });
            s.missions.Add(new MissionProgress { id = "b", active = true });
            s.Sanitize();
            int active = 0;
            foreach (var m in s.missions) if (m.active) active++;
            Assert.AreEqual(1, active);
        }

        [Test]
        public void SanitizeClampsSettings()
        {
            var s = new SaveData();
            s.settings.cameraSensitivity = 99f;
            s.settings.targetFps = 7;
            s.Sanitize();
            Assert.AreEqual(1f, s.settings.cameraSensitivity, 0.0001f);
            Assert.AreEqual(30, s.settings.targetFps);
        }

        [Test]
        public void ServicesStillWorkOnSanitizedPartialSave()
        {
            var db = TestFixtures.CreateDb();
            var save = new SaveData { missions = null, buildings = null };
            save.missions = new List<MissionProgress> { new MissionProgress { id = "m1", active = true, objectiveCounts = null } };
            save.Sanitize();
            var wallet = new Wallet(save);
            var city = new CityProgressService(db, save);
            var ms = new MissionService(db, save, wallet, city);
            Assert.AreEqual(MissionStatus.Active, ms.GetStatus("m1"));
            Assert.IsTrue(ms.Notify(ObjectiveType.Collect, "tool"));   // must not throw on empty counts
        }

        [Test]
        public void WalletRejectsNegativeAndOverspend()
        {
            var save = new SaveData();
            var w = new Wallet(save);
            w.Add(-10, -10);
            Assert.AreEqual(0, w.Coins);
            w.Add(50, 250);
            Assert.AreEqual(50, w.Coins);
            Assert.AreEqual(3, w.Level);
            Assert.IsFalse(w.TrySpend(51));
            Assert.IsTrue(w.TrySpend(20));
            Assert.AreEqual(30, w.Coins);
        }
    }
}
