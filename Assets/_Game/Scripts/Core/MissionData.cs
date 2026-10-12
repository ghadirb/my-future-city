using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    public enum ObjectiveType { Talk, Collect, Interact }
    public enum MissionStatus { Locked, Available, Active, Completed }
    public enum BuildingState { Abandoned, Renovating, Restored, Active }

    [Serializable]
    public class JobDefinition
    {
        public string id;
        public string nameFa;
        public string descriptionFa;
        public string badgeId;
        public string badgeNameFa;
    }

    [Serializable]
    public class ObjectiveDefinition
    {
        // "talk" | "collect" | "interact" (string so JSON stays human-editable)
        public string type;
        public string targetId;
        public int count = 1;
        public string textFa;
        // Optional: every unit of progress on this objective also advances a building.
        public string buildingId;
        public int buildingProgress;

        public bool TryParseType(out ObjectiveType result)
        {
            return Enum.TryParse(type ?? "", true, out result);
        }

        public ObjectiveType ParsedType()
        {
            ObjectiveType t;
            return TryParseType(out t) ? t : ObjectiveType.Interact;
        }

        public int RequiredCount { get { return count < 1 ? 1 : count; } }
    }

    [Serializable]
    public class RewardDefinition
    {
        public int coins;
        public int xp;
        public string badgeId;
        public string buildingId;
        public int buildingProgress;
    }

    [Serializable]
    public class MissionDefinition
    {
        public string id;
        public string jobId;
        public string nameFa;
        public string descriptionFa;
        public string startNpcId;
        public string requiresMissionId;
        public List<ObjectiveDefinition> objectives = new List<ObjectiveDefinition>();
        public RewardDefinition reward = new RewardDefinition();
        public bool repeatable;
        public int repeatCoins;
        public string offerTextFa;
        public string completeTextFa;
    }

    [Serializable]
    public class BuildingDefinition
    {
        public string id;
        public string nameFa;
        public string descriptionFa;
    }

    [Serializable]
    public class MissionDatabase
    {
        public List<JobDefinition> jobs = new List<JobDefinition>();
        public List<MissionDefinition> missions = new List<MissionDefinition>();
        public List<BuildingDefinition> buildings = new List<BuildingDefinition>();

        public MissionDefinition FindMission(string id)
        {
            if (missions == null || id == null) return null;
            for (int i = 0; i < missions.Count; i++)
                if (missions[i] != null && missions[i].id == id) return missions[i];
            return null;
        }

        public JobDefinition FindJob(string id)
        {
            if (jobs == null || id == null) return null;
            for (int i = 0; i < jobs.Count; i++)
                if (jobs[i] != null && jobs[i].id == id) return jobs[i];
            return null;
        }

        public BuildingDefinition FindBuilding(string id)
        {
            if (buildings == null || id == null) return null;
            for (int i = 0; i < buildings.Count; i++)
                if (buildings[i] != null && buildings[i].id == id) return buildings[i];
            return null;
        }
    }

    /// <summary>Static checks that catch broken mission data before it reaches a player.</summary>
    public static class MissionDatabaseValidator
    {
        public static List<string> Validate(MissionDatabase db)
        {
            var errors = new List<string>();
            if (db == null) { errors.Add("Mission database is null."); return errors; }

            var jobIds = new HashSet<string>();
            foreach (var j in db.jobs)
            {
                if (j == null || string.IsNullOrEmpty(j.id)) { errors.Add("A job has no id."); continue; }
                if (!jobIds.Add(j.id)) errors.Add("Duplicate job id: " + j.id);
            }

            var buildingIds = new HashSet<string>();
            foreach (var b in db.buildings)
            {
                if (b == null || string.IsNullOrEmpty(b.id)) { errors.Add("A building has no id."); continue; }
                if (!buildingIds.Add(b.id)) errors.Add("Duplicate building id: " + b.id);
            }

            var missionIds = new HashSet<string>();
            foreach (var m in db.missions)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) { errors.Add("A mission has no id."); continue; }
                if (!missionIds.Add(m.id)) errors.Add("Duplicate mission id: " + m.id);
            }

            foreach (var m in db.missions)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) continue;
                string p = "Mission '" + m.id + "': ";
                if (string.IsNullOrEmpty(m.nameFa)) errors.Add(p + "missing nameFa.");
                if (!jobIds.Contains(m.jobId ?? "")) errors.Add(p + "unknown jobId '" + m.jobId + "'.");
                if (!string.IsNullOrEmpty(m.requiresMissionId))
                {
                    if (!missionIds.Contains(m.requiresMissionId)) errors.Add(p + "unknown requiresMissionId '" + m.requiresMissionId + "'.");
                    if (m.requiresMissionId == m.id) errors.Add(p + "requires itself.");
                }
                if (m.objectives == null || m.objectives.Count == 0) errors.Add(p + "has no objectives.");
                else
                {
                    for (int i = 0; i < m.objectives.Count; i++)
                    {
                        var o = m.objectives[i];
                        string op = p + "objective " + i + ": ";
                        if (o == null) { errors.Add(op + "is null."); continue; }
                        ObjectiveType t;
                        if (!o.TryParseType(out t)) errors.Add(op + "unknown type '" + o.type + "'.");
                        if (string.IsNullOrEmpty(o.targetId)) errors.Add(op + "missing targetId.");
                        if (o.count < 1) errors.Add(op + "count must be >= 1.");
                        if (string.IsNullOrEmpty(o.textFa)) errors.Add(op + "missing textFa.");
                        if (!string.IsNullOrEmpty(o.buildingId) && !buildingIds.Contains(o.buildingId))
                            errors.Add(op + "unknown buildingId '" + o.buildingId + "'.");
                    }
                }
                if (m.reward == null) errors.Add(p + "missing reward.");
                else
                {
                    if (m.reward.coins < 0 || m.reward.xp < 0) errors.Add(p + "negative reward.");
                    if (!string.IsNullOrEmpty(m.reward.buildingId) && !buildingIds.Contains(m.reward.buildingId))
                        errors.Add(p + "reward references unknown building '" + m.reward.buildingId + "'.");
                }
                if (m.repeatable && m.repeatCoins < 0) errors.Add(p + "negative repeatCoins.");
            }

            // Cycle check on requiresMissionId chains.
            foreach (var m in db.missions)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) continue;
                var seen = new HashSet<string>();
                var cur = m;
                while (cur != null && !string.IsNullOrEmpty(cur.requiresMissionId))
                {
                    if (!seen.Add(cur.id)) { errors.Add("Mission requirement cycle through '" + m.id + "'."); break; }
                    cur = db.FindMission(cur.requiresMissionId);
                }
            }
            return errors;
        }
    }
}
