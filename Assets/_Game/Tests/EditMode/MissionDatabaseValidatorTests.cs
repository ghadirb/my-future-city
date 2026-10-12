using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class MissionDatabaseValidatorTests
    {
        [Test]
        public void FixtureDatabaseIsValid()
        {
            var errors = MissionDatabaseValidator.Validate(TestFixtures.CreateDb());
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors.ToArray()));
        }

        [Test]
        public void DetectsUnknownReferencesAndDuplicates()
        {
            var db = TestFixtures.CreateDb();
            db.missions[1].requiresMissionId = "ghost";
            db.missions[2].jobId = "ghost_job";
            db.missions[3].id = "m1"; // duplicate
            db.missions[0].objectives[0].buildingId = "ghost_building";
            var errors = MissionDatabaseValidator.Validate(db);
            Assert.GreaterOrEqual(errors.Count, 4);
        }

        [Test]
        public void DetectsBadObjectives()
        {
            var db = TestFixtures.CreateDb();
            db.missions[0].objectives[0].type = "fly";
            db.missions[0].objectives[1].count = 0;
            db.missions[1].objectives.Clear();
            var errors = MissionDatabaseValidator.Validate(db);
            Assert.GreaterOrEqual(errors.Count, 3);
        }

        [Test]
        public void DetectsRequirementCycle()
        {
            var db = TestFixtures.CreateDb();
            db.missions[0].requiresMissionId = "m3";
            var errors = MissionDatabaseValidator.Validate(db);
            Assert.IsTrue(errors.Exists(e => e.Contains("cycle")));
        }
    }
}
