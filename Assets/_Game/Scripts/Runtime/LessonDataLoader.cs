using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Loads the teaching lessons (Resources/Data/lessons.json) and reports data mistakes loudly.</summary>
    public static class LessonDataLoader
    {
        public const string ResourcePath = "Data/lessons";

        public static LessonDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError("[MyFutureCity] Missing Resources/" + ResourcePath + ".json");
                return new LessonDatabase();
            }

            LessonDatabase db = null;
            try { db = JsonUtility.FromJson<LessonDatabase>(asset.text); }
            catch (System.Exception e) { Debug.LogError("[MyFutureCity] lessons.json could not be parsed: " + e.Message); }
            if (db == null) db = new LessonDatabase();

            foreach (var err in LessonDatabaseValidator.Validate(db))
                Debug.LogError("[MyFutureCity] Lesson data: " + err);
            return db;
        }
    }
}
