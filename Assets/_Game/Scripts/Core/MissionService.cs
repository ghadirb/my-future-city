using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    public class MissionCompletion
    {
        public MissionDefinition Mission;
        public bool FirstTime;
        public int Coins;
        public int Xp;
        public string BadgeId;
        public string BuildingId;
        public int BuildingProgress;
    }

    /// <summary>
    /// Data-driven mission logic: start, ordered objectives, rewards, repeat rules.
    /// Pure C# so it can be unit-tested without Unity.
    /// </summary>
    public class MissionService
    {
        private readonly MissionDatabase db;
        private readonly SaveData save;
        private readonly Wallet wallet;
        private readonly CityProgressService city;

        public event Action<MissionDefinition> Started;
        public event Action<MissionDefinition, int> ObjectiveAdvanced;
        public event Action<MissionCompletion> Completed;
        public event Action Changed;

        public MissionService(MissionDatabase db, SaveData save, Wallet wallet, CityProgressService city)
        {
            if (db == null) throw new ArgumentNullException("db");
            if (save == null) throw new ArgumentNullException("save");
            if (wallet == null) throw new ArgumentNullException("wallet");
            if (city == null) throw new ArgumentNullException("city");
            this.db = db;
            this.save = save;
            this.wallet = wallet;
            this.city = city;
        }

        public MissionDatabase Database { get { return db; } }

        public MissionStatus GetStatus(string missionId)
        {
            var def = db.FindMission(missionId);
            if (def == null) return MissionStatus.Locked;
            var p = save.GetMission(missionId, false);
            if (p != null && p.active) return MissionStatus.Active;
            if (p != null && p.timesCompleted > 0) return MissionStatus.Completed;
            return RequirementMet(def) ? MissionStatus.Available : MissionStatus.Locked;
        }

        private bool RequirementMet(MissionDefinition def)
        {
            if (string.IsNullOrEmpty(def.requiresMissionId)) return true;
            var r = save.GetMission(def.requiresMissionId, false);
            return r != null && r.timesCompleted > 0;
        }

        public int TimesCompleted(string missionId)
        {
            var p = save.GetMission(missionId, false);
            return p == null ? 0 : p.timesCompleted;
        }

        public MissionDefinition ActiveMission
        {
            get
            {
                foreach (var p in save.missions)
                    if (p.active) return db.FindMission(p.id);
                return null;
            }
        }

        public bool CanStart(string missionId)
        {
            var def = db.FindMission(missionId);
            if (def == null || ActiveMission != null) return false;
            var s = GetStatus(missionId);
            return s == MissionStatus.Available || (s == MissionStatus.Completed && def.repeatable);
        }

        public bool Start(string missionId)
        {
            if (!CanStart(missionId)) return false;
            var def = db.FindMission(missionId);
            var p = save.GetMission(missionId, true);
            p.active = true;
            p.objectiveCounts = new List<int>();
            for (int i = 0; i < def.objectives.Count; i++) p.objectiveCounts.Add(0);
            var h = Started;
            if (h != null) h(def);
            RaiseChanged();
            return true;
        }

        /// <summary>Giving up has no penalty (child-friendly): progress of the run is simply cleared.</summary>
        public bool AbandonActive()
        {
            foreach (var p in save.missions)
            {
                if (!p.active) continue;
                p.active = false;
                p.objectiveCounts = new List<int>();
                RaiseChanged();
                return true;
            }
            return false;
        }

        public int GetObjectiveCount(string missionId, int index)
        {
            var p = save.GetMission(missionId, false);
            if (p == null || !p.active || p.objectiveCounts == null) return 0;
            return index >= 0 && index < p.objectiveCounts.Count ? p.objectiveCounts[index] : 0;
        }

        /// <summary>Progress of the first Collect/Interact objective that targets <paramref name="targetId"/> in an ACTIVE run.</summary>
        public int GetCountByTarget(string missionId, string targetId)
        {
            var def = db.FindMission(missionId);
            if (def == null) return 0;
            for (int i = 0; i < def.objectives.Count; i++)
                if (def.objectives[i].targetId == targetId) return GetObjectiveCount(missionId, i);
            return 0;
        }

        /// <summary>Index of the first unfinished objective of the active mission, or -1.</summary>
        public int CurrentObjectiveIndex
        {
            get
            {
                var def = ActiveMission;
                return def == null ? -1 : FirstUnfinished(def, save.GetMission(def.id, false));
            }
        }

        public ObjectiveDefinition CurrentObjective
        {
            get
            {
                var def = ActiveMission;
                int i = CurrentObjectiveIndex;
                return (def == null || i < 0) ? null : def.objectives[i];
            }
        }

        private static int FirstUnfinished(MissionDefinition def, MissionProgress p)
        {
            if (def == null || p == null) return -1;
            for (int i = 0; i < def.objectives.Count; i++)
            {
                int c = (p.objectiveCounts != null && i < p.objectiveCounts.Count) ? p.objectiveCounts[i] : 0;
                if (c < def.objectives[i].RequiredCount) return i;
            }
            return -1;
        }

        /// <summary>True if the active mission is currently waiting for exactly this kind of action.</summary>
        public bool IsObjectiveCurrent(ObjectiveType type, string targetId)
        {
            var o = CurrentObjective;
            return o != null && o.ParsedType() == type && o.targetId == targetId;
        }

        /// <summary>Report a gameplay action. Returns true if it advanced the active mission.</summary>
        public bool Notify(ObjectiveType type, string targetId, int amount)
        {
            if (amount <= 0) return false;
            var def = ActiveMission;
            if (def == null) return false;
            var p = save.GetMission(def.id, false);
            int idx = FirstUnfinished(def, p);
            if (idx < 0) return false;
            var obj = def.objectives[idx];
            if (obj.ParsedType() != type || obj.targetId != targetId) return false;

            while (p.objectiveCounts.Count < def.objectives.Count) p.objectiveCounts.Add(0);
            int before = p.objectiveCounts[idx];
            int after = Math.Min(before + amount, obj.RequiredCount);
            int applied = after - before;
            if (applied <= 0) return false;
            p.objectiveCounts[idx] = after;

            if (!string.IsNullOrEmpty(obj.buildingId) && obj.buildingProgress > 0)
                city.AddProgress(obj.buildingId, obj.buildingProgress * applied);

            var h = ObjectiveAdvanced;
            if (h != null) h(def, idx);

            if (FirstUnfinished(def, p) < 0) Complete(def, p);
            RaiseChanged();
            return true;
        }

        public bool Notify(ObjectiveType type, string targetId)
        {
            return Notify(type, targetId, 1);
        }

        private void Complete(MissionDefinition def, MissionProgress p)
        {
            bool first = p.timesCompleted == 0;
            p.active = false;
            p.timesCompleted++;

            var result = new MissionCompletion { Mission = def, FirstTime = first };
            var r = def.reward ?? new RewardDefinition();
            if (first)
            {
                result.Coins = r.coins;
                result.Xp = r.xp;
                result.BadgeId = r.badgeId;
                result.BuildingId = r.buildingId;
                result.BuildingProgress = r.buildingProgress;
            }
            else if (def.repeatable)
            {
                result.Coins = def.repeatCoins;   // repeat runs never repeat xp, badges or city progress
            }

            wallet.Add(result.Coins, result.Xp);
            if (!string.IsNullOrEmpty(result.BadgeId) && !save.badges.Contains(result.BadgeId))
                save.badges.Add(result.BadgeId);
            if (!string.IsNullOrEmpty(result.BuildingId) && result.BuildingProgress > 0)
                city.AddProgress(result.BuildingId, result.BuildingProgress);

            var h = Completed;
            if (h != null) h(result);
        }

        public int CompletedCountForJob(string jobId)
        {
            int n = 0;
            foreach (var m in db.missions)
                if (m.jobId == jobId && TimesCompleted(m.id) > 0) n++;
            return n;
        }

        public int MissionCountForJob(string jobId)
        {
            int n = 0;
            foreach (var m in db.missions) if (m.jobId == jobId) n++;
            return n;
        }

        private void RaiseChanged()
        {
            var h = Changed;
            if (h != null) h();
        }
    }
}
