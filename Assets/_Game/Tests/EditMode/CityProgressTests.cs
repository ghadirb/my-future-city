using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class CityProgressTests
    {
        [Test]
        public void StateThresholds()
        {
            Assert.AreEqual(BuildingState.Abandoned, CityProgressService.StateFor(0));
            Assert.AreEqual(BuildingState.Renovating, CityProgressService.StateFor(1));
            Assert.AreEqual(BuildingState.Renovating, CityProgressService.StateFor(99));
            Assert.AreEqual(BuildingState.Restored, CityProgressService.StateFor(100));
            Assert.AreEqual(BuildingState.Restored, CityProgressService.StateFor(149));
            Assert.AreEqual(BuildingState.Active, CityProgressService.StateFor(150));
        }

        [Test]
        public void ProgressIsClampedAndEventsFireOnStateChange()
        {
            var w = TestFixtures.CreateWorld();
            int stateChanges = 0, changed = 0;
            w.City.BuildingStateChanged += (id, a, b) => stateChanges++;
            w.City.Changed += () => changed++;

            Assert.IsTrue(w.City.AddProgress("school", 40));
            Assert.AreEqual(1, stateChanges);   // Abandoned -> Renovating
            Assert.IsTrue(w.City.AddProgress("school", 40));
            Assert.AreEqual(1, stateChanges);   // still Renovating
            Assert.IsTrue(w.City.AddProgress("school", 40));
            Assert.AreEqual(2, stateChanges);   // -> Restored
            Assert.AreEqual(3, changed);

            w.City.AddProgress("school", 9999);
            Assert.AreEqual(CityProgressService.MaxProgress, w.City.GetProgress("school"));
            Assert.IsFalse(w.City.AddProgress("school", 5));
        }

        [Test]
        public void UnknownBuildingAndBadAmountsAreIgnored()
        {
            var w = TestFixtures.CreateWorld();
            Assert.IsFalse(w.City.AddProgress("nope", 10));
            Assert.IsFalse(w.City.AddProgress("school", 0));
            Assert.IsFalse(w.City.AddProgress("school", -5));
            Assert.IsFalse(w.City.AddProgress(null, 5));
        }

        [Test]
        public void OverallPercentAveragesBuildings()
        {
            var w = TestFixtures.CreateWorld();
            Assert.AreEqual(0f, w.City.OverallPercent(), 0.0001f);
            w.City.AddProgress("school", 100);
            Assert.AreEqual(0.5f, w.City.OverallPercent(), 0.0001f);
            w.City.AddProgress("clinic", 100);
            w.City.AddProgress("school", 100);   // extra beyond 100 must not inflate the percentage
            Assert.AreEqual(1f, w.City.OverallPercent(), 0.0001f);
        }
    }
}
