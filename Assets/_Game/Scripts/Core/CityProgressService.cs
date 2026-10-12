using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    /// <summary>Tracks how restored each building is. State is derived from persisted progress.</summary>
    public class CityProgressService
    {
        public const int MaxProgress = 200;
        public const int RestoredThreshold = 100;
        public const int ActiveThreshold = 150;

        private readonly MissionDatabase db;
        private readonly SaveData save;

        public event Action<string, BuildingState, BuildingState> BuildingStateChanged;
        public event Action Changed;

        public CityProgressService(MissionDatabase db, SaveData save)
        {
            if (db == null) throw new ArgumentNullException("db");
            if (save == null) throw new ArgumentNullException("save");
            this.db = db;
            this.save = save;
        }

        public IList<BuildingDefinition> Buildings { get { return db.buildings; } }

        public static BuildingState StateFor(int progress)
        {
            if (progress >= ActiveThreshold) return BuildingState.Active;
            if (progress >= RestoredThreshold) return BuildingState.Restored;
            if (progress > 0) return BuildingState.Renovating;
            return BuildingState.Abandoned;
        }

        public int GetProgress(string buildingId)
        {
            var b = save.GetBuilding(buildingId, false);
            return b == null ? 0 : b.progress;
        }

        public BuildingState GetState(string buildingId)
        {
            return StateFor(GetProgress(buildingId));
        }

        public bool AddProgress(string buildingId, int amount)
        {
            if (string.IsNullOrEmpty(buildingId) || amount <= 0) return false;
            if (db.FindBuilding(buildingId) == null) return false;

            var b = save.GetBuilding(buildingId, true);
            var before = StateFor(b.progress);
            int next = b.progress + amount;
            if (next > MaxProgress) next = MaxProgress;
            if (next == b.progress) return false;
            b.progress = next;
            var after = StateFor(b.progress);

            if (before != after)
            {
                var h = BuildingStateChanged;
                if (h != null) h(buildingId, before, after);
            }
            var c = Changed;
            if (c != null) c();
            return true;
        }

        /// <summary>0..1 average restoration over all known buildings (each capped at "restored").</summary>
        public float OverallPercent()
        {
            if (db.buildings == null || db.buildings.Count == 0) return 0f;
            float sum = 0f;
            foreach (var def in db.buildings)
            {
                int p = GetProgress(def.id);
                if (p > RestoredThreshold) p = RestoredThreshold;
                sum += (float)p / RestoredThreshold;
            }
            return sum / db.buildings.Count;
        }
    }
}
