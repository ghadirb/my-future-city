using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    /// <summary>One answer the child can pick in an activity step.</summary>
    [Serializable]
    public class ActivityOption
    {
        public string textFa;
        public bool correct;
        /// <summary>Friendly, educational reaction shown after this option is picked.</summary>
        public string feedbackFa;
    }

    /// <summary>One decision inside a scene. A "free" step has no wrong answer: it records what the child prefers.</summary>
    [Serializable]
    public class ActivityStep
    {
        public string promptFa;
        public string hintFa;
        public bool free;
        public List<ActivityOption> options = new List<ActivityOption>();

        public bool IsAccepted(int optionIndex)
        {
            if (options == null || optionIndex < 0 || optionIndex >= options.Count) return false;
            return free || options[optionIndex].correct;
        }
    }

    /// <summary>One playable situation (one spot in the world). Finishing every step completes one objective unit.</summary>
    [Serializable]
    public class ActivityScene
    {
        public string titleFa;
        public List<ActivityStep> steps = new List<ActivityStep>();
    }

    /// <summary>
    /// The real work a child does for one mission objective. The first three text fields document the
    /// design rule of the master addendum (what the real job does, what the child does, what skill is practised).
    /// </summary>
    [Serializable]
    public class ActivityDefinition
    {
        public string missionId;
        public string targetId;
        public string jobId;
        public string realWorldFa;
        public string childDoesFa;
        public string skillFa;
        /// <summary>Spot number N of the objective plays scene N (wraps around).</summary>
        public List<ActivityScene> scenes = new List<ActivityScene>();

        public ActivityScene SceneFor(int order)
        {
            if (scenes == null || scenes.Count == 0) return null;
            int i = order < 0 ? 0 : order % scenes.Count;
            return scenes[i];
        }

        public int SceneIndexFor(int order)
        {
            if (scenes == null || scenes.Count == 0) return 0;
            return order < 0 ? 0 : order % scenes.Count;
        }
    }

    [Serializable]
    public class ActivityDatabase
    {
        public List<ActivityDefinition> activities = new List<ActivityDefinition>();

        public ActivityDefinition Find(string missionId, string targetId)
        {
            if (activities == null) return null;
            for (int i = 0; i < activities.Count; i++)
            {
                var a = activities[i];
                if (a != null && a.missionId == missionId && a.targetId == targetId) return a;
            }
            return null;
        }

        public bool Has(string missionId, string targetId) { return Find(missionId, targetId) != null; }
    }

    /// <summary>Static checks that stop an "activity" without real decisions from reaching a player.</summary>
    public static class ActivityDatabaseValidator
    {
        public static List<string> Validate(ActivityDatabase acts, MissionDatabase db)
        {
            var errors = new List<string>();
            if (acts == null) { errors.Add("Activity database is null."); return errors; }
            if (db == null) { errors.Add("Mission database is null."); return errors; }

            var seen = new HashSet<string>();
            foreach (var a in acts.activities)
            {
                if (a == null) { errors.Add("An activity is null."); continue; }
                string p = "Activity '" + a.missionId + "/" + a.targetId + "': ";
                if (!seen.Add(a.missionId + "/" + a.targetId)) errors.Add(p + "duplicate.");
                var m = db.FindMission(a.missionId);
                if (m == null) { errors.Add(p + "unknown mission."); continue; }
                if (m.jobId != a.jobId) errors.Add(p + "jobId '" + a.jobId + "' does not match the mission's job '" + m.jobId + "'.");
                ObjectiveDefinition obj = null;
                foreach (var o in m.objectives)
                    if (o != null && o.targetId == a.targetId && o.ParsedType() == ObjectiveType.Interact) obj = o;
                if (obj == null) errors.Add(p + "mission has no interact objective with this targetId.");
                if (string.IsNullOrEmpty(a.realWorldFa)) errors.Add(p + "missing realWorldFa (what the real job does).");
                if (string.IsNullOrEmpty(a.childDoesFa)) errors.Add(p + "missing childDoesFa (what the child does).");
                if (string.IsNullOrEmpty(a.skillFa)) errors.Add(p + "missing skillFa (what is practised).");
                if (a.scenes == null || a.scenes.Count == 0) { errors.Add(p + "has no scenes."); continue; }
                if (obj != null && a.scenes.Count < obj.RequiredCount)
                    errors.Add(p + "only " + a.scenes.Count + " scene(s) for " + obj.RequiredCount + " required spots.");

                for (int s = 0; s < a.scenes.Count; s++)
                {
                    var sc = a.scenes[s];
                    string sp = p + "scene " + s + ": ";
                    if (sc == null || sc.steps == null || sc.steps.Count == 0) { errors.Add(sp + "has no steps."); continue; }
                    if (string.IsNullOrEmpty(sc.titleFa)) errors.Add(sp + "missing titleFa.");
                    for (int t = 0; t < sc.steps.Count; t++)
                    {
                        var st = sc.steps[t];
                        string tp = sp + "step " + t + ": ";
                        if (st == null) { errors.Add(tp + "is null."); continue; }
                        if (string.IsNullOrEmpty(st.promptFa)) errors.Add(tp + "missing promptFa.");
                        if (st.options == null || st.options.Count < 2 || st.options.Count > 3)
                        { errors.Add(tp + "needs 2 or 3 options."); continue; }
                        int correct = 0;
                        foreach (var o in st.options)
                        {
                            if (o == null || string.IsNullOrEmpty(o.textFa)) errors.Add(tp + "an option has no text.");
                            else if (string.IsNullOrEmpty(o.feedbackFa)) errors.Add(tp + "option '" + o.textFa + "' has no feedback.");
                            if (o != null && o.correct) correct++;
                        }
                        if (!st.free && correct == 0) errors.Add(tp + "has no correct option.");
                        if (!st.free && correct == st.options.Count) errors.Add(tp + "every option is correct (use free:true for preference choices).");
                        if (!st.free && string.IsNullOrEmpty(st.hintFa)) errors.Add(tp + "missing hintFa.");
                    }
                }
            }
            return errors;
        }

        /// <summary>
        /// Jobs in which at least one mission has no interact objective backed by an activity, i.e. missions a
        /// child could finish by only collecting things or talking. Empty list == every job is real work.
        /// </summary>
        public static List<string> JobsWithMissionsWithoutRealActivity(MissionDatabase db, ActivityDatabase acts)
        {
            var result = new List<string>();
            if (db == null || acts == null) return result;
            foreach (var m in db.missions)
            {
                bool real = false;
                foreach (var o in m.objectives)
                    if (o != null && o.ParsedType() == ObjectiveType.Interact && acts.Has(m.id, o.targetId)) real = true;
                if (!real && !result.Contains(m.jobId)) result.Add(m.jobId);
            }
            return result;
        }

        /// <summary>Missions of one job that could be completed without performing any real activity.</summary>
        public static List<string> MissionsWithoutRealActivity(MissionDatabase db, ActivityDatabase acts, string jobId)
        {
            var result = new List<string>();
            foreach (var m in db.missions)
            {
                if (m.jobId != jobId) continue;
                bool real = false;
                foreach (var o in m.objectives)
                    if (o != null && o.ParsedType() == ObjectiveType.Interact && acts.Has(m.id, o.targetId)) real = true;
                if (!real) result.Add(m.id);
            }
            return result;
        }
    }
}
