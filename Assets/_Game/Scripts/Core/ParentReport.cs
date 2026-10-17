using System.Collections.Generic;

namespace MyFutureCity.Core
{
    /// <summary>One titled block of the parent/coach report.</summary>
    public class ParentSection
    {
        public string TitleFa;
        public List<string> Lines = new List<string>();
    }

    /// <summary>
    /// Addendum section 8: a plain report for a parent or coach. It only DESCRIBES what the child chose and
    /// repeated inside the game. It never names a talent or a future job, never compares with other children,
    /// and says "not enough information" until there is enough real play. Built from the real play log only.
    /// </summary>
    public class ParentReport
    {
        public bool EnoughData;
        public DataConfidence Confidence;
        public List<ParentSection> Sections = new List<ParentSection>();
        public string NoteFa;
    }

    public static class ParentReportBuilder
    {
        /// <summary>A job is only called "chosen more" after the child tried it at least this many times.</summary>
        public const int MinAttemptsForChoice = 2;
        /// <summary>If the runner-up is this close to the top share, both are named.</summary>
        public const float TieMargin = 0.05f;

        public const string TitleMostChosen = "کدام حرفه‌ها بیشتر انتخاب شده‌اند";
        public const string TitleRepeated = "کدام فعالیت‌ها را بیشتر تکرار کرده است";
        public const string TitleExperienced = "در کدام حرفه‌ها مأموریت‌های اصلی را تجربه کرده است";
        public const string TitleNotTried = "کدام حرفه‌ها هنوز به‌اندازه کافی امتحان نشده‌اند";
        public const string TitleNext = "چه فعالیت‌هایی برای تجربهٔ بعدی مناسب‌اند";
        public const string TextNotEnough = "اطلاعات کافی نیست.";
        public const string TextNotEnoughHint = "اطلاعات کافی نیست؛ هنوز تعداد مأموریت‌های انجام‌شده کم است و نمی‌شود دربارهٔ گرایش او حرفی زد.";
        public const string TextNoneYet = "هنوز مورد ثبت‌شده‌ای نیست.";
        public const string TextNote = "این گزارش فقط رفتار کودک را در همین بازی توصیف می‌کند. ارزیابی علمی یا روان‌شناختی نیست و دربارهٔ آیندهٔ شغلی او نتیجه‌ای نمی‌گیرد. او می‌تواند هر حرفه‌ای را آزادانه امتحان کند.";

        public static ParentReport Build(MissionDatabase db, SaveData save)
        {
            var insight = InterestAnalyzer.Analyze(db, save);
            return Build(insight);
        }

        public static ParentReport Build(InsightReport insight)
        {
            var r = new ParentReport();
            r.NoteFa = TextNote;
            r.Confidence = insight == null ? DataConfidence.None : insight.Confidence;
            r.EnoughData = r.Confidence == DataConfidence.Medium || r.Confidence == DataConfidence.High;

            var most = new ParentSection { TitleFa = TitleMostChosen };
            var repeated = new ParentSection { TitleFa = TitleRepeated };
            var experienced = new ParentSection { TitleFa = TitleExperienced };
            var notTried = new ParentSection { TitleFa = TitleNotTried };
            var next = new ParentSection { TitleFa = TitleNext };
            r.Sections.Add(most); r.Sections.Add(repeated); r.Sections.Add(experienced);
            r.Sections.Add(notTried); r.Sections.Add(next);

            if (insight == null || insight.Jobs.Count == 0 || insight.Confidence == DataConfidence.None)
            {
                most.Lines.Add(TextNotEnough);
                repeated.Lines.Add(TextNoneYet);
                experienced.Lines.Add(TextNoneYet);
                notTried.Lines.Add(TextNoneYet);
                next.Lines.Add(TextNoneYet);
                return r;
            }

            // 1) most chosen: only with enough data, never from success or speed
            if (!r.EnoughData || insight.Jobs[0].Attempts < MinAttemptsForChoice)
            {
                most.Lines.Add(TextNotEnoughHint);
            }
            else
            {
                var top = insight.Jobs[0];
                most.Lines.Add("در این دوره بازی، فعالیت‌های «" + top.NameFa + "» را بیشتر انتخاب کرده است.");
                if (insight.Jobs.Count > 1)
                {
                    var second = insight.Jobs[1];
                    if (second.Attempts >= MinAttemptsForChoice && top.InterestShare - second.InterestShare < TieMargin)
                        most.Lines.Add("فعالیت‌های «" + second.NameFa + "» را هم تقریباً به همان اندازه انتخاب کرده است.");
                }
            }

            // 2) repeated: voluntary returns and extra activities nobody asked for
            var byReplays = new List<JobInsight>();
            foreach (var j in insight.Jobs) if (j.Replays > 0) byReplays.Add(j);
            byReplays.Sort((a, b) => b.Replays.CompareTo(a.Replays));
            for (int i = 0; i < byReplays.Count && i < 3; i++)
                repeated.Lines.Add("به «" + byReplays[i].NameFa + "» " + byReplays[i].Replays + " بار بازگشته است.");
            var byOptional = new List<JobInsight>();
            foreach (var j in insight.Jobs) if (j.OptionalPicked > 0) byOptional.Add(j);
            byOptional.Sort((a, b) => b.OptionalPicked.CompareTo(a.OptionalPicked));
            for (int i = 0; i < byOptional.Count && i < 3; i++)
                repeated.Lines.Add("در «" + byOptional[i].NameFa + "» فعالیت اختیاری را " + byOptional[i].OptionalPicked + " بار خودش انتخاب کرده است.");
            if (repeated.Lines.Count == 0) repeated.Lines.Add(TextNoneYet);

            // 3) experienced: real main missions (a stage counts only when it was really finished)
            foreach (var j in insight.Jobs)
            {
                if (j.StagesDone <= 0) continue;
                string s = "در «" + j.NameFa + "» " + j.StagesDone + " مأموریت از " + j.StagesTotal + " را تجربه کرده است";
                if (j.ActivitiesDone > 0) s += " و " + j.ActivitiesDone + " فعالیت واقعی را خودش انجام داده است";
                experienced.Lines.Add(s + ".");
            }
            if (experienced.Lines.Count == 0) experienced.Lines.Add(TextNoneYet);

            // 4) + 5) not tried enough, and what could come next
            var untried = new List<JobInsight>();
            var little = new List<JobInsight>();
            foreach (var j in insight.Jobs)
            {
                if (j.Attempts == 0) untried.Add(j);
                else if (j.Attempts < MinAttemptsForChoice) little.Add(j);
            }
            foreach (var j in untried) notTried.Lines.Add("«" + j.NameFa + "» هنوز امتحان نشده است.");
            foreach (var j in little) notTried.Lines.Add("«" + j.NameFa + "» فقط یک بار امتحان شده است.");
            if (notTried.Lines.Count == 0) notTried.Lines.Add("همهٔ حرفه‌ها دست‌کم چند بار امتحان شده‌اند.");

            var suggest = new List<JobInsight>();
            foreach (var j in untried) { if (suggest.Count < 2) suggest.Add(j); }
            foreach (var j in little) { if (suggest.Count < 2) suggest.Add(j); }
            if (suggest.Count > 0)
            {
                var names = new List<string>();
                foreach (var j in suggest) names.Add("«" + j.NameFa + "»");
                next.Lines.Add("برای شناخت بهتر علاقه‌اش، می‌تواند فعالیت‌های " + string.Join(" و ", names.ToArray()) + " را هم امتحان کند.");
            }
            else
            {
                next.Lines.Add("می‌تواند هر حرفه‌ای را دوباره و آزادانه انتخاب کند؛ تکرار یک حرفه یا امتحان فعالیت اختیاری آن هم کمک می‌کند.");
            }
            return r;
        }
    }
}
