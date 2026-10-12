using System.Collections.Generic;
using MyFutureCity.Core;

namespace MyFutureCity.Tests
{
    /// <summary>Builds a small in-code database that mirrors the structure of Resources/Data/missions.json.</summary>
    public static class TestFixtures
    {
        public class World
        {
            public MissionDatabase Db;
            public SaveData Save;
            public Wallet Wallet;
            public CityProgressService City;
            public MissionService Missions;
        }

        public static MissionDatabase CreateDb()
        {
            var db = new MissionDatabase();
            db.jobs.Add(new JobDefinition { id = "engineer", nameFa = "مهندس", badgeId = "badge_eng", badgeNameFa = "نشان" });
            db.jobs.Add(new JobDefinition { id = "farmer", nameFa = "کشاورز" });
            db.buildings.Add(new BuildingDefinition { id = "school", nameFa = "مدرسه" });
            db.buildings.Add(new BuildingDefinition { id = "clinic", nameFa = "درمانگاه" });

            db.missions.Add(new MissionDefinition
            {
                id = "m1", jobId = "engineer", nameFa = "یک", startNpcId = "npc",
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "collect", targetId = "tool", count = 1, textFa = "ابزار" },
                    new ObjectiveDefinition { type = "talk", targetId = "npc", count = 1, textFa = "گفتگو" }
                },
                reward = new RewardDefinition { coins = 10, xp = 20 }
            });
            db.missions.Add(new MissionDefinition
            {
                id = "m2", jobId = "engineer", nameFa = "دو", startNpcId = "npc", requiresMissionId = "m1",
                repeatable = true, repeatCoins = 5,
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "collect", targetId = "plank", count = 3, textFa = "تخته" },
                    new ObjectiveDefinition { type = "talk", targetId = "npc", count = 1, textFa = "گفتگو" }
                },
                reward = new RewardDefinition { coins = 15, xp = 30 }
            });
            db.missions.Add(new MissionDefinition
            {
                id = "m3", jobId = "engineer", nameFa = "سه", startNpcId = "npc", requiresMissionId = "m2",
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "interact", targetId = "repair", count = 3, textFa = "تعمیر", buildingId = "school", buildingProgress = 25 },
                    new ObjectiveDefinition { type = "talk", targetId = "npc", count = 1, textFa = "گفتگو" }
                },
                reward = new RewardDefinition { coins = 30, xp = 60, badgeId = "badge_eng", buildingId = "school", buildingProgress = 25 }
            });
            db.missions.Add(new MissionDefinition
            {
                id = "f1", jobId = "farmer", nameFa = "کشت", startNpcId = "farmer",
                objectives = new List<ObjectiveDefinition>
                {
                    new ObjectiveDefinition { type = "interact", targetId = "field", count = 1, textFa = "کاشت" }
                },
                reward = new RewardDefinition { coins = 5, xp = 5 }
            });
            return db;
        }

        public static World CreateWorld()
        {
            var w = new World();
            w.Db = CreateDb();
            w.Save = new SaveData();
            w.Wallet = new Wallet(w.Save);
            w.City = new CityProgressService(w.Db, w.Save);
            w.Missions = new MissionService(w.Db, w.Save, w.Wallet, w.City);
            return w;
        }

        /// <summary>Plays m1 fully so m2 unlocks.</summary>
        public static void CompleteM1(World w)
        {
            w.Missions.Start("m1");
            w.Missions.Notify(ObjectiveType.Collect, "tool");
            w.Missions.Notify(ObjectiveType.Talk, "npc");
        }
    }
}
