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
            DeactivateUnknownMissions();
        }

        /// <summary>
        /// A save made before a mission was renamed or removed may still mark it "active". Such a ghost run
        /// could never be finished, so it is switched off (its completed history is kept as it is).
        /// </summary>
        private void DeactivateUnknownMissions()
        {
            foreach (var p in save.missions)
            {
                if (p == null || !p.active) continue;
                if (db.FindMission(p.id) != null) continue;
                p.active = false;
                p.activeSeconds = 0f;
                p.objectiveCounts = new List<int>();
            }
        }

        public MissionDatabase Database { get { return db; } }
        public SaveData Data { get { return save; } }

        /// <summary>Longest single stretch counted as "playing" (a phone left on a table must not skew the report).</summary>
        public const float MaxRunSeconds = 1800f;
        /// <summary>A run abandoned sooner than this, without any progress, is treated as an accidental tap and not recorded.</summary>
        public const float MinMeaningfulSeconds = 10f;

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

        /// <summary>True if the mission could be started right now were nothing else running.</summary>
        public bool IsStartable(string missionId)
        {
            var def = db.FindMission(missionId);
            if (def == null) return false;
            var s = GetStatus(missionId);
            return s == MissionStatus.Available || (s == MissionStatus.Completed && def.repeatable);
        }

        public bool CanStart(string missionId)
        {
            return ActiveMission == null && IsStartable(missionId);
        }

        /// <summary>
        /// Everything this NPC can hand out right now: new stages first, then stages the child
        /// has finished and may play again (every repeat is the child's own choice).
        /// </summary>
        public List<MissionDefinition> OfferableForNpc(string npcId)
        {
            var list = new List<MissionDefinition>();
            foreach (var m in db.missions)
                if (m.startNpcId == npcId && GetStatus(m.id) == MissionStatus.Available) list.Add(m);
            foreach (var m in db.missions)
                if (m.startNpcId == npcId && m.repeatable && GetStatus(m.id) == MissionStatus.Completed) list.Add(m);
            return list;
        }

        /// <summary>Counts real play time of the active run. Call once per frame while the child is actually playing.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            if (deltaSeconds > 1f) deltaSeconds = 1f;      // ignore long hitches (app resume, scene load)
            foreach (var p in save.missions)
            {
                if (!p.active) continue;
                if (p.activeSeconds < MaxRunSeconds) p.activeSeconds += deltaSeconds;
                return;
            }
        }

        /// <summary>Gives up the current run (no penalty) and starts a different mission in one step.</summary>
        public bool SwitchTo(string missionId)
        {
            if (!IsStartable(missionId)) return false;
            AbandonActive();
            return Start(missionId);
        }

        public bool Start(string missionId)
        {
            if (!CanStart(missionId)) return false;
            var def = db.FindMission(missionId);
            var p = save.GetMission(missionId, true);
            p.active = true;
            p.activeSeconds = 0f;
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
                var def = db.FindMission(p.id);
                bool hadProgress = false;
                if (p.objectiveCounts != null)
                    foreach (var c in p.objectiveCounts) if (c > 0) hadProgress = true;
                if (def != null && (hadProgress || p.activeSeconds >= MinMeaningfulSeconds))
                    RecordAttempt(def, false, false, p.activeSeconds);
                p.active = false;
                p.activeSeconds = 0f;
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
            RecordAttempt(def, true, !first, p.activeSeconds);
            p.activeSeconds = 0f;

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

        private void RecordAttempt(MissionDefinition def, bool completed, bool replay, float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            if (seconds > MaxRunSeconds) seconds = MaxRunSeconds;
            save.attemptSeq++;
            save.attempts.Add(new AttemptRecord
            {
                seq = save.attemptSeq,
                missionId = def.id,
                jobId = def.jobId,
                seconds = (int)Math.Round(seconds),
                completed = completed,
                replay = replay
            });
            if (save.attempts.Count > SaveData.MaxAttempts)
                save.attempts.RemoveRange(0, save.attempts.Count - SaveData.MaxAttempts);

            var js = save.GetJobStats(def.jobId, true);
            js.playSeconds += seconds;
            if (!completed) js.abandoned++;
            if (completed && replay) js.replays++;
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
