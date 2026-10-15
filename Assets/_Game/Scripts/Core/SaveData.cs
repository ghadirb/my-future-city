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

    /// <summary>Exact running totals of the real job activities per job (interest log, separate from rewards).</summary>
    [Serializable]
    public class ActivityJobStats
    {
        public string jobId;
        public int started;        // scenes begun
        public int completed;      // scenes finished
        public int stepsDone;      // decisions accepted
        public int firstTry;       // decisions accepted without a mistake or a hint
        public int wrongChoices;
        public int hints;
        public int optionalRuns;   // scenes finished while repeating an already completed stage (the child's own choice)
        public int abandoned;      // scenes left unfinished
    }

    /// <summary>How often one scene (one spot of one activity) was completed. Proof that the activity was really done.</summary>
    [Serializable]
    public class ActivityRecord
    {
        public string missionId;
        public string targetId;
        public string jobId;
        public int scene;
        public int completions;
    }

    /// <summary>One decision of the child, kept so the report is built from real choices (newest 400 only).</summary>
    [Serializable]
    public class ActivityChoice
    {
        public int seq;
        public string missionId;
        public string targetId;
        public int scene;
        public int step;
        public int option;
        public bool correct;
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
        public const int CurrentVersion = 3;
        public const int MaxAttempts = 300;
        public const int MaxActivityChoices = 400;

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
        public int activitySeq;
        public List<ActivityJobStats> activityStats = new List<ActivityJobStats>();
        public List<ActivityRecord> activityRecords = new List<ActivityRecord>();
        public List<ActivityChoice> activityChoices = new List<ActivityChoice>();
        /// <summary>Free choices that shape later activities, as "key=value" (one value per key), e.g. "topic=science".</summary>
        public List<string> choiceTags = new List<string>();

        public void SetChoiceTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            int eq = tag.IndexOf('=');
            if (eq <= 0) return;
            if (choiceTags == null) choiceTags = new List<string>();
            string key = tag.Substring(0, eq + 1);
            choiceTags.RemoveAll(t => t != null && t.StartsWith(key));
            choiceTags.Add(tag);
        }

        public ActivityJobStats GetActivityStats(string jobId, bool create)
        {
            if (activityStats == null) activityStats = new List<ActivityJobStats>();
            for (int i = 0; i < activityStats.Count; i++)
                if (activityStats[i] != null && activityStats[i].jobId == jobId) return activityStats[i];
            if (!create) return null;
            var a = new ActivityJobStats { jobId = jobId };
            activityStats.Add(a);
            return a;
        }

        public ActivityRecord GetActivityRecord(string missionId, string targetId, int scene, bool create)
        {
            if (activityRecords == null) activityRecords = new List<ActivityRecord>();
            for (int i = 0; i < activityRecords.Count; i++)
            {
                var r = activityRecords[i];
                if (r != null && r.missionId == missionId && r.targetId == targetId && r.scene == scene) return r;
            }
            if (!create) return null;
            var n = new ActivityRecord { missionId = missionId, targetId = targetId, scene = scene };
            activityRecords.Add(n);
            return n;
        }

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
            if (activityStats == null) activityStats = new List<ActivityJobStats>();
            if (activityRecords == null) activityRecords = new List<ActivityRecord>();
            if (activityChoices == null) activityChoices = new List<ActivityChoice>();
            if (activitySeq < 0) activitySeq = 0;
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

            activityStats.RemoveAll(a => a == null || string.IsNullOrEmpty(a.jobId));
            var seenA = new HashSet<string>();
            activityStats.RemoveAll(a => !seenA.Add(a.jobId));
            foreach (var a in activityStats)
            {
                if (a.started < 0) a.started = 0;
                if (a.completed < 0) a.completed = 0;
                if (a.stepsDone < 0) a.stepsDone = 0;
                if (a.firstTry < 0) a.firstTry = 0;
                if (a.firstTry > a.stepsDone) a.firstTry = a.stepsDone;
                if (a.wrongChoices < 0) a.wrongChoices = 0;
                if (a.hints < 0) a.hints = 0;
                if (a.optionalRuns < 0) a.optionalRuns = 0;
                if (a.abandoned < 0) a.abandoned = 0;
            }
            activityRecords.RemoveAll(r => r == null || string.IsNullOrEmpty(r.missionId) || string.IsNullOrEmpty(r.targetId));
            var seenR = new HashSet<string>();
            activityRecords.RemoveAll(r => !seenR.Add(r.missionId + "|" + r.targetId + "|" + r.scene));
            foreach (var r in activityRecords) { if (r.completions < 0) r.completions = 0; if (r.scene < 0) r.scene = 0; }
            if (choiceTags == null) choiceTags = new List<string>();
            choiceTags.RemoveAll(t => string.IsNullOrEmpty(t) || t.IndexOf('=') <= 0);
            activityChoices.RemoveAll(c => c == null || string.IsNullOrEmpty(c.missionId));
            foreach (var c in activityChoices) { if (c.seq > activitySeq) activitySeq = c.seq; }
            if (activityChoices.Count > MaxActivityChoices) activityChoices.RemoveRange(0, activityChoices.Count - MaxActivityChoices);

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
