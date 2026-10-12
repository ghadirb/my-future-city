using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    public static class MissionDataLoader
    {
        public const string ResourcePath = "Data/missions";

        public static MissionDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError("[MyFutureCity] Missing Resources/" + ResourcePath + ".json");
                return new MissionDatabase();
            }

            MissionDatabase db = null;
            try { db = JsonUtility.FromJson<MissionDatabase>(asset.text); }
            catch (System.Exception e) { Debug.LogError("[MyFutureCity] missions.json could not be parsed: " + e.Message); }
            if (db == null) db = new MissionDatabase();

            foreach (var err in MissionDatabaseValidator.Validate(db))
                Debug.LogError("[MyFutureCity] Mission data: " + err);
            return db;
        }
    }
}
