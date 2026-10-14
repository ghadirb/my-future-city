using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    /// <summary>Names of the real in-game activities that are logged for the interest report.</summary>
    public static class ActivityIds
    {
        // teacher
        public const string LessonChosen = "lesson_chosen";
        public const string PresentConcept = "present_concept";
        public const string ExplainConcept = "explain_concept";
        public const string StudentCheck = "student_check";
        public const string HelpStudent = "help_student";
        public const string ManageClass = "manage_class";
        public const string FullSession = "full_session";
    }

    /// <summary>How the activity went. Kept apart from rewards and from the interest weights on purpose.</summary>
    public static class ActivityResult
    {
        public const int None = 0;
        /// <summary>The very first choice was the best one.</summary>
        public const int FirstTry = 1;
        /// <summary>Finished, but needed the alternative explanation (a hint) first.</summary>
        public const int NeededHelp = 2;
    }

    [Serializable]
    public class ActivityEvent
    {
        public int seq;
        /// <summary>Number of the run this happened in (SaveData.attemptSeq at that moment). Used to ignore duplicates.</summary>
        public int runSeq;
        public string jobId;
        public string missionId;
        public string activityId;
        /// <summary>What was chosen or done (a lesson id, a step id...). Never free text typed by the child.</summary>
        public string detail;
        public int result;
    }

    /// <summary>Running totals so the report stays exact after old events are trimmed.</summary>
    [Serializable]
    public class ActivityCounter
    {
        public string jobId;
        public string activityId;
        public int count;
        public int firstTry;
        public int neededHelp;
    }

    [Serializable]
    public class ActivityData
    {
        public int seq;
        public List<ActivityEvent> events = new List<ActivityEvent>();
        public List<ActivityCounter> counters = new List<ActivityCounter>();

        public ActivityCounter GetCounter(string jobId, string activityId, bool create)
        {
            if (counters == null) counters = new List<ActivityCounter>();
            for (int i = 0; i < counters.Count; i++)
                if (counters[i] != null && counters[i].jobId == jobId && counters[i].activityId == activityId) return counters[i];
            if (!create) return null;
            var c = new ActivityCounter { jobId = jobId, activityId = activityId };
            counters.Add(c);
            return c;
        }
    }

    /// <summary>
    /// Independent module that records what the child really DID in a profession (not coins, not xp).
    /// Everything is stored locally inside the save file; nothing is sent anywhere.
    /// </summary>
    public sealed class ActivityLog
    {
        public const int MaxEvents = 400;

        private readonly SaveData save;

        public ActivityLog(SaveData save)
        {
            if (save == null) throw new ArgumentNullException("save");
            this.save = save;
            if (save.activity == null) save.activity = new ActivityData();
        }

        public event Action Recorded;

        /// <summary>
        /// Records one activity. Returns false (and records nothing) when the same activity was already
        /// logged in the same run of the same mission, so a repeated tap cannot inflate the report.
        /// </summary>
        public bool Record(string jobId, string missionId, string activityId, string detail, int result)
        {
            if (string.IsNullOrEmpty(jobId) || string.IsNullOrEmpty(activityId)) return false;
            if (missionId == null) missionId = "";
            if (detail == null) detail = "";

            var a = save.activity;
            int run = save.attemptSeq;
            for (int i = a.events.Count - 1; i >= 0; i--)
            {
                var e = a.events[i];
                if (e == null) continue;
                if (e.runSeq != run) break;
                if (e.missionId == missionId && e.activityId == activityId && e.detail == detail) return false;
            }

            a.seq++;
            a.events.Add(new ActivityEvent
            {
                seq = a.seq, runSeq = run, jobId = jobId, missionId = missionId,
                activityId = activityId, detail = detail, result = result
            });
            if (a.events.Count > MaxEvents) a.events.RemoveRange(0, a.events.Count - MaxEvents);

            var c = a.GetCounter(jobId, activityId, true);
            c.count++;
            if (result == ActivityResult.FirstTry) c.firstTry++;
            else if (result == ActivityResult.NeededHelp) c.neededHelp++;

            var h = Recorded;
            if (h != null) h();
            return true;
        }

        public int Count(string jobId, string activityId)
        {
            var c = save.activity.GetCounter(jobId, activityId, false);
            return c == null ? 0 : c.count;
        }

        /// <summary>How many times a given detail (e.g. a lesson id) was chosen for an activity, from the stored events.</summary>
        public int CountDetail(string jobId, string activityId, string detail)
        {
            int n = 0;
            foreach (var e in save.activity.events)
                if (e != null && e.jobId == jobId && e.activityId == activityId && e.detail == detail) n++;
            return n;
        }
    }
}
