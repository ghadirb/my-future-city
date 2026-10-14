using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    /// <summary>Kinds of teaching decisions. Each maps to one real part of a teacher's work.</summary>
    public static class StepKinds
    {
        public const string Present = "present";   // choose how to show a concept (picture, objects, board)
        public const string Explain = "explain";   // say the concept in simple words
        public const string Check = "check";       // a student answered; decide how to continue the lesson
        public const string Help = "help";         // a student is stuck; choose a helpful explanation
        public const string Manage = "manage";     // classroom situation: pick the next activity

        public static bool IsKnown(string kind)
        {
            return kind == Present || kind == Explain || kind == Check || kind == Help || kind == Manage;
        }
    }

    /// <summary>
    /// One teaching decision: a situation, 2-4 possible actions and exactly one best action.
    /// Kept as plain data so new lessons can be added in JSON without touching code.
    /// </summary>
    [Serializable]
    public class LessonStep
    {
        public string id;
        public string kind;
        public string promptFa;
        /// <summary>Optional: what a student says or did (shown above the question).</summary>
        public string studentSaysFa;
        public List<string> options = new List<string>();
        public int correctIndex;
        /// <summary>Friendly feedback for each option, parallel to <see cref="options"/>. May be empty.</summary>
        public List<string> feedbackFa = new List<string>();
        /// <summary>A different way to explain it, shown after the first wrong choice.</summary>
        public string retryHintFa;
        public string successFa;

        public string FeedbackFor(int optionIndex)
        {
            if (feedbackFa == null || optionIndex < 0 || optionIndex >= feedbackFa.Count) return "";
            return feedbackFa[optionIndex] ?? "";
        }
    }

    [Serializable]
    public class LessonDefinition
    {
        public string id;
        public string titleFa;
        /// <summary>Short subject label, e.g. "ریاضی".</summary>
        public string topicFa;
        public string summaryFa;
        /// <summary>The idea the child is going to teach.</summary>
        public string conceptFa;
        public List<LessonStep> steps = new List<LessonStep>();

        public List<LessonStep> StepsOfKind(string kind)
        {
            var list = new List<LessonStep>();
            if (steps == null) return list;
            for (int i = 0; i < steps.Count; i++)
                if (steps[i] != null && steps[i].kind == kind) list.Add(steps[i]);
            return list;
        }
    }

    [Serializable]
    public class LessonDatabase
    {
        public List<LessonDefinition> lessons = new List<LessonDefinition>();
        /// <summary>Classroom-management situations shared by all lessons.</summary>
        public List<LessonStep> manageSteps = new List<LessonStep>();

        public LessonDefinition FindLesson(string id)
        {
            if (lessons == null || id == null) return null;
            for (int i = 0; i < lessons.Count; i++)
                if (lessons[i] != null && lessons[i].id == id) return lessons[i];
            return null;
        }
    }

    /// <summary>Static checks that catch broken lesson data before a child sees it.</summary>
    public static class LessonDatabaseValidator
    {
        public const int MinPerKind = 1;
        public const int MinCheck = 2;
        public const int MinHelp = 2;
        public const int MinManage = 3;

        public static List<string> Validate(LessonDatabase db)
        {
            var errors = new List<string>();
            if (db == null) { errors.Add("Lesson database is null."); return errors; }
            if (db.lessons == null || db.lessons.Count == 0) { errors.Add("No lessons."); return errors; }

            var ids = new HashSet<string>();
            var stepIds = new HashSet<string>();
            foreach (var l in db.lessons)
            {
                if (l == null || string.IsNullOrEmpty(l.id)) { errors.Add("A lesson has no id."); continue; }
                if (!ids.Add(l.id)) errors.Add("Duplicate lesson id: " + l.id);
                string p = "Lesson '" + l.id + "': ";
                if (string.IsNullOrEmpty(l.titleFa)) errors.Add(p + "missing titleFa.");
                if (string.IsNullOrEmpty(l.conceptFa)) errors.Add(p + "missing conceptFa.");
                if (l.steps == null) { errors.Add(p + "no steps."); continue; }
                foreach (var s in l.steps) ValidateStep(s, p, StepKindsInLesson, stepIds, errors);

                if (l.StepsOfKind(StepKinds.Present).Count < MinPerKind) errors.Add(p + "needs a 'present' step.");
                if (l.StepsOfKind(StepKinds.Explain).Count < MinPerKind) errors.Add(p + "needs an 'explain' step.");
                if (l.StepsOfKind(StepKinds.Check).Count < MinCheck) errors.Add(p + "needs at least " + MinCheck + " 'check' steps.");
                if (l.StepsOfKind(StepKinds.Help).Count < MinHelp) errors.Add(p + "needs at least " + MinHelp + " 'help' steps.");
            }

            if (db.manageSteps == null || db.manageSteps.Count < MinManage)
                errors.Add("At least " + MinManage + " shared 'manage' steps are required.");
            else
                foreach (var s in db.manageSteps) ValidateStep(s, "Shared: ", StepKindsShared, stepIds, errors);
            return errors;
        }

        private static readonly string[] StepKindsInLesson =
            { StepKinds.Present, StepKinds.Explain, StepKinds.Check, StepKinds.Help };
        private static readonly string[] StepKindsShared = { StepKinds.Manage };

        private static void ValidateStep(LessonStep s, string prefix, string[] allowedKinds, HashSet<string> stepIds, List<string> errors)
        {
            if (s == null) { errors.Add(prefix + "a step is null."); return; }
            string p = prefix + "step '" + s.id + "': ";
            if (string.IsNullOrEmpty(s.id)) errors.Add(prefix + "a step has no id.");
            else if (!stepIds.Add(s.id)) errors.Add(p + "duplicate step id.");
            if (!StepKinds.IsKnown(s.kind)) errors.Add(p + "unknown kind '" + s.kind + "'.");
            else if (Array.IndexOf(allowedKinds, s.kind) < 0) errors.Add(p + "kind '" + s.kind + "' is not allowed here.");
            if (string.IsNullOrEmpty(s.promptFa)) errors.Add(p + "missing promptFa.");
            if (string.IsNullOrEmpty(s.successFa)) errors.Add(p + "missing successFa.");
            if (string.IsNullOrEmpty(s.retryHintFa)) errors.Add(p + "missing retryHintFa (the alternative explanation).");
            if (s.options == null || s.options.Count < 2 || s.options.Count > 4)
            {
                errors.Add(p + "needs 2-4 options.");
                return;
            }
            foreach (var o in s.options)
                if (string.IsNullOrEmpty(o)) errors.Add(p + "has an empty option.");
            if (s.correctIndex < 0 || s.correctIndex >= s.options.Count) errors.Add(p + "correctIndex out of range.");
            if (s.feedbackFa != null && s.feedbackFa.Count != 0 && s.feedbackFa.Count != s.options.Count)
                errors.Add(p + "feedbackFa must be empty or match options.");
        }
    }
}
