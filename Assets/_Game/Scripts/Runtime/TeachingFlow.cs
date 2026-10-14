using System;
using System.Collections.Generic;
using System.Text;
using MyFutureCity.Core;

namespace MyFutureCity
{
    /// <summary>
    /// Plays the teacher profession as a real interaction: pick a lesson, show a concept, explain it,
    /// react to a student's answer, help a student who is stuck, manage the class. It only drives the
    /// dialogs; the choices, retries, shuffling and logging are in the unit-tested Core classes.
    /// </summary>
    public static class TeachingFlow
    {
        private const int LessonsPerPage = 3;

        // ------------------------------------------------------------------ lesson topic

        public static void ChooseTopic(GameSceneController scene, string missionId, Action onChosen)
        {
            ChooseTopicPage(scene, missionId, onChosen, 0);
        }

        private static void ChooseTopicPage(GameSceneController scene, string missionId, Action onChosen, int page)
        {
            var teaching = scene.Ctx.Teaching;
            var lessons = teaching.Lessons;
            if (lessons == null || lessons.Count == 0) { scene.Hud.Toast(Strings.NoLessons); return; }
            int pages = (lessons.Count + LessonsPerPage - 1) / LessonsPerPage;
            if (page >= pages) page = 0;

            var body = new StringBuilder(Strings.ChooseLessonBody);
            var options = new List<DialogOption>();
            for (int i = page * LessonsPerPage; i < lessons.Count && i < (page + 1) * LessonsPerPage; i++)
            {
                var lesson = lessons[i];
                body.Append("\n• ").Append(lesson.titleFa).Append(" (").Append(lesson.topicFa).Append(")");
                options.Add(new DialogOption(lesson.titleFa, () => OnTopicPicked(scene, missionId, onChosen, lesson)));
            }
            if (pages > 1)
            {
                int next = page + 1;
                options.Add(new DialogOption(Strings.More, () => ChooseTopicPage(scene, missionId, onChosen, next)));
            }
            options.Add(new DialogOption(Strings.Later, null));
            scene.Hud.ShowDialog(Strings.ChooseLessonTitle, body.ToString(), options.ToArray());
        }

        private static void OnTopicPicked(GameSceneController scene, string missionId, Action onChosen, LessonDefinition lesson)
        {
            if (!scene.Ctx.Teaching.ChooseLesson(lesson.id, missionId)) return;
            if (onChosen != null) onChosen();
            else scene.Hud.Toast(Strings.LessonChosen + lesson.titleFa);
        }

        // ------------------------------------------------------------------ one teaching decision

        /// <summary>
        /// Runs one teaching decision of the given kind. <paramref name="onDone"/> is called as soon as the
        /// decision is finished (a correct choice, or the best choice revealed after two tries).
        /// </summary>
        public static void RunStep(GameSceneController scene, string kind, string missionId, Action<TeachingSession> onDone)
        {
            var teaching = scene.Ctx.Teaching;
            if (kind != StepKinds.Manage && teaching.CurrentLesson == null)
            {
                // The child has not chosen a lesson yet (e.g. replaying a later stage): choose first.
                ChooseTopic(scene, missionId, () => RunStep(scene, kind, missionId, onDone));
                return;
            }
            var step = teaching.NextStep(kind);
            if (step == null) { scene.Hud.Toast(Strings.NoLessons); return; }
            Play(scene, step, missionId, "", onDone, null);
        }

        private static void Play(GameSceneController scene, LessonStep step, string missionId, string progress,
                                 Action<TeachingSession> onDone, Action onContinue)
        {
            var session = scene.Ctx.Teaching.Begin(step);
            Ask(scene, session, missionId, progress, onDone, onContinue);
        }

        private static void Ask(GameSceneController scene, TeachingSession session, string missionId, string progress,
                                Action<TeachingSession> onDone, Action onContinue)
        {
            var step = session.Step;
            var options = new DialogOption[session.OptionCount];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                options[i] = new DialogOption(session.OptionText(i),
                    () => OnChoice(scene, session, index, missionId, progress, onDone, onContinue));
            }
            scene.Hud.ShowDialog(TitleFor(step.kind) + progress, BuildBody(scene, step), options);
        }

        private static void OnChoice(GameSceneController scene, TeachingSession session, int index, string missionId,
                                     string progress, Action<TeachingSession> onDone, Action onContinue)
        {
            var result = session.Choose(index);
            if (result == null) return;

            if (result.Outcome == StepOutcome.WrongRetry)
            {
                // A friendly reaction plus another way to explain it; the child chooses again.
                var text = new StringBuilder();
                if (!string.IsNullOrEmpty(result.FeedbackFa)) text.Append(result.FeedbackFa).Append("\n\n");
                text.Append(Strings.AlternativeExplanation).Append(result.RetryHintFa);
                scene.Hud.ShowDialog(Strings.TryAnotherWay, text.ToString(),
                    new DialogOption(Strings.TryAgain, () => Ask(scene, session, missionId, progress, onDone, onContinue)));
                return;
            }

            // Finished: log what really happened first, then let the caller advance the mission.
            scene.Ctx.Teaching.RecordStep(missionId, session);
            if (onDone != null) onDone(session);

            // If advancing already finished the mission, its own completion dialog is on screen.
            if (onContinue == null && scene.Ctx.Missions.ActiveMission == null) return;

            var feedback = new StringBuilder();
            string title;
            if (result.Outcome == StepOutcome.Correct)
            {
                title = Strings.GreatTeaching;
                feedback.Append(result.FeedbackFa);
            }
            else
            {
                title = Strings.RightWayTitle;
                if (!string.IsNullOrEmpty(result.FeedbackFa)) feedback.Append(result.FeedbackFa).Append("\n\n");
                feedback.Append(result.RetryHintFa).Append("\n\n").Append(Strings.BestChoice).Append(result.CorrectTextFa);
            }
            scene.Hud.ShowDialog(title, feedback.ToString(), new DialogOption(Strings.Resume, onContinue));
        }

        private static string BuildBody(GameSceneController scene, LessonStep step)
        {
            var sb = new StringBuilder();
            var lesson = scene.Ctx.Teaching.CurrentLesson;
            if (lesson != null && (step.kind == StepKinds.Present || step.kind == StepKinds.Explain))
                sb.Append(Strings.LessonTopicPrefix).Append(lesson.titleFa).Append("»: ").Append(lesson.conceptFa).Append("\n\n");
            if (!string.IsNullOrEmpty(step.studentSaysFa)) sb.Append(step.studentSaysFa).Append("\n\n");
            sb.Append(step.promptFa);
            return sb.ToString();
        }

        private static string TitleFor(string kind)
        {
            switch (kind)
            {
                case StepKinds.Present: return Strings.TeachPresent;
                case StepKinds.Explain: return Strings.TeachExplain;
                case StepKinds.Check: return Strings.TeachCheck;
                case StepKinds.Help: return Strings.TeachHelp;
                default: return Strings.TeachManage;
            }
        }

        // ------------------------------------------------------------------ a whole lesson

        private sealed class FullRun
        {
            public GameSceneController Scene;
            public string MissionId;
            public List<LessonStep> Steps = new List<LessonStep>();
            public int Index;
            public int FirstTry;
            public Action OnFinished;
        }

        private static readonly string[] FullLessonOrder =
            { StepKinds.Present, StepKinds.Explain, StepKinds.Check, StepKinds.Manage, StepKinds.Help };

        /// <summary>Runs a complete lesson: present, explain, react to a student, choose class activity, help a student.</summary>
        public static void RunFullLesson(GameSceneController scene, string missionId, Action onFinished)
        {
            var teaching = scene.Ctx.Teaching;
            if (teaching.CurrentLesson == null)
            {
                ChooseTopic(scene, missionId, () => RunFullLesson(scene, missionId, onFinished));
                return;
            }
            var run = new FullRun { Scene = scene, MissionId = missionId, OnFinished = onFinished };
            foreach (var kind in FullLessonOrder)
            {
                var step = teaching.RandomStep(kind);
                if (step == null) { scene.Hud.Toast(Strings.NoLessons); return; }
                run.Steps.Add(step);
            }
            PlayNext(run);
        }

        private static void PlayNext(FullRun run)
        {
            var scene = run.Scene;
            if (run.Index >= run.Steps.Count)
            {
                scene.Ctx.Teaching.RecordFullSession(run.MissionId, run.Steps.Count, run.FirstTry);
                if (run.OnFinished != null) run.OnFinished();
                if (scene.Ctx.Missions.ActiveMission != null)
                    scene.Hud.ShowDialog(Strings.SessionEndTitle, SummaryText(run.FirstTry, run.Steps.Count));
                return;
            }
            string progress = " (" + (run.Index + 1) + " از " + run.Steps.Count + ")";
            Play(scene, run.Steps[run.Index], run.MissionId, progress,
                s => { if (s.FirstTryCorrect) run.FirstTry++; },
                () => { run.Index++; PlayNext(run); });
        }

        /// <summary>Encouraging and descriptive; never a grade.</summary>
        public static string SummaryText(int firstTry, int total)
        {
            var sb = new StringBuilder("جلسهٔ تو تمام شد! ");
            sb.Append(firstTry).Append(" تصمیم از ").Append(total).Append(" تصمیم را از همان بار اول درست انتخاب کردی.");
            if (firstTry < total)
                sb.Append("\nبقیه را بعد از دیدن توضیح دیگر درست انجام دادی؛ معلم‌ها هم همین‌طور یاد می‌گیرند.");
            return sb.ToString();
        }
    }
}
