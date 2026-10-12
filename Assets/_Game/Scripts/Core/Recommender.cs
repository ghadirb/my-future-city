using System.Collections.Generic;

namespace MyFutureCity.Core
{
    public class Suggestion
    {
        public string MissionId;
        public string ReasonFa;
    }

    /// <summary>
    /// Local, rule-based suggestions from gameplay history only. Never claims to measure talent
    /// or personality; wording is deliberately soft ("it seems…", "maybe…").
    /// </summary>
    public class Recommender
    {
        public const string ReasonContinue = "مأموریت نیمه‌تمامت منتظر توست.";
        public const string ReasonFavorite = "به نظر می‌رسد از این فعالیت بیشتر خوشت آمده است.";
        public const string ReasonNew = "شاید این مأموریت را هم دوست داشته باشی.";
        public const string ReasonRepeat = "می‌خواهی دوباره تمرینش کنی؟";

        private readonly MissionDatabase db;
        private readonly MissionService missions;

        public Recommender(MissionDatabase db, MissionService missions)
        {
            this.db = db;
            this.missions = missions;
        }

        public Suggestion Suggest(ICollection<string> dismissed)
        {
            var active = missions.ActiveMission;
            if (active != null)
                return new Suggestion { MissionId = active.id, ReasonFa = ReasonContinue };

            // Favourite job = the one with the most completed runs.
            string favJob = null;
            int best = 0;
            foreach (var j in db.jobs)
            {
                int total = 0;
                foreach (var m in db.missions)
                    if (m.jobId == j.id) total += missions.TimesCompleted(m.id);
                if (total > best) { best = total; favJob = j.id; }
            }

            if (favJob != null)
            {
                foreach (var m in db.missions)
                {
                    if (m.jobId != favJob || IsDismissed(dismissed, m.id)) continue;
                    if (missions.CanStart(m.id) && missions.GetStatus(m.id) == MissionStatus.Available)
                        return new Suggestion { MissionId = m.id, ReasonFa = ReasonFavorite };
                }
            }

            foreach (var m in db.missions)
            {
                if (IsDismissed(dismissed, m.id)) continue;
                if (missions.GetStatus(m.id) == MissionStatus.Available)
                    return new Suggestion { MissionId = m.id, ReasonFa = ReasonNew };
            }

            foreach (var m in db.missions)
            {
                if (IsDismissed(dismissed, m.id)) continue;
                if (m.repeatable && missions.GetStatus(m.id) == MissionStatus.Completed)
                    return new Suggestion { MissionId = m.id, ReasonFa = ReasonRepeat };
            }
            return null;
        }

        private static bool IsDismissed(ICollection<string> dismissed, string id)
        {
            return dismissed != null && dismissed.Contains(id);
        }
    }
}
