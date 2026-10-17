using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    public enum DataConfidence { None, Low, Medium, High }

    /// <summary>What the child's play history says about one job.</summary>
    public class JobInsight
    {
        public string JobId;
        public string NameFa;
        public int StagesTotal;
        public int StagesDone;          // distinct stages completed at least once
        public int Completed;           // all completed runs (first runs + repeats)
        public int Abandoned;           // runs given up after real effort
        public int Replays;             // voluntary repeats of an already completed stage
        public float Seconds;           // real play time spent in this job
        public float InterestShare;     // 0..1, shares of all jobs add up to 1
        public float CompletionRate;    // completed / (completed + abandoned), 0..1
        public int FasterStages;        // stages whose latest run was clearly quicker than the first
        public int ComparedStages;      // stages that were finished at least twice (so a comparison exists)

        // Real job activities (decisions the child made). Kept apart on purpose: taking part, succeeding,
        // repeating and choosing freely are different things and must never be merged into one score.
        public int ActivitiesStarted;
        public int ActivitiesDone;      // participation: scenes really performed
        public int ActivityStepsDone;
        public int FirstTryCorrect;     // success: decisions that were right at the first try without a hint
        public int WrongChoices;
        public int HintsUsed;
        public int OptionalActivityRuns;// scenes finished while voluntarily repeating a finished stage
        public int OptionalPicked;      // scenes finished of the extra activities no mission asks for
        public int ActivitiesAbandoned;
        /// <summary>Hands-on placements (doing the work directly), kept apart from dialog decisions.</summary>
        public int DirectActions;
        public int DirectActionsRight;
        public int TasksDone;
        public int FreeChoices;         // preference decisions (no wrong answer) recorded in the log

        public int Attempts { get { return Completed + Abandoned; } }
        public float StageProgress { get { return StagesTotal <= 0 ? 0f : (float)StagesDone / StagesTotal; } }
        public bool HasActivities { get { return ActivitiesDone > 0; } }
        public float ActivitySuccessRate { get { return ActivityStepsDone <= 0 ? 0f : (float)FirstTryCorrect / ActivityStepsDone; } }
        public int ActivitySuccessPercent { get { return (int)Math.Round(ActivitySuccessRate * 100f); } }
        public int InterestPercent { get { return (int)Math.Round(InterestShare * 100f); } }
        public int CompletionPercent { get { return (int)Math.Round(CompletionRate * 100f); } }
    }

    public class InsightReport
    {
        public List<JobInsight> Jobs = new List<JobInsight>();     // sorted: most interest first
        public int TotalAttempts;
        public float TotalSeconds;
        public DataConfidence Confidence;
        public string TopInterestJobId;      // where the child spends / returns the most
        public string TopSuccessJobId;       // where the child finishes most reliably
        public string MostImprovedJobId;     // where repeats got clearly faster
        public string MostActivitiesJobId;   // where the child performed the most real activities
        public List<string> LinesFa = new List<string>();

        public bool HasData { get { return Confidence != DataConfidence.None; } }

        public JobInsight Find(string jobId)
        {
            foreach (var j in Jobs) if (j.JobId == jobId) return j;
            return null;
        }
    }

    /// <summary>
    /// Turns the child's own play history into a soft, explainable report. It only describes what
    /// the child DID (time, repeats, finished runs); it never claims talent, intelligence or personality.
    /// Pure C# so it is fully unit-tested.
    /// </summary>
    public static class InterestAnalyzer
    {
        // Weights of the interest signals. A voluntary repeat is the strongest sign of liking something.
        public const float WeightTime = 0.40f;
        public const float WeightRepeats = 0.35f;
        public const float WeightAttempts = 0.25f;
        /// <summary>Latest run counts as "faster" when it took at most this fraction of the first run.</summary>
        public const float FasterRatio = 0.85f;
        /// <summary>Success is only compared between jobs the child tried at least this many times.</summary>
        public const int MinAttemptsForSuccess = 2;
        /// <summary>Activity success is only mentioned after at least this many decisions (too few says nothing).</summary>
        public const int MinStepsForActivitySuccess = 3;

        public static InsightReport Analyze(MissionDatabase db, SaveData save)
        {
            var report = new InsightReport();
            if (db == null || save == null) return report;

            foreach (var job in db.jobs)
            {
                if (job == null || string.IsNullOrEmpty(job.id)) continue;
                var ji = new JobInsight { JobId = job.id, NameFa = job.nameFa };
                foreach (var m in db.missions)
                {
                    if (m.jobId != job.id) continue;
                    ji.StagesTotal++;
                    var p = save.GetMission(m.id, false);
                    if (p != null && p.timesCompleted > 0)
                    {
                        ji.StagesDone++;
                        ji.Completed += p.timesCompleted;     // authoritative, also covers saves made before reports existed
                    }
                }
                var js = save.GetJobStats(job.id, false);
                if (js != null)
                {
                    ji.Seconds = js.playSeconds;
                    ji.Abandoned = js.abandoned;
                }
                ji.Replays = ji.Completed - ji.StagesDone;      // runs beyond the first of each stage
                if (ji.Replays < 0) ji.Replays = 0;
                ji.CompletionRate = ji.Attempts == 0 ? 0f : (float)ji.Completed / ji.Attempts;
                CompareFirstAndLatest(save, job.id, ji);
                var ast = save.GetActivityStats(job.id, false);
                if (ast != null)
                {
                    ji.ActivitiesStarted = ast.started;
                    ji.ActivitiesDone = ast.completed;
                    ji.ActivityStepsDone = ast.stepsDone;
                    ji.FirstTryCorrect = ast.firstTry;
                    ji.WrongChoices = ast.wrongChoices;
                    ji.HintsUsed = ast.hints;
                    ji.OptionalActivityRuns = ast.optionalRuns;
                    ji.OptionalPicked = ast.optionalPicked;
                    ji.ActivitiesAbandoned = ast.abandoned;
                    ji.DirectActions = ast.actions;
                    ji.DirectActionsRight = ast.actionsRight;
                    ji.TasksDone = ast.tasksDone;
                }
                report.Jobs.Add(ji);
            }

            float sumSeconds = 0f; int sumReplays = 0, sumAttempts = 0;
            foreach (var j in report.Jobs) { sumSeconds += j.Seconds; sumReplays += j.Replays; sumAttempts += j.Attempts; }
            report.TotalSeconds = sumSeconds;
            report.TotalAttempts = sumAttempts;

            // Interest = weighted share of time, repeats and attempts. Signals with no data are
            // dropped and the remaining weights re-normalised, so a brand-new player is not distorted.
            float wTime = sumSeconds > 0f ? WeightTime : 0f;
            float wRep = sumReplays > 0 ? WeightRepeats : 0f;
            float wAtt = sumAttempts > 0 ? WeightAttempts : 0f;
            float wSum = wTime + wRep + wAtt;
            foreach (var j in report.Jobs)
            {
                if (wSum <= 0f) { j.InterestShare = 0f; continue; }
                float s = 0f;
                if (wTime > 0f) s += wTime * (j.Seconds / sumSeconds);
                if (wRep > 0f) s += wRep * ((float)j.Replays / sumReplays);
                if (wAtt > 0f) s += wAtt * ((float)j.Attempts / sumAttempts);
                j.InterestShare = s / wSum;
            }

            // Stable order: most interest first, then more finished runs, then database order.
            var order = new Dictionary<string, int>();
            for (int i = 0; i < report.Jobs.Count; i++) order[report.Jobs[i].JobId] = i;
            report.Jobs.Sort((a, b) =>
            {
                int c = b.InterestShare.CompareTo(a.InterestShare);
                if (c != 0) return c;
                c = b.Completed.CompareTo(a.Completed);
                if (c != 0) return c;
                return order[a.JobId].CompareTo(order[b.JobId]);
            });

            report.Confidence = ConfidenceFor(sumAttempts);
            if (report.Confidence == DataConfidence.None) { BuildLines(report); return report; }

            report.TopInterestJobId = report.Jobs[0].JobId;
            report.TopSuccessJobId = PickMostSuccessful(report);
            report.MostImprovedJobId = PickMostImproved(report);
            report.MostActivitiesJobId = PickMostActivities(report);
            BuildLines(report);
            return report;
        }

        public static DataConfidence ConfidenceFor(int totalAttempts)
        {
            if (totalAttempts <= 0) return DataConfidence.None;
            if (totalAttempts < 4) return DataConfidence.Low;
            if (totalAttempts < 10) return DataConfidence.Medium;
            return DataConfidence.High;
        }

        /// <summary>Compares the first and the most recent completed run of every stage played at least twice.</summary>
        private static void CompareFirstAndLatest(SaveData save, string jobId, JobInsight ji)
        {
            var first = new Dictionary<string, AttemptRecord>();
            var last = new Dictionary<string, AttemptRecord>();
            var count = new Dictionary<string, int>();
            foreach (var a in save.attempts)
            {
                if (a == null || a.jobId != jobId || !a.completed) continue;
                if (!first.ContainsKey(a.missionId)) first[a.missionId] = a;
                last[a.missionId] = a;
                int n;
                count.TryGetValue(a.missionId, out n);
                count[a.missionId] = n + 1;
            }
            foreach (var kv in count)
            {
                if (kv.Value < 2) continue;
                ji.ComparedStages++;
                var f = first[kv.Key];
                var l = last[kv.Key];
                if (f.seconds > 0 && l.seconds <= f.seconds * FasterRatio) ji.FasterStages++;
            }
        }

        private static string PickMostSuccessful(InsightReport r)
        {
            JobInsight best = null;
            foreach (var j in r.Jobs)
            {
                if (j.Attempts < MinAttemptsForSuccess) continue;
                if (best == null || IsMoreSuccessful(j, best)) best = j;
            }
            return best == null ? null : best.JobId;
        }

        private static bool IsMoreSuccessful(JobInsight a, JobInsight b)
        {
            if (Math.Abs(a.CompletionRate - b.CompletionRate) > 0.0001f) return a.CompletionRate > b.CompletionRate;
            if (a.StagesDone != b.StagesDone) return a.StagesDone > b.StagesDone;
            return a.Completed > b.Completed;
        }

        private static string PickMostActivities(InsightReport r)
        {
            JobInsight best = null;
            foreach (var j in r.Jobs)
            {
                if (j.ActivitiesDone <= 0) continue;
                if (best == null || j.ActivitiesDone > best.ActivitiesDone) best = j;
            }
            return best == null ? null : best.JobId;
        }

        private static string PickMostImproved(InsightReport r)
        {
            JobInsight best = null;
            foreach (var j in r.Jobs)
            {
                if (j.FasterStages <= 0) continue;
                if (best == null || j.FasterStages > best.FasterStages) best = j;
            }
            return best == null ? null : best.JobId;
        }

        // ---------- Persian wording (soft: "it seems", never a verdict) ----------

        public const string TextNoData = "هنوز مأموریتی انجام نداده‌ای. چند مأموریت را امتحان کن تا ببینیم چه کاری بیشتر دوست داری.";
        public const string TextLowData = "این فقط شروع کار است؛ هرچه بیشتر بازی کنی، گزارش دقیق‌تر می‌شود.";
        public const string TextNotEnough = "هنوز اطلاعات کافی نیست تا بگوییم کدام کار را بیشتر دوست داری.";
        public const string TextFreedom = "هر شغلی را هر چند بار خواستی می‌توانی دوباره انجام بدهی.";

        private static void BuildLines(InsightReport r)
        {
            if (r.Confidence == DataConfidence.None) { r.LinesFa.Add(TextNoData); return; }

            var top = r.Find(r.TopInterestJobId);
            // Addendum section 8: with too little data the honest answer is "not enough information".
            if (r.Confidence == DataConfidence.Low) top = null;
            if (r.Confidence == DataConfidence.Low) r.LinesFa.Add(TextNotEnough);
            if (top != null)
                r.LinesFa.Add("به نظر می‌رسد بیشتر از همه به «" + top.NameFa + "» علاقه نشان داده‌ای: "
                              + DescribeEffort(top) + ".");

            var succ = r.Find(r.TopSuccessJobId);
            if (succ != null)
            {
                if (succ.CompletionRate >= 0.999f)
                    r.LinesFa.Add("در «" + succ.NameFa + "» هر بار کار را تمام کرده‌ای.");
                else
                    r.LinesFa.Add("در «" + succ.NameFa + "» بیشتر از بقیه کار را به پایان رسانده‌ای ("
                                  + succ.CompletionPercent + " درصد).");
            }

            var imp = r.Find(r.MostImprovedJobId);
            if (imp != null)
                r.LinesFa.Add("در «" + imp.NameFa + "» با تکرار، سریع‌تر شده‌ای.");

            if (top != null && succ != null && top.JobId != succ.JobId)
                r.LinesFa.Add("بیشترین وقتت صرف «" + top.NameFa + "» شده، ولی در «" + succ.NameFa
                              + "» موفق‌تر بوده‌ای؛ شاید هر دو را بیشتر امتحان کنی.");

            // Wording stays descriptive ("did"), never a verdict ("is talented").
            var act = r.Find(r.MostActivitiesJobId);
            if (act != null)
                r.LinesFa.Add("در «" + act.NameFa + "» " + act.ActivitiesDone + " فعالیت واقعی کار را خودت انجام داده‌ای"
                              + (act.OptionalActivityRuns > 0 ? " و " + act.OptionalActivityRuns + " بار آن را با میل خودت تکرار کرده‌ای." : "."));

            // Choosing an extra activity nobody asked for is a separate, descriptive signal (never a verdict).
            JobInsight extra = null;
            foreach (var j in r.Jobs)
                if (j.OptionalPicked > 0 && (extra == null || j.OptionalPicked > extra.OptionalPicked)) extra = j;
            if (extra != null)
                r.LinesFa.Add("در «" + extra.NameFa + "» فعالیت اختیاری را هم خودت انتخاب کرده‌ای (" + extra.OptionalPicked + " بار).");

            JobInsight steady = null;
            foreach (var j in r.Jobs)
            {
                if (j.ActivityStepsDone < MinStepsForActivitySuccess) continue;
                if (steady == null || j.ActivitySuccessRate > steady.ActivitySuccessRate) steady = j;
            }
            if (steady != null && steady.ActivitySuccessRate >= 0.5f)
                r.LinesFa.Add("در «" + steady.NameFa + "» بیشتر تصمیم‌ها را در همان تلاش اول درست انتخاب کرده‌ای.");

            int untried = 0;
            JobInsight firstUntried = null;
            foreach (var j in r.Jobs)
                if (j.Attempts == 0) { untried++; if (firstUntried == null) firstUntried = j; }
            if (untried > 0)
                r.LinesFa.Add(untried + " شغل را هنوز امتحان نکرده‌ای؛ برای شناخت بهتر علاقه‌ات می‌توانی «" + firstUntried.NameFa + "» را هم امتحان کنی.");

            if (r.Confidence == DataConfidence.Low) r.LinesFa.Add(TextLowData);
        }

        public static string DescribeEffort(JobInsight j)
        {
            var parts = new List<string>();
            parts.Add(FormatMinutes(j.Seconds));
            if (j.Replays > 0) parts.Add(j.Replays + " بار تکرار");
            else parts.Add(j.StagesDone + " مرحله");
            return string.Join("، ", parts.ToArray());
        }

        public static string FormatMinutes(float seconds)
        {
            int m = (int)Math.Round(seconds / 60f);
            if (seconds < 60f) return "کمتر از یک دقیقه";
            return m + " دقیقه";
        }
    }
}
