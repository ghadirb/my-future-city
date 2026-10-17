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
        /// <summary>
        /// Optional "key=value" tag remembered when this option is accepted (e.g. "topic=science"). A later activity can
        /// have a variant for that tag, so the child's choice really changes what happens next.
        /// </summary>
        public string setsTag;
    }

    /// <summary>One decision inside a scene. A "free" step has no wrong answer: it records what the child prefers.</summary>
    [Serializable]
    public class ActivityStep
    {
        public string promptFa;
        public string hintFa;
        public bool free;
        /// <summary>
        /// Direct "doing" step: the child puts the items in the right order. The options are AUTHORED in the correct
        /// order (index 0 first); the UI shows them shuffled and each tap places one item. Option text = the item,
        /// option feedback = why it belongs at that position. Wrong placements cost nothing but are logged.
        /// </summary>
        public bool ordered;
        public List<ActivityOption> options = new List<ActivityOption>();

        public bool IsAccepted(int optionIndex)
        {
            if (options == null || optionIndex < 0 || optionIndex >= options.Count) return false;
            return free || options[optionIndex].correct;
        }
    }

    /// <summary>
    /// How the student in a scene reacts to what the child does. Pools of short lines; "{name}" is replaced by the
    /// scene's studentFa. Which line is shown depends on the child's real choices (see ActivitySession).
    /// </summary>
    [Serializable]
    public class StudentReactions
    {
        public List<string> unsureFa = new List<string>();        // after a first wrong pick in a step
        public List<string> discouragedFa = new List<string>();   // after two or more wrong picks in the same step
        public List<string> reliefFa = new List<string>();        // right pick after mistakes
        public List<string> confidentFa = new List<string>();     // right pick at the first try
        public List<string> endingGoodFa = new List<string>();    // scene ended without needing extra support
        public List<string> endingReviewFa = new List<string>();  // scene needed extra support / many mistakes
    }

    /// <summary>One playable situation (one spot in the world). Finishing every step completes one objective unit.</summary>
    [Serializable]
    public class ActivityScene
    {
        /// <summary>Optional name of the student in this scene; used in the reaction lines.</summary>
        public string studentFa;
        public string titleFa;
        public List<ActivityStep> steps = new List<ActivityStep>();
    }

    /// <summary>The scenes played instead of the default ones when the child has chosen the matching tag earlier.</summary>
    [Serializable]
    public class ActivityVariant
    {
        public string tag;
        public List<ActivityScene> scenes = new List<ActivityScene>();
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
        /// <summary>
        /// True for an extra activity of the job that no mission asks for. It unlocks once <see cref="missionId"/> has been
        /// completed once, never advances a mission, never pays coins/xp, and choosing it is logged as an optional pick.
        /// </summary>
        public bool optional;
        /// <summary>Optional: how the student reacts to the child's choices (see <see cref="StudentReactions"/>).</summary>
        public StudentReactions reactions;
        /// <summary>
        /// Optional detour step. When the child picks two wrong options in the same step the student is discouraged and
        /// this step (how to encourage the student) is played once per scene before the original step is tried again.
        /// </summary>
        public ActivityStep supportStep;

        // Unity's JsonUtility turns a missing nested object into an EMPTY object, never null, so "present" means "filled in".
        public bool HasSupport
        {
            get { return supportStep != null && (!string.IsNullOrEmpty(supportStep.promptFa) || (supportStep.options != null && supportStep.options.Count > 0)); }
        }

        public bool HasReactions
        {
            get
            {
                var r = reactions;
                return r != null && ((r.unsureFa != null && r.unsureFa.Count > 0) || (r.discouragedFa != null && r.discouragedFa.Count > 0)
                    || (r.reliefFa != null && r.reliefFa.Count > 0) || (r.confidentFa != null && r.confidentFa.Count > 0)
                    || (r.endingGoodFa != null && r.endingGoodFa.Count > 0) || (r.endingReviewFa != null && r.endingReviewFa.Count > 0));
            }
        }
        /// <summary>Spot number N of the objective plays scene N (wraps around).</summary>
        public List<ActivityScene> scenes = new List<ActivityScene>();
        /// <summary>Optional alternative scene sets chosen by an earlier decision of the child (see ActivityOption.setsTag).</summary>
        public List<ActivityVariant> variants = new List<ActivityVariant>();

        /// <summary>The first variant whose tag the child has chosen, or null for the default scenes.</summary>
        public int VariantIndexFor(IList<string> tags)
        {
            if (variants == null || tags == null) return -1;
            for (int i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                if (v == null || string.IsNullOrEmpty(v.tag) || v.scenes == null || v.scenes.Count == 0) continue;
                if (tags.Contains(v.tag)) return i;
            }
            return -1;
        }

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
                if (obj == null && !a.optional) errors.Add(p + "mission has no interact objective with this targetId.");
                if (string.IsNullOrEmpty(a.realWorldFa)) errors.Add(p + "missing realWorldFa (what the real job does).");
                if (string.IsNullOrEmpty(a.childDoesFa)) errors.Add(p + "missing childDoesFa (what the child does).");
                if (string.IsNullOrEmpty(a.skillFa)) errors.Add(p + "missing skillFa (what is practised).");
                if (a.scenes == null || a.scenes.Count == 0) { errors.Add(p + "has no scenes."); continue; }
                if (obj != null && !a.optional && a.scenes.Count < obj.RequiredCount)
                    errors.Add(p + "only " + a.scenes.Count + " scene(s) for " + obj.RequiredCount + " required spots.");

                ValidateScenes(errors, p, a.scenes);
                if (a.variants != null)
                {
                    for (int v = 0; v < a.variants.Count; v++)
                    {
                        var variant = a.variants[v];
                        string vp = p + "variant " + v + ": ";
                        if (variant == null || string.IsNullOrEmpty(variant.tag) || variant.tag.IndexOf('=') <= 0)
                        { errors.Add(vp + "needs a 'key=value' tag."); continue; }
                        if (variant.scenes == null || variant.scenes.Count == 0) { errors.Add(vp + "has no scenes."); continue; }
                        if (obj != null && variant.scenes.Count < obj.RequiredCount)
                            errors.Add(vp + "only " + variant.scenes.Count + " scene(s) for " + obj.RequiredCount + " required spots.");
                        ValidateScenes(errors, vp, variant.scenes);
                    }
                }
                ValidateStudentSupport(errors, p, a);
            }
            return errors;
        }

        private static void ValidateStudentSupport(List<string> errors, string p, ActivityDefinition a)
        {
            if (a.HasSupport)
            {
                var wrap = new List<ActivityScene>();
                wrap.Add(new ActivityScene { titleFa = "support", steps = new List<ActivityStep> { a.supportStep } });
                var sub = new List<string>();
                ValidateScenes(sub, p + "supportStep: ", wrap);
                errors.AddRange(sub);
                if (a.supportStep.free || a.supportStep.ordered) errors.Add(p + "supportStep must be a normal right/wrong step.");
                if (!a.HasReactions) errors.Add(p + "a supportStep needs reactions too.");
            }
            if (a.HasReactions)
            {
                CheckPool(errors, p, "unsureFa", a.reactions.unsureFa);
                CheckPool(errors, p, "discouragedFa", a.reactions.discouragedFa);
                CheckPool(errors, p, "reliefFa", a.reactions.reliefFa);
                CheckPool(errors, p, "confidentFa", a.reactions.confidentFa);
                CheckPool(errors, p, "endingGoodFa", a.reactions.endingGoodFa);
                CheckPool(errors, p, "endingReviewFa", a.reactions.endingReviewFa);
            }
        }

        private static void CheckPool(List<string> errors, string p, string name, List<string> pool)
        {
            if (pool == null || pool.Count == 0) { errors.Add(p + "reactions." + name + " is empty."); return; }
            foreach (var line in pool)
                if (string.IsNullOrEmpty(line)) errors.Add(p + "reactions." + name + " has an empty line.");
        }

        private static void ValidateScenes(List<string> errors, string p, List<ActivityScene> scenes)
        {
            for (int s = 0; s < scenes.Count; s++)
            {
                var sc = scenes[s];
                string sp = p + "scene " + s + ": ";
                if (sc == null || sc.steps == null || sc.steps.Count == 0) { errors.Add(sp + "has no steps."); continue; }
                if (string.IsNullOrEmpty(sc.titleFa)) errors.Add(sp + "missing titleFa.");
                for (int t = 0; t < sc.steps.Count; t++)
                {
                    var st = sc.steps[t];
                    string tp = sp + "step " + t + ": ";
                    if (st == null) { errors.Add(tp + "is null."); continue; }
                    if (string.IsNullOrEmpty(st.promptFa)) errors.Add(tp + "missing promptFa.");
                    if (st.options == null || st.options.Count < 2 || st.options.Count > (st.ordered ? 4 : 3))
                    { errors.Add(tp + (st.ordered ? "needs 3 or 4 items." : "needs 2 or 3 options.")); continue; }
                    int correct = 0;
                    foreach (var o in st.options)
                    {
                        if (o == null || string.IsNullOrEmpty(o.textFa)) errors.Add(tp + "an option has no text.");
                        else if (string.IsNullOrEmpty(o.feedbackFa)) errors.Add(tp + "option '" + o.textFa + "' has no feedback.");
                        if (o != null && o.correct) correct++;
                        if (o != null && !string.IsNullOrEmpty(o.setsTag) && o.setsTag.IndexOf('=') <= 0)
                            errors.Add(tp + "option '" + o.textFa + "' has a malformed setsTag (needs key=value).");
                    }
                    if (st.ordered)
                    {
                        if (st.free) errors.Add(tp + "an ordered step cannot also be free.");
                        if (st.options.Count < 3) errors.Add(tp + "an ordered step needs at least 3 items.");
                    }
                    else
                    {
                        if (!st.free && correct == 0) errors.Add(tp + "has no correct option.");
                        if (!st.free && correct == st.options.Count) errors.Add(tp + "every option is correct (use free:true for preference choices).");
                    }
                    if (!st.free && string.IsNullOrEmpty(st.hintFa)) errors.Add(tp + "missing hintFa.");
                }
            }
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
