using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    /// <summary>
    /// Everything the teacher profession needs that is not drawing or input: which lesson is chosen,
    /// which teaching decision comes next, and writing what the child really did to the activity log.
    /// Pure C#, so it is unit-tested without Unity.
    /// </summary>
    public sealed class TeachingService
    {
        public const string JobId = "teacher";

        private readonly LessonDatabase db;
        private readonly SaveData save;
        private readonly ActivityLog log;
        private readonly Random rng;

        public TeachingService(LessonDatabase db, SaveData save, ActivityLog log, Random rng)
        {
            if (db == null) throw new ArgumentNullException("db");
            if (save == null) throw new ArgumentNullException("save");
            if (log == null) throw new ArgumentNullException("log");
            this.db = db;
            this.save = save;
            this.log = log;
            this.rng = rng ?? new Random();
        }

        public LessonDatabase Database { get { return db; } }
        public ActivityLog Log { get { return log; } }
        public List<LessonDefinition> Lessons { get { return db.lessons; } }

        /// <summary>The lesson the child picked, or null when no (valid) lesson has been chosen yet.</summary>
        public LessonDefinition CurrentLesson
        {
            get { return db.FindLesson(save.teachingLessonId); }
        }

        /// <summary>Picks the lesson to teach and logs the choice (the first real decision of the profession).</summary>
        public bool ChooseLesson(string lessonId, string missionId)
        {
            var lesson = db.FindLesson(lessonId);
            if (lesson == null) return false;
            save.teachingLessonId = lesson.id;
            log.Record(JobId, missionId, ActivityIds.LessonChosen, lesson.id, ActivityResult.None);
            return true;
        }

        public static string ActivityFor(string kind)
        {
            switch (kind)
            {
                case StepKinds.Present: return ActivityIds.PresentConcept;
                case StepKinds.Explain: return ActivityIds.ExplainConcept;
                case StepKinds.Check: return ActivityIds.StudentCheck;
                case StepKinds.Help: return ActivityIds.HelpStudent;
                case StepKinds.Manage: return ActivityIds.ManageClass;
                default: return "";
            }
        }

        /// <summary>
        /// The next teaching decision of a kind for the chosen lesson. Successive calls rotate through the
        /// available steps (by how many were already done), so replays do not always show the same one.
        /// Returns null if no lesson is chosen (manage steps are shared and need no lesson).
        /// </summary>
        public LessonStep NextStep(string kind)
        {
            List<LessonStep> pool;
            if (kind == StepKinds.Manage) pool = db.manageSteps;
            else
            {
                var lesson = CurrentLesson;
                if (lesson == null) return null;
                pool = lesson.StepsOfKind(kind);
            }
            if (pool == null || pool.Count == 0) return null;
            int done = log.Count(JobId, ActivityFor(kind));
            return pool[done % pool.Count];
        }

        /// <summary>A random step of a kind (used by the full lesson so every session feels different).</summary>
        public LessonStep RandomStep(string kind)
        {
            List<LessonStep> pool;
            if (kind == StepKinds.Manage) pool = db.manageSteps;
            else
            {
                var lesson = CurrentLesson;
                if (lesson == null) return null;
                pool = lesson.StepsOfKind(kind);
            }
            if (pool == null || pool.Count == 0) return null;
            return pool[rng.Next(pool.Count)];
        }

        public TeachingSession Begin(LessonStep step)
        {
            return new TeachingSession(step, rng);
        }

        /// <summary>Logs a finished teaching decision. Returns false for an unfinished session or a duplicate.</summary>
        public bool RecordStep(string missionId, TeachingSession session)
        {
            if (session == null || !session.Finished) return false;
            string activity = ActivityFor(session.Step.kind);
            if (string.IsNullOrEmpty(activity)) return false;
            int result = session.FirstTryCorrect ? ActivityResult.FirstTry : ActivityResult.NeededHelp;
            return log.Record(JobId, missionId, activity, session.Step.id, result);
        }

        /// <summary>Logs that the child ran a whole lesson from start to finish.</summary>
        public bool RecordFullSession(string missionId, int stepsTotal, int stepsFirstTry)
        {
            var lesson = CurrentLesson;
            string detail = (lesson == null ? "" : lesson.id) + ":" + stepsFirstTry + "/" + stepsTotal;
            int result = stepsFirstTry == stepsTotal ? ActivityResult.FirstTry : ActivityResult.NeededHelp;
            return log.Record(JobId, missionId, ActivityIds.FullSession, detail, result);
        }
    }

    /// <summary>
    /// Turns the activity log into soft sentences for the parent report. It only says what the child
    /// did; it never claims talent and never uses coins or scores.
    /// </summary>
    public static class ActivityAnalyzer
    {
        public const string TextNote = "این گزارش فقط نشان می‌دهد در بازی چه کارهایی را انتخاب کرده‌ای؛ نتیجهٔ علمی نیست.";

        /// <summary>Number of real teaching actions (choosing a lesson is a preparation step and is not counted).</summary>
        public static int RealActivityCount(SaveData save, string jobId)
        {
            if (save == null || save.activity == null) return 0;
            int n = 0;
            foreach (var c in save.activity.counters)
                if (c != null && c.jobId == jobId && c.activityId != ActivityIds.LessonChosen) n += c.count;
            return n;
        }

        /// <summary>The different kinds of real activity the child has done at least once in a job.</summary>
        public static List<string> DistinctActivities(SaveData save, string jobId)
        {
            var list = new List<string>();
            if (save == null || save.activity == null) return list;
            foreach (var c in save.activity.counters)
                if (c != null && c.jobId == jobId && c.count > 0 && c.activityId != ActivityIds.LessonChosen) list.Add(c.activityId);
            return list;
        }

        public static List<string> TeacherLinesFa(SaveData save, LessonDatabase lessons)
        {
            var lines = new List<string>();
            if (save == null || save.activity == null) return lines;
            string job = TeachingService.JobId;
            int real = RealActivityCount(save, job);
            if (real == 0) return lines;

            lines.Add("در معلمی " + real + " بار کار واقعی تدریس را انجام داده‌ای: توضیح دادن مفهوم، جواب دادن به دانش‌آموزان و اداره کردن کلاس.");

            // Which lessons were chosen most (from the stored choices).
            var counts = new Dictionary<string, int>();
            foreach (var e in save.activity.events)
            {
                if (e == null || e.jobId != job || e.activityId != ActivityIds.LessonChosen) continue;
                int n;
                counts.TryGetValue(e.detail, out n);
                counts[e.detail] = n + 1;
            }
            if (counts.Count > 0)
            {
                var names = new List<string>();
                var ordered = new List<KeyValuePair<string, int>>(counts);
                ordered.Sort((a, b) => b.Value.CompareTo(a.Value));
                foreach (var kv in ordered)
                {
                    var l = lessons == null ? null : lessons.FindLesson(kv.Key);
                    names.Add(l == null ? kv.Key : l.titleFa);
                    if (names.Count == 3) break;
                }
                lines.Add("موضوع درسی که برای تدریس انتخاب کرده‌ای: " + string.Join("، ", names.ToArray()) + ".");
            }

            int helped = 0;
            foreach (var c in save.activity.counters)
                if (c != null && c.jobId == job && c.activityId != ActivityIds.LessonChosen) helped += c.neededHelp;
            if (helped > 0)
                lines.Add(helped + " بار بعد از دیدن توضیح دیگر، راه بهتر برای کمک به یادگیری را پیدا کرده‌ای.");
            return lines;
        }
    }
}
