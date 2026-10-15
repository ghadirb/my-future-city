using System.Collections.Generic;
using MyFutureCity.Core;
using NUnit.Framework;
using UnityEngine;

namespace MyFutureCity.Tests
{
    /// <summary>
    /// Acceptance test of the master addendum (section 10) for EVERY shipped job, played against the real
    /// missions.json / activities.json: a child cannot finish any mission by collecting or talking alone, and a
    /// full play of the job performs real activities that are saved and show up in the interest report.
    /// </summary>
    [TestFixture]
    public class JobAcceptanceTests
    {
        private static MissionDatabase LoadDb()
        {
            return JsonUtility.FromJson<MissionDatabase>(Resources.Load<TextAsset>("Data/missions").text);
        }

        private static ActivityDatabase LoadActs()
        {
            return JsonUtility.FromJson<ActivityDatabase>(Resources.Load<TextAsset>("Data/activities").text);
        }

        private class World
        {
            public SaveData Save = new SaveData();
            public MissionService Missions;
            public ActivityService Activities;
            public MissionDatabase Db;
        }

        private static World Create(MissionDatabase db, ActivityDatabase acts)
        {
            var w = new World { Db = db };
            var wallet = new Wallet(w.Save);
            var city = new CityProgressService(db, w.Save);
            w.Missions = new MissionService(db, w.Save, wallet, city);
            w.Activities = new ActivityService(acts, w.Save, w.Missions);
            return w;
        }

        private static void Solve(ActivitySession s)
        {
            int guard = 0;
            while (s.CurrentStep != null && guard++ < 50)
            {
                int pick = 0;
                for (int i = 0; i < s.CurrentStep.options.Count; i++)
                    if (s.CurrentStep.IsAccepted(i)) { pick = i; break; }
                Assert.IsTrue(s.Choose(pick).Accepted);
            }
            Assert.IsTrue(s.Finished);
        }

        private static List<string> JobIds(MissionDatabase db)
        {
            var ids = new List<string>();
            foreach (var m in db.missions) if (!ids.Contains(m.jobId)) ids.Add(m.jobId);
            return ids;
        }

        [Test]
        public void EveryJobHasFiveMissionsAndEachNeedsARealActivity()
        {
            var db = LoadDb();
            var acts = LoadActs();
            var jobs = JobIds(db);
            Assert.AreEqual(10, jobs.Count);
            foreach (var job in jobs)
            {
                Assert.AreEqual(5, db.missions.FindAll(m => m.jobId == job).Count, job);
                Assert.AreEqual(0, ActivityDatabaseValidator.MissionsWithoutRealActivity(db, acts, job).Count, job);
            }
        }

        [Test]
        public void CollectingAndTalkingNeverFinishAnyMissionOfAnyJob()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var job in JobIds(db))
            {
                var w = Create(db, acts);
                foreach (var m in db.missions)
                {
                    if (m.jobId != job) continue;
                    Assert.IsTrue(w.Missions.Start(m.id), m.id + " must be startable in order");
                    // try to skip the real work: report every collect and talk objective, in order, repeatedly
                    for (int round = 0; round < 3; round++)
                        foreach (var o in m.objectives)
                        {
                            var type = o.ParsedType();
                            if (type == ObjectiveType.Interact) continue;
                            w.Missions.Notify(type, o.targetId, o.RequiredCount);
                        }
                    Assert.AreNotEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id + " finished without any real activity");
                    // now do the real work so the next mission becomes available
                    PlayMission(w, m);
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                }
            }
        }

        private static void PlayMission(World w, MissionDefinition m)
        {
            foreach (var o in m.objectives)
            {
                var type = o.ParsedType();
                if (type == ObjectiveType.Interact)
                {
                    for (int order = 0; order < o.RequiredCount; order++)
                    {
                        var session = w.Activities.Begin(m.id, o.targetId, order);
                        Assert.IsNotNull(session, m.id + "/" + o.targetId + " spot " + order + " could not be started");
                        Solve(session);
                    }
                }
                else
                {
                    w.Missions.Notify(type, o.targetId, o.RequiredCount);
                }
            }
        }

        [Test]
        public void PlayingAJobPerformsSavesAndReportsRealActivities()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var job in JobIds(db))
            {
                var w = Create(db, acts);
                int expectedScenes = 0;
                foreach (var m in db.missions)
                {
                    if (m.jobId != job) continue;
                    Assert.IsTrue(w.Missions.Start(m.id), m.id);
                    foreach (var o in m.objectives)
                        if (o.ParsedType() == ObjectiveType.Interact) expectedScenes += o.RequiredCount;
                    PlayMission(w, m);
                }

                var stats = w.Save.GetActivityStats(job, false);
                Assert.IsNotNull(stats, job + ": no activity stats were saved");
                Assert.AreEqual(expectedScenes, stats.completed, job + ": scenes performed");
                Assert.Greater(stats.stepsDone, expectedScenes, job + ": more than one decision per scene on average");
                Assert.Greater(w.Save.activityChoices.Count, 0, job + ": decisions must be logged");

                var report = InterestAnalyzer.Analyze(db, w.Save);
                var ji = report.Find(job);
                Assert.IsNotNull(ji, job);
                Assert.AreEqual(expectedScenes, ji.ActivitiesDone, job + ": report must come from the real activity log");
                var text = string.Join(" ", report.LinesFa.ToArray());
                StringAssert.DoesNotContain("استعداد", text, job);

                // survives a save round trip
                var copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(w.Save));
                copy.Sanitize();
                Assert.AreEqual(expectedScenes, copy.GetActivityStats(job, false).completed, job + ": save round trip");
            }
        }

        [Test]
        public void TheChosenTeachingTopicReallyChangesTheLesson()
        {
            var db = LoadDb();
            var acts = LoadActs();
            var teach = acts.Find("teach_02_classroom", "lesson_teach");
            Assert.IsNotNull(teach);
            Assert.GreaterOrEqual(teach.variants.Count, 2, "science and nature lessons must exist");
            foreach (var v in teach.variants)
            {
                var w = Create(db, acts);
                w.Save.SetChoiceTag(v.tag);
                Assert.AreEqual(teach.variants.IndexOf(v), teach.VariantIndexFor(w.Save.choiceTags));
            }
        }

        private static void SolvePreferringTag(ActivitySession s, string tag)
        {
            int guard = 0;
            while (s.CurrentStep != null && guard++ < 50)
            {
                int pick = -1;
                for (int i = 0; i < s.CurrentStep.options.Count; i++)
                    if (tag != null && s.CurrentStep.options[i].setsTag == tag) { pick = i; break; }
                if (pick < 0)
                    for (int i = 0; i < s.CurrentStep.options.Count; i++)
                        if (s.CurrentStep.IsAccepted(i)) { pick = i; break; }
                Assert.IsTrue(s.Choose(pick).Accepted);
            }
            Assert.IsTrue(s.Finished);
        }

        [Test]
        public void TheChosenTopicIsFollowedByEveryLaterTeacherMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "topic=science", "topic=nature" })
            {
                var w = Create(db, acts);
                foreach (var m in db.missions)
                {
                    if (m.jobId != "teacher") continue;
                    Assert.IsTrue(w.Missions.Start(m.id), m.id);
                    foreach (var o in m.objectives)
                    {
                        if (o.ParsedType() != ObjectiveType.Interact)
                        {
                            w.Missions.Notify(o.ParsedType(), o.targetId, o.RequiredCount);
                            continue;
                        }
                        var def = acts.Find(m.id, o.targetId);
                        Assert.IsNotNull(def, m.id);
                        bool isPrep = m.id == "teach_01_books";
                        int expectedVariant = isPrep ? -1 : def.VariantIndexFor(w.Save.choiceTags);
                        if (!isPrep)
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen topic must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, isPrep ? tag : null);
                            if (!isPrep)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the topic scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                }
                Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag);
            }
        }

        [Test]
        public void TheChosenCropIsFollowedByEveryLaterFarmerMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "crop=carrot", "crop=herbs", "crop=tomato" })
            {
                var w = Create(db, acts);
                bool pastChoice = false;
                foreach (var m in db.missions)
                {
                    Assert.IsTrue(w.Missions.Start(m.id), m.id);
                    foreach (var o in m.objectives)
                    {
                        if (o.ParsedType() != ObjectiveType.Interact)
                        {
                            w.Missions.Notify(o.ParsedType(), o.targetId, o.RequiredCount);
                            continue;
                        }
                        var def = acts.Find(m.id, o.targetId);
                        Assert.IsNotNull(def, m.id);
                        bool followsCrop = pastChoice && m.jobId == "farmer" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsCrop ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsCrop && tag != "crop=tomato")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen crop must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsCrop) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "farm_01_seeds" ? tag : null);
                            if (followsCrop && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the crop scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "farm_01_seeds") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "farm_04_harvest") break;
                }
            }
        }

        [Test]
        public void TheChosenPetIsFollowedByEveryLaterVetMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "pet=dog", "pet=cat", "pet=rabbit" })
            {
                var w = Create(db, acts);
                bool pastChoice = false;
                foreach (var m in db.missions)
                {
                    Assert.IsTrue(w.Missions.Start(m.id), m.id);
                    foreach (var o in m.objectives)
                    {
                        if (o.ParsedType() != ObjectiveType.Interact)
                        {
                            w.Missions.Notify(o.ParsedType(), o.targetId, o.RequiredCount);
                            continue;
                        }
                        var def = acts.Find(m.id, o.targetId);
                        Assert.IsNotNull(def, m.id);
                        bool followsPet = pastChoice && m.jobId == "vet" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsPet ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsPet)
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen pet must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsPet) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "vet_01_leashes" ? tag : null);
                            if (followsPet)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the pet scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "vet_01_leashes") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "vet_05_open_clinic") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }
    }
}
