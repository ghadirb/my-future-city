using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class RecommenderTests
    {
        [Test]
        public void ActiveMissionIsSuggestedFirst()
        {
            var w = TestFixtures.CreateWorld();
            var r = new Recommender(w.Db, w.Missions);
            w.Missions.Start("m1");
            var s = r.Suggest(null);
            Assert.AreEqual("m1", s.MissionId);
            Assert.AreEqual(Recommender.ReasonContinue, s.ReasonFa);
        }

        [Test]
        public void FavouriteJobGetsNextMission()
        {
            var w = TestFixtures.CreateWorld();
            var r = new Recommender(w.Db, w.Missions);
            TestFixtures.CompleteM1(w);
            var s = r.Suggest(null);
            Assert.AreEqual("m2", s.MissionId);
            Assert.AreEqual(Recommender.ReasonFavorite, s.ReasonFa);
        }

        [Test]
        public void NewPlayerGetsFirstAvailable()
        {
            var w = TestFixtures.CreateWorld();
            var r = new Recommender(w.Db, w.Missions);
            var s = r.Suggest(null);
            Assert.AreEqual("m1", s.MissionId);
            Assert.AreEqual(Recommender.ReasonNew, s.ReasonFa);
        }

        [Test]
        public void DismissedSuggestionsAreSkipped()
        {
            var w = TestFixtures.CreateWorld();
            var r = new Recommender(w.Db, w.Missions);
            var dismissed = new HashSet<string> { "m1" };
            var s = r.Suggest(dismissed);
            Assert.AreEqual("f1", s.MissionId);
        }

        [Test]
        public void ReturnsNullWhenNothingToSuggest()
        {
            var w = TestFixtures.CreateWorld();
            var r = new Recommender(w.Db, w.Missions);
            var dismissed = new HashSet<string> { "m1", "f1" };
            Assert.IsNull(r.Suggest(dismissed));
        }

        [Test]
        public void SuggestionTextNeverClaimsDiagnosis()
        {
            // Wording rule from the design document: soft suggestions only.
            string[] reasons = { Recommender.ReasonContinue, Recommender.ReasonFavorite, Recommender.ReasonNew, Recommender.ReasonRepeat };
            foreach (var t in reasons)
            {
                Assert.IsFalse(t.Contains("استعداد"));
                Assert.IsFalse(t.Contains("هوش"));
                Assert.IsFalse(t.Contains("شخصیت"));
            }
        }
    }
}
