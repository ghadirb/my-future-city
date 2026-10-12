using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class MissionServiceTests
    {
        [Test]
        public void FirstMissionIsAvailable_LaterOnesAreLocked()
        {
            var w = TestFixtures.CreateWorld();
            Assert.AreEqual(MissionStatus.Available, w.Missions.GetStatus("m1"));
            Assert.AreEqual(MissionStatus.Locked, w.Missions.GetStatus("m2"));
            Assert.AreEqual(MissionStatus.Locked, w.Missions.GetStatus("m3"));
        }

        [Test]
        public void CannotStartLockedMission()
        {
            var w = TestFixtures.CreateWorld();
            Assert.IsFalse(w.Missions.Start("m2"));
            Assert.IsNull(w.Missions.ActiveMission);
        }

        [Test]
        public void OnlyOneMissionCanBeActive()
        {
            var w = TestFixtures.CreateWorld();
            Assert.IsTrue(w.Missions.Start("m1"));
            Assert.IsFalse(w.Missions.Start("f1"));
            Assert.AreEqual("m1", w.Missions.ActiveMission.id);
        }

        [Test]
        public void ObjectivesAreOrdered_TalkBeforeCollectIsIgnored()
        {
            var w = TestFixtures.CreateWorld();
            w.Missions.Start("m1");
            Assert.IsFalse(w.Missions.Notify(ObjectiveType.Talk, "npc"));   // collect must come first
            Assert.AreEqual(0, w.Missions.CurrentObjectiveIndex);
            Assert.IsTrue(w.Missions.Notify(ObjectiveType.Collect, "tool"));
            Assert.AreEqual(1, w.Missions.CurrentObjectiveIndex);
        }

        [Test]
        public void WrongTargetDoesNotProgress()
        {
            var w = TestFixtures.CreateWorld();
            w.Missions.Start("m1");
            Assert.IsFalse(w.Missions.Notify(ObjectiveType.Collect, "plank"));
            Assert.IsFalse(w.Missions.Notify(ObjectiveType.Interact, "tool"));
        }

        [Test]
        public void CompletingMissionGivesRewardOnce_AndUnlocksNext()
        {
            var w = TestFixtures.CreateWorld();
            TestFixtures.CompleteM1(w);
            Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus("m1"));
            Assert.AreEqual(10, w.Wallet.Coins);
            Assert.AreEqual(20, w.Wallet.Xp);
            Assert.AreEqual(MissionStatus.Available, w.Missions.GetStatus("m2"));
            Assert.IsNull(w.Missions.ActiveMission);
        }

        [Test]
        public void NonRepeatableMissionCannotBeRestartedForReward()
        {
            var w = TestFixtures.CreateWorld();
            TestFixtures.CompleteM1(w);
            Assert.IsFalse(w.Missions.CanStart("m1"));
            Assert.IsFalse(w.Missions.Start("m1"));
            Assert.AreEqual(10, w.Wallet.Coins);
        }

        [Test]
        public void RepeatableMission_GivesOnlyRepeatCoins_NoXpNoBadge()
        {
            var w = TestFixtures.CreateWorld();
            TestFixtures.CompleteM1(w);
            for (int run = 0; run < 2; run++)
            {
                Assert.IsTrue(w.Missions.Start("m2"));
                for (int i = 0; i < 3; i++) Assert.IsTrue(w.Missions.Notify(ObjectiveType.Collect, "plank"));
                Assert.IsTrue(w.Missions.Notify(ObjectiveType.Talk, "npc"));
            }
            // 10 (m1) + 15 (m2 first) + 5 (m2 repeat); xp 20 + 30 only
            Assert.AreEqual(30, w.Wallet.Coins);
            Assert.AreEqual(50, w.Wallet.Xp);
            Assert.AreEqual(2, w.Missions.TimesCompleted("m2"));
        }

        [Test]
        public void CollectCountsUpToRequiredAndNoFurther()
        {
            var w = TestFixtures.CreateWorld();
            TestFixtures.CompleteM1(w);
            w.Missions.Start("m2");
            w.Missions.Notify(ObjectiveType.Collect, "plank");
            w.Missions.Notify(ObjectiveType.Collect, "plank");
            Assert.AreEqual(2, w.Missions.GetObjectiveCount("m2", 0));
            Assert.AreEqual(2, w.Missions.GetCountByTarget("m2", "plank"));
            w.Missions.Notify(ObjectiveType.Collect, "plank");
            Assert.AreEqual(3, w.Missions.GetObjectiveCount("m2", 0));
            Assert.IsFalse(w.Missions.Notify(ObjectiveType.Collect, "plank"));
        }

        [Test]
        public void AbandonClearsRunWithoutPenalty()
        {
            var w = TestFixtures.CreateWorld();
            TestFixtures.CompleteM1(w);
            w.Missions.Start("m2");
            w.Missions.Notify(ObjectiveType.Collect, "plank");
            int coins = w.Wallet.Coins;
            Assert.IsTrue(w.Missions.AbandonActive());
            Assert.IsNull(w.Missions.ActiveMission);
            Assert.AreEqual(coins, w.Wallet.Coins);
            Assert.IsTrue(w.Missions.Start("m2"));
            Assert.AreEqual(0, w.Missions.GetObjectiveCount("m2", 0));
        }

        [Test]
        public void ObjectiveCanAdvanceBuildingAndRewardRestoresIt()
        {
            var w = TestFixtures.CreateWorld();
            TestFixtures.CompleteM1(w);
            w.Missions.Start("m2");
            for (int i = 0; i < 3; i++) w.Missions.Notify(ObjectiveType.Collect, "plank");
            w.Missions.Notify(ObjectiveType.Talk, "npc");

            w.Missions.Start("m3");
            Assert.AreEqual(BuildingState.Abandoned, w.City.GetState("school"));
            w.Missions.Notify(ObjectiveType.Interact, "repair");
            Assert.AreEqual(25, w.City.GetProgress("school"));
            Assert.AreEqual(BuildingState.Renovating, w.City.GetState("school"));
            w.Missions.Notify(ObjectiveType.Interact, "repair");
            w.Missions.Notify(ObjectiveType.Interact, "repair");
            Assert.AreEqual(75, w.City.GetProgress("school"));
            w.Missions.Notify(ObjectiveType.Talk, "npc");

            Assert.AreEqual(BuildingState.Restored, w.City.GetState("school"));
            Assert.IsTrue(w.Save.badges.Contains("badge_eng"));
            Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus("m3"));
        }

        [Test]
        public void CompletedEventCarriesRewardAndFirstTimeFlag()
        {
            var w = TestFixtures.CreateWorld();
            MissionCompletion got = null;
            w.Missions.Completed += c => got = c;
            TestFixtures.CompleteM1(w);
            Assert.IsNotNull(got);
            Assert.IsTrue(got.FirstTime);
            Assert.AreEqual(10, got.Coins);
            Assert.AreEqual("m1", got.Mission.id);
        }

        [Test]
        public void JobCounters()
        {
            var w = TestFixtures.CreateWorld();
            Assert.AreEqual(3, w.Missions.MissionCountForJob("engineer"));
            Assert.AreEqual(0, w.Missions.CompletedCountForJob("engineer"));
            TestFixtures.CompleteM1(w);
            Assert.AreEqual(1, w.Missions.CompletedCountForJob("engineer"));
        }
    }
}
