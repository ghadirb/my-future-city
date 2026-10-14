using System.Collections.Generic;
using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>
    /// The one place that wires the pure-C# game services to persistent storage.
    /// (Deliberately a single holder instead of many singletons.)
    /// </summary>
    public sealed class GameContext
    {
        private static GameContext instance;

        public static GameContext Instance
        {
            get
            {
                if (instance == null) instance = Create(SaveStore.Load());
                return instance;
            }
        }

        public SaveData Save { get; private set; }
        public MissionDatabase Database { get; private set; }
        public Wallet Wallet { get; private set; }
        public CityProgressService City { get; private set; }
        public MissionService Missions { get; private set; }
        public Recommender Recommender { get; private set; }
        public LessonDatabase Lessons { get; private set; }
        public ActivityLog Activity { get; private set; }
        public TeachingService Teaching { get; private set; }
        public readonly HashSet<string> DismissedSuggestions = new HashSet<string>();

        public bool HasProgress { get { return !string.IsNullOrEmpty(Save.characterId); } }

        private static GameContext Create(SaveData save)
        {
            var ctx = new GameContext();
            ctx.Save = save;
            ctx.Database = MissionDataLoader.Load();
            ctx.Wallet = new Wallet(save);
            ctx.City = new CityProgressService(ctx.Database, save);
            ctx.Missions = new MissionService(ctx.Database, save, ctx.Wallet, ctx.City);
            ctx.Recommender = new Recommender(ctx.Database, ctx.Missions);
            ctx.Lessons = LessonDataLoader.Load();
            ctx.Activity = new ActivityLog(save);
            ctx.Teaching = new TeachingService(ctx.Lessons, save, ctx.Activity, new System.Random());

            // Persist after meaningful events only (never every frame).
            ctx.Missions.Started += m => ctx.SaveNow();
            ctx.Missions.ObjectiveAdvanced += (m, i) => ctx.SaveNow();
            ctx.Missions.Completed += c => ctx.SaveNow();
            ctx.Activity.Recorded += () => ctx.SaveNow();
            return ctx;
        }

        public static void StartNewGame(string characterId)
        {
            SaveStore.Delete();
            var save = new SaveData { characterId = characterId };
            instance = Create(save);
            instance.SaveNow();
        }

        public void SaveNow()
        {
            SaveStore.Write(Save);
        }
    }
}
