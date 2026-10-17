using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class ParentReportTests
    {
        private static TestFixtures.World World()
        {
            var w = TestFixtures.CreateWorld();
            foreach (var m in w.Db.missions) { m.repeatable = true; m.repeatCoins = 1; }
            return w;
        }

        private static string All(ParentReport r)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in r.Sections)
            {
                sb.Append(s.TitleFa).Append('\n');
                foreach (var l in s.Lines) sb.Append(l).Append('\n');
            }
            sb.Append(r.NoteFa);
            return sb.ToString();
        }

        private static ParentSection Find(ParentReport r, string title)
        {
            foreach (var s in r.Sections) if (s.TitleFa == title) return s;
            return null;
        }

        [Test]
        public void NewPlayerGetsNotEnoughInformationEverywhere()
        {
            var w = World();
            var r = ParentReportBuilder.Build(w.Db, w.Save);
            Assert.IsFalse(r.EnoughData);
            Assert.AreEqual(5, r.Sections.Count);
            Assert.AreEqual(ParentReportBuilder.TextNotEnough, Find(r, ParentReportBuilder.TitleMostChosen).Lines[0]);
            Assert.IsNotNull(r.NoteFa);
        }

        [Test]
        public void NullInputsDoNotThrow()
        {
            Assert.IsNotNull(ParentReportBuilder.Build(null, null));
            Assert.IsNotNull(ParentReportBuilder.Build((InsightReport)null));
        }

        [Test]
        public void TooFewRunsNeverNameAJobAsMostChosen()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 60);
            AttemptTrackingTests.Play(w, "f1", 60);
            var r = ParentReportBuilder.Build(w.Db, w.Save);
            Assert.IsFalse(r.EnoughData);
            var most = Find(r, ParentReportBuilder.TitleMostChosen);
            Assert.AreEqual(ParentReportBuilder.TextNotEnoughHint, most.Lines[0]);
            Assert.IsFalse(All(r).Contains("بیشتر انتخاب کرده است"));
        }

        [Test]
        public void EnoughRealPlayNamesTheMostChosenJobWithoutAVerdict()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 120);
            AttemptTrackingTests.Play(w, "m2", 120);
            AttemptTrackingTests.Play(w, "m2", 60);
            AttemptTrackingTests.Play(w, "m1", 60);
            var r = ParentReportBuilder.Build(w.Db, w.Save);
            Assert.IsTrue(r.EnoughData);
            var most = Find(r, ParentReportBuilder.TitleMostChosen);
            Assert.IsTrue(most.Lines[0].Contains("«مهندس»"));
            Assert.IsTrue(most.Lines[0].StartsWith("در این دوره بازی"));
        }

        [Test]
        public void ReturnsAndUntriedJobsAreReportedFromTheLog()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 120);
            AttemptTrackingTests.Play(w, "m2", 120);
            AttemptTrackingTests.Play(w, "m2", 60);
            AttemptTrackingTests.Play(w, "m1", 60);
            var r = ParentReportBuilder.Build(w.Db, w.Save);
            var rep = Find(r, ParentReportBuilder.TitleRepeated);
            Assert.IsTrue(rep.Lines[0].Contains("بار بازگشته است"));
            var not = Find(r, ParentReportBuilder.TitleNotTried);
            Assert.IsTrue(not.Lines[0].Contains("«کشاورز» هنوز امتحان نشده"));
            var next = Find(r, ParentReportBuilder.TitleNext);
            Assert.IsTrue(next.Lines[0].Contains("«کشاورز»"));
            Assert.IsTrue(next.Lines[0].Contains("را هم امتحان کند"));
            var exp = Find(r, ParentReportBuilder.TitleExperienced);
            Assert.IsTrue(exp.Lines[0].Contains("«مهندس»"));
        }

        [Test]
        public void WordingNeverClaimsTalentFutureJobOrComparison()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 120);
            AttemptTrackingTests.Play(w, "m2", 120);
            AttemptTrackingTests.Play(w, "m2", 60);
            AttemptTrackingTests.Play(w, "f1", 30);
            string text = All(ParentReportBuilder.Build(w.Db, w.Save));
            string[] banned = { "استعداد", "هوش", "شخصیت", "ضعیف", "بهتر از", "کودکان دیگر", "همسن", "آیندهٔ شغلی او این است", "باید شغل" };
            foreach (var b in banned) Assert.IsFalse(text.Contains(b), "banned wording: " + b);
            Assert.IsTrue(text.Contains("ارزیابی علمی یا روان‌شناختی نیست"));
        }

        [Test]
        public void ReportIsDeterministicAndFollowsTheRealLog()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 100);
            AttemptTrackingTests.Play(w, "m2", 100);
            AttemptTrackingTests.Play(w, "m2", 50);
            AttemptTrackingTests.Play(w, "m1", 50);
            string a = All(ParentReportBuilder.Build(w.Db, w.Save));
            string b = All(ParentReportBuilder.Build(w.Db, w.Save));
            Assert.AreEqual(a, b);
            // more farmer play changes the report: farmer is no longer "untried"
            AttemptTrackingTests.Play(w, "f1", 100);
            AttemptTrackingTests.Play(w, "f1", 100);
            string c = All(ParentReportBuilder.Build(w.Db, w.Save));
            Assert.IsTrue(a != c);
            Assert.IsFalse(c.Contains("«کشاورز» هنوز امتحان نشده"));
        }

        [Test]
        public void ChildReportSaysNotEnoughInformationWhileDataIsLow()
        {
            var w = World();
            AttemptTrackingTests.Play(w, "m1", 60);
            var r = InterestAnalyzer.Analyze(w.Db, w.Save);
            Assert.AreEqual(DataConfidence.Low, r.Confidence);
            Assert.IsTrue(r.LinesFa.Contains(InterestAnalyzer.TextNotEnough));
            foreach (var l in r.LinesFa) Assert.IsFalse(l.Contains("بیشتر از همه به"));
        }
    }
}
