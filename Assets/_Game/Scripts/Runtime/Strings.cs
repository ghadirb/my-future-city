namespace MyFutureCity
{
    /// <summary>All player-facing Persian strings in one place (ready for later localisation).</summary>
    public static class Strings
    {
        public const string GameTitle = "شهر آینده من";
        public const string Interact = "تعامل";
        public const string Continue = "ادامه بازی";
        public const string NewGame = "بازی جدید";
        public const string Settings = "تنظیمات";
        public const string Exit = "خروج";
        public const string Back = "بازگشت";
        public const string Resume = "ادامه";
        public const string Menu = "منو";
        public const string Missions = "مأموریت\u200Cها";
        public const string MyCity = "شهر من";
        public const string MainMenu = "منوی اصلی";
        public const string Accept = "قبول می\u200Cکنم";
        public const string Later = "بعداً";
        public const string Yes = "بله";
        public const string No = "نه";
        public const string Great = "عالی!";
        public const string GotIt = "فهمیدم";
        public const string Girl = "دختر";
        public const string Boy = "پسر";
        public const string ChooseCharacter = "شخصیتت را انتخاب کن";
        public const string ConfirmNewGame = "بازی جدید پیشرفت قبلی را پاک می\u200Cکند. مطمئنی؟";
        public const string Tutorial = "با جوی\u200Cاستیک سمت چپ راه برو.\nبا کشیدن انگشت روی سمت راست صفحه، دوربین را بچرخان.\nگوی زرد شناور بالای سرِ هدف بعدی را دنبال کن؛ فلش راهنما هم جهت و فاصله را نشان می\u200Cدهد.\nنزدیک آدم\u200Cها و وسیله\u200Cها یک دکمهٔ گرد ظاهر می\u200Cشود؛ آن را بزن.\nاول با استاد بهروز حرف بزن!";
        public const string NoActiveMission = "مأموریتی فعال نیست";
        public const string TalkToPeople = "به سراغ آدم\u200Cهای شهر برو";
        public const string Guide = "راهنما";
        public const string GuidePrefix = "راهنما: ";
        public const string GuideNearby = "هدف نزدیک است";
        public const string MissionStarted = "مأموریت شروع شد: ";
        public const string MissionDone = "مأموریت انجام شد!";
        public const string Reward = "پاداش: ";
        public const string Coins = "سکه";
        public const string Xp = "امتیاز تجربه";
        public const string Level = "سطح";
        public const string Saved = "پیشرفت ذخیره شد";
        public const string AbandonMission = "رها کردن مأموریت";
        public const string NoBadges = "هنوز نشانی نگرفته\u200Cای.";
        public const string Badges = "نشان\u200Cها: ";
        public const string CityProgress = "آبادانی شهر: ";
        public const string Percent = " درصد";
        public const string DismissSuggestion = "فعلاً نه";
        public const string Suggestion = "پیشنهاد: ";
        public const string SensitivityLess = "حساسیت دوربین کمتر";
        public const string SensitivityMore = "حساسیت دوربین بیشتر";
        public const string Fps30 = "۳۰ فریم (کم\u200Cمصرف)";
        public const string Fps60 = "۶۰ فریم (روان\u200Cتر)";
        public const string MoreWork = "ادامه بده: ";
        public const string NpcIdle = "امروز کاری ندارم. به شهر سر بزن!";
        public const string RepeatOffer = "می\u200Cخواهی دوباره تمرینش کنی؟ پاداشش کمتر است.";

        public static string StatusFa(Core.MissionStatus s)
        {
            switch (s)
            {
                case Core.MissionStatus.Locked: return "قفل";
                case Core.MissionStatus.Available: return "آماده";
                case Core.MissionStatus.Active: return "در حال انجام";
                default: return "انجام شد";
            }
        }

        public static string BuildingStateFa(Core.BuildingState s)
        {
            switch (s)
            {
                case Core.BuildingState.Abandoned: return "متروک";
                case Core.BuildingState.Renovating: return "در حال بازسازی";
                case Core.BuildingState.Restored: return "بازسازی\u200Cشده";
                default: return "فعال";
            }
        }
    }
}
