using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>
    /// A short, child-friendly teaching simulation. The player must choose a helpful
    /// explanation or teaching response; simply touching the object cannot complete it.
    /// </summary>
    public sealed class TeacherLessonInteractable : Interactable
    {
        public string missionId;
        public string targetId;
        public int order;
        public GameObject pendingVisual;
        public GameObject completedVisual;

        private GameContext ctx;
        private bool done;

        private sealed class LessonQuestion
        {
            public string Prompt;
            public string First;
            public string Second;
            public int CorrectIndex;
            public string Feedback;
        }

        private void Start()
        {
            ctx = GameContext.Instance;
            if (ctx != null) ctx.Missions.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (ctx != null) ctx.Missions.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (ctx == null) return;
            var status = ctx.Missions.GetStatus(missionId);
            done = status == MissionStatus.Completed ||
                   (status == MissionStatus.Active && order < ctx.Missions.GetCountByTarget(missionId, targetId));
            if (pendingVisual != null) pendingVisual.SetActive(!done);
            if (completedVisual != null) completedVisual.SetActive(done);
        }

        public override bool IsAvailable(GameContext context)
        {
            return context != null &&
                   context.Missions.GetStatus(missionId) == MissionStatus.Active &&
                   context.Missions.IsObjectiveCurrent(ObjectiveType.Interact, targetId) &&
                   order == context.Missions.GetCountByTarget(missionId, targetId);
        }

        public override bool MatchesCurrentObjective(GameContext context)
        {
            return IsAvailable(context);
        }

        public override string GuideName
        {
            get { return targetId == "lesson_board" ? "تمرین تدریس" : "دانش‌آموز منتظر کمک"; }
        }

        public override string GetPrompt(GameContext context)
        {
            return targetId == "lesson_board" ? "تمرین روش تدریس" : "کمک به دانش‌آموز";
        }

        public override void Interact(GameSceneController scene)
        {
            if (scene == null || !IsAvailable(scene.Ctx)) return;
            var question = GetQuestion(targetId, order);
            scene.Hud.ShowDialog(
                targetId == "lesson_board" ? "تمرین تدریس" : "کلاس درس",
                question.Prompt,
                new DialogOption(question.First, () => Answer(scene, question, 0)),
                new DialogOption(question.Second, () => Answer(scene, question, 1)));
        }

        private void Answer(GameSceneController scene, LessonQuestion question, int choice)
        {
            if (choice != question.CorrectIndex)
            {
                scene.Hud.ShowDialog(
                    "یک بار دیگر فکر کنیم",
                    "اشکالی ندارد؛ معلم خوب با حوصله راه‌های مختلف را بررسی می‌کند. دوباره انتخاب کن.",
                    new DialogOption("دوباره تلاش می‌کنم", () => Interact(scene)),
                    new DialogOption("بعداً برمی‌گردم", null));
                return;
            }

            bool advanced = scene.Ctx.Missions.Notify(ObjectiveType.Interact, targetId);
            if (advanced && scene.Ctx.Missions.ActiveMission != null)
                scene.Hud.Toast(question.Feedback);
            Refresh();
        }

        private static LessonQuestion GetQuestion(string target, int index)
        {
            if (target == "lesson_board")
            {
                switch (index % 3)
                {
                    case 0:
                        return new LessonQuestion {
                            Prompt = "دانش‌آموز می‌پرسد چرا یک‌دوم از یک‌چهارم بزرگ‌تر است. کدام روش بهتر کمکش می‌کند؟",
                            First = "با دو شکل هم‌اندازه نشان بدهم و با هم مقایسه کنیم",
                            Second = "بگویم فقط جواب را حفظ کند",
                            CorrectIndex = 0,
                            Feedback = "خوب است! یک مثال دیداری می‌تواند فهمیدن مفهوم را آسان‌تر کند."
                        };
                    case 1:
                        return new LessonQuestion {
                            Prompt = "دانش‌آموز پاسخ اشتباهی داده است. معلم چگونه بهتر کمک می‌کند؟",
                            First = "بپرسم چطور فکر کرده و بعد یک مثال ساده‌تر بدهم",
                            Second = "او را سرزنش کنم تا دفعه بعد اشتباه نکند",
                            CorrectIndex = 0,
                            Feedback = "آفرین! فهمیدن روش فکر کردن دانش‌آموز به انتخاب توضیح مناسب کمک می‌کند."
                        };
                    default:
                        return new LessonQuestion {
                            Prompt = "چطور بررسی کنیم که دانش‌آموز واقعاً درس را یاد گرفته است؟",
                            First = "از او بخواهم با زبان خودش توضیح بدهد و یک مثال حل کند",
                            Second = "فقط بپرسم «فهمیدی؟» و سریع به درس بعدی بروم",
                            CorrectIndex = 0,
                            Feedback = "درست است! توضیح دادن با زبان خود دانش‌آموز نشانه بهتری از یادگیری است."
                        };
                }
            }

            switch (index % 3)
            {
                case 0:
                    return new LessonQuestion {
                        Prompt = "دانش‌آموز هنگام خواندن یک جمله مکث می‌کند. چه کمکی بهتر است؟",
                        First = "جمله را به بخش‌های کوتاه‌تر تقسیم کنیم و با هم تمرین کنیم",
                        Second = "جمله را به جای او بخوانم و از او بخواهم ساکت بماند",
                        CorrectIndex = 0,
                        Feedback = "آفرین! تمرین مرحله‌به‌مرحله فرصت یادگیری می‌دهد."
                    };
                case 1:
                    return new LessonQuestion {
                        Prompt = "دو دانش‌آموز با سرعت‌های متفاوت تمرین می‌کنند. چه کار منصفانه‌تر است؟",
                        First = "به هر کدام زمان و راهنمایی متناسب با نیازش بدهم",
                        Second = "آن‌ها را با هم مقایسه کنم و به نفر کندتر عجله بدهم",
                        CorrectIndex = 0,
                        Feedback = "درست است! دانش‌آموزان با سرعت‌های متفاوت یاد می‌گیرند."
                    };
                default:
                    return new LessonQuestion {
                        Prompt = "دانش‌آموز جواب را درست گفته، اما نمی‌تواند روشش را توضیح دهد. چه کار کنم؟",
                        First = "با یک پرسش ساده کمکش کنم مراحل فکر کردنش را توضیح دهد",
                        Second = "بدون بررسی بگویم درس را کامل یاد گرفته است",
                        CorrectIndex = 0,
                        Feedback = "خوب است! پرسش آرام و روشن به بررسی یادگیری کمک می‌کند."
                    };
            }
        }
    }
}
