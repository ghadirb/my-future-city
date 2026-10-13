using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    [Serializable]
    public class MissionProgress
    {
        public string id;
        public bool active;
        public int timesCompleted;
        public List<int> objectiveCounts = new List<int>();
        /// <summary>Seconds of real play spent on the CURRENT run (reset on every start).</summary>
        public float activeSeconds;
    }

    /// <summary>One finished or given-up run of a mission. The raw material for the interest report.</summary>
    [Serializable]
    public class AttemptRecord
    {
        public int seq;
        public string missionId;
        public string jobId;
        public int seconds;
        public bool completed;
        /// <summary>True when the mission had already been completed before (a voluntary repeat).</summary>
        public bool replay;
    }

    /// <summary>Running totals per job so the report stays exact even after old attempts are trimmed.</summary>
    [Serializable]
    public class JobStats
    {
        public string jobId;
        public float playSeconds;
        public int abandoned;
        public int replays;
    }

    [Serializable]
    public class BuildingProgress
    {
        public string id;
        public int progress;
    }

    [Serializable]
    public class SettingsData
    {
        public float cameraSensitivity = 1f;
        public int targetFps = 30;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;
        public const int MaxAttempts = 300;

        public int version = CurrentVersion;
        public string characterId = "";
        public string lastScene = "";
        public bool tutorialDone;
        public int coins;
        public int xp;
        public List<MissionProgress> missions = new List<MissionProgress>();
        public List<string> badges = new List<string>();
        public List<BuildingProgress> buildings = new List<BuildingProgress>();
        public SettingsData settings = new SettingsData();
        public int attemptSeq;
        public List<AttemptRecord> attempts = new List<AttemptRecord>();
        public List<JobStats> jobStats = new List<JobStats>();

        public JobStats GetJobStats(string jobId, bool create)
        {
            if (jobStats == null) jobStats = new List<JobStats>();
            for (int i = 0; i < jobStats.Count; i++)
                if (jobStats[i] != null && jobStats[i].jobId == jobId) return jobStats[i];
            if (!create) return null;
            var j = new JobStats { jobId = jobId };
            jobStats.Add(j);
            return j;
        }

        public MissionProgress GetMission(string id, bool create)
        {
            if (missions == null) missions = new List<MissionProgress>();
            for (int i = 0; i < missions.Count; i++)
                if (missions[i] != null && missions[i].id == id) return missions[i];
            if (!create) return null;
            var p = new MissionProgress { id = id };
            missions.Add(p);
            return p;
        }

        public BuildingProgress GetBuilding(string id, bool create)
        {
            if (buildings == null) buildings = new List<BuildingProgress>();
            for (int i = 0; i < buildings.Count; i++)
                if (buildings[i] != null && buildings[i].id == id) return buildings[i];
            if (!create) return null;
            var b = new BuildingProgress { id = id };
            buildings.Add(b);
            return b;
        }

        /// <summary>
        /// Repairs partial / damaged data so the game never has to crash on a bad save.
        /// Safe to call repeatedly.
        /// </summary>
        public void Sanitize()
        {
            if (version < CurrentVersion) version = CurrentVersion;   // v1 saves simply gain empty report data
            if (characterId == null) characterId = "";
            if (lastScene == null) lastScene = "";
            if (coins < 0) coins = 0;
            if (xp < 0) xp = 0;
            if (missions == null) missions = new List<MissionProgress>();
            if (badges == null) badges = new List<string>();
            if (buildings == null) buildings = new List<BuildingProgress>();
            if (settings == null) settings = new SettingsData();
            if (attempts == null) attempts = new List<AttemptRecord>();
            if (jobStats == null) jobStats = new List<JobStats>();
            if (attemptSeq < 0) attemptSeq = 0;
            if (settings.cameraSensitivity < 0.2f || settings.cameraSensitivity > 3f) settings.cameraSensitivity = 1f;
            if (settings.targetFps != 30 && settings.targetFps != 60) settings.targetFps = 30;

            missions.RemoveAll(m => m == null || string.IsNullOrEmpty(m.id));
            buildings.RemoveAll(b => b == null || string.IsNullOrEmpty(b.id));
            badges.RemoveAll(string.IsNullOrEmpty);

            // Drop duplicates (keep the first entry).
            var seenM = new HashSet<string>();
            missions.RemoveAll(m => !seenM.Add(m.id));
            var seenB = new HashSet<string>();
            buildings.RemoveAll(b => !seenB.Add(b.id));
            var seenBadge = new HashSet<string>();
            badges.RemoveAll(b => !seenBadge.Add(b));

            attempts.RemoveAll(a => a == null || string.IsNullOrEmpty(a.missionId) || string.IsNullOrEmpty(a.jobId));
            foreach (var a in attempts) { if (a.seconds < 0) a.seconds = 0; if (a.seq > attemptSeq) attemptSeq = a.seq; }
            if (attempts.Count > MaxAttempts) attempts.RemoveRange(0, attempts.Count - MaxAttempts);
            jobStats.RemoveAll(j => j == null || string.IsNullOrEmpty(j.jobId));
            var seenJ = new HashSet<string>();
            jobStats.RemoveAll(j => !seenJ.Add(j.jobId));
            foreach (var j in jobStats)
            {
                if (j.playSeconds < 0f || float.IsNaN(j.playSeconds)) j.playSeconds = 0f;
                if (j.abandoned < 0) j.abandoned = 0;
                if (j.replays < 0) j.replays = 0;
            }

            foreach (var m in missions)
            {
                if (m.activeSeconds < 0f || float.IsNaN(m.activeSeconds)) m.activeSeconds = 0f;
                if (m.objectiveCounts == null) m.objectiveCounts = new List<int>();
                if (m.timesCompleted < 0) m.timesCompleted = 0;
                for (int i = 0; i < m.objectiveCounts.Count; i++)
                    if (m.objectiveCounts[i] < 0) m.objectiveCounts[i] = 0;
            }
            foreach (var b in buildings)
            {
                if (b.progress < 0) b.progress = 0;
                if (b.progress > CityProgressService.MaxProgress) b.progress = CityProgressService.MaxProgress;
            }

            // Only one mission may be active at a time.
            bool foundActive = false;
            foreach (var m in missions)
            {
                if (!m.active) continue;
                if (foundActive) m.active = false;
                foundActive = true;
            }
        }
    }
}
