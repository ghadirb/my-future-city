using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Loads the real-job activities (Resources/Data/activities.json) and validates them against the missions.</summary>
    public static class ActivityDataLoader
    {
        public const string ResourcePath = "Data/activities";

        public static ActivityDatabase Load(MissionDatabase missions)
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError("[MyFutureCity] Missing Resources/" + ResourcePath + ".json");
                return new ActivityDatabase();
            }

            ActivityDatabase db = null;
            try { db = JsonUtility.FromJson<ActivityDatabase>(asset.text); }
            catch (System.Exception e) { Debug.LogError("[MyFutureCity] activities.json could not be parsed: " + e.Message); }
            if (db == null) db = new ActivityDatabase();

            foreach (var err in ActivityDatabaseValidator.Validate(db, missions))
                Debug.LogError("[MyFutureCity] Activity data: " + err);
            return db;
        }
    }
}
