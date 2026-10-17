using System;
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
                int pick = Math.Max(0, s.NextAcceptedIndex());
                var r = s.Choose(pick);
                Assert.IsTrue(r.Accepted || r.Placed);
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
                if (pick < 0) pick = Math.Max(0, s.NextAcceptedIndex());
                var r = s.Choose(pick);
                Assert.IsTrue(r.Accepted || r.Placed);
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
                    if (m.id == "farm_05_open_farm") break;
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

        [Test]
        public void TheChosenMedicalCaseIsFollowedByEveryLaterDoctorMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "case=scratch", "case=fever", "case=sprain" })
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
                        bool followsCase = pastChoice && m.jobId == "doctor" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsCase ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsCase && tag != "case=scratch")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen case must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsCase) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "med_01_first_aid" ? tag : null);
                            if (followsCase && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the case scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "med_01_first_aid") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "med_05_open_clinic") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }

        [Test]
        public void TheChosenStoreIsFollowedByEveryLaterMerchantMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "store=home", "store=healthy_food", "store=produce" })
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
                        bool followsStore = pastChoice && m.jobId == "merchant" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsStore ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsStore && tag != "store=produce")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen store must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsStore) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "market_01_crates" ? tag : null);
                            if (followsStore && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the store scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "market_01_crates") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "market_05_open_market") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }

        [Test]
        public void TeacherClassManagementHasADirectOrderingActivityInEveryTopic()
        {
            var db = LoadDb();
            var acts = LoadActs();
            var def = acts.Find("teach_04_lesson_board", "lesson_board");
            if (def == null)
            {
                foreach (var o in db.missions.Find(m => m.id == "teach_04_lesson_board").objectives)
                    if (o.ParsedType() == ObjectiveType.Interact) def = acts.Find("teach_04_lesson_board", o.targetId);
            }
            Assert.IsNotNull(def);
            var sets = new List<List<ActivityScene>> { def.scenes };
            foreach (var v in def.variants) sets.Add(v.scenes);
            Assert.GreaterOrEqual(sets.Count, 3);
            foreach (var scenes in sets)
            {
                bool found = false;
                foreach (var sc in scenes)
                    foreach (var st in sc.steps)
                        if (st.ordered && st.options.Count >= 3) found = true;
                Assert.IsTrue(found, "every topic needs a direct ordering step in class management");
            }
        }

        [Test]
        public void EveryOtherJobHasADirectOrderingActivityInBaseAndInEveryBranch()
        {
            var acts = LoadActs();
            var expected = new[]
            {
                new[] { "med_02_supplies", "first_aid_case" },
                new[] { "fire_04_alarms", "fire_alarm" },
                new[] { "farm_02_soil", "farm_soil" },
                new[] { "market_05_open_market", "market_session" },
                new[] { "eng_03_repair_school", "school_fix" },
                new[] { "vet_04_checkup", "pet_checkup" },
                new[] { "transit_02_parcels", "parcel_bay" },
                new[] { "env_02_cleanup", "cleanup_station" },
                new[] { "tech_05_open_lab", "lab_session" },
            };
            foreach (var e in expected)
            {
                var def = acts.Find(e[0], e[1]);
                Assert.IsNotNull(def, e[0]);
                var sets = new List<List<ActivityScene>> { def.scenes };
                foreach (var v in def.variants) sets.Add(v.scenes);
                Assert.GreaterOrEqual(sets.Count, 3, e[0] + ": base and every branch must be checked");
                foreach (var scenes in sets)
                {
                    bool found = false;
                    foreach (var sc in scenes)
                        foreach (var st in sc.steps)
                            if (st.ordered && st.options.Count >= 3) found = true;
                    Assert.IsTrue(found, e[0] + ": every branch needs a direct ordering step");
                }
            }
        }

        [Test]
        public void TheChosenHazardIsFollowedByEveryLaterFirefighterMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "hazard=kitchen", "hazard=electric", "hazard=station" })
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
                        bool followsHazard = pastChoice && m.jobId == "firefighter" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsHazard ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsHazard && tag != "hazard=station")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen hazard must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsHazard) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "fire_01_helmets" ? tag : null);
                            if (followsHazard && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the hazard scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "fire_01_helmets") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "fire_05_open_station") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }

        [Test]
        public void TheChosenFocusIsFollowedByEveryLaterRangerMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "focus=river", "focus=compost", "focus=recycle" })
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
                        bool followsFocus = pastChoice && m.jobId == "environment" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsFocus ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsFocus && tag != "focus=recycle")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen choice must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsFocus) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "env_01_gloves" ? tag : null);
                            if (followsFocus && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the chosen scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "env_01_gloves") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "env_05_open_station") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }

        [Test]
        public void TheChosenServiceIsFollowedByEveryLaterDriverMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "service=school", "service=fresh", "service=city" })
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
                        bool followsService = pastChoice && m.jobId == "transport" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsService ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsService && tag != "service=city")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen choice must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsService) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "transit_01_passes" ? tag : null);
                            if (followsService && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the chosen scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "transit_01_passes") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "transit_05_open_terminal") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }

        [Test]
        public void TheChosenDeviceIsFollowedByEveryLaterTechMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "device=robot", "device=computer", "device=solar" })
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
                        bool followsDevice = pastChoice && m.jobId == "tech" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsDevice ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsDevice && tag != "device=solar")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen choice must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsDevice) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "tech_01_parts" ? tag : null);
                            if (followsDevice && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the chosen scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "tech_01_parts") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "tech_05_open_lab") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }

        [Test]
        public void TheChosenSiteIsFollowedByEveryLaterEngineerMission()
        {
            var db = LoadDb();
            var acts = LoadActs();
            foreach (var tag in new[] { "site=roof", "site=water", "site=wall" })
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
                        bool followsSite = pastChoice && m.jobId == "engineer" && def.variants != null && def.variants.Count > 0;
                        int expectedVariant = followsSite ? def.VariantIndexFor(w.Save.choiceTags) : -1;
                        if (followsSite && tag != "site=wall")
                        {
                            Assert.GreaterOrEqual(expectedVariant, 0, m.id + ": the chosen site must change this mission (" + tag + ")");
                            Assert.AreEqual(tag, def.variants[expectedVariant].tag, m.id);
                            Assert.GreaterOrEqual(def.variants[expectedVariant].scenes.Count, o.RequiredCount, m.id);
                        }
                        for (int order = 0; order < o.RequiredCount; order++)
                        {
                            var session = w.Activities.Begin(m.id, o.targetId, order);
                            Assert.IsNotNull(session, m.id + " spot " + order);
                            if (followsSite) Assert.AreEqual(expectedVariant, session.VariantIndex, m.id + " spot " + order);
                            SolvePreferringTag(session, m.id == "eng_01_toolbox" ? tag : null);
                            if (followsSite && expectedVariant >= 0)
                                Assert.IsNotNull(w.Save.GetActivityRecord(m.id, o.targetId, 100 * (expectedVariant + 1) + order, false),
                                    m.id + " spot " + order + ": the site scene must be saved under its own key");
                        }
                    }
                    Assert.AreEqual(MissionStatus.Completed, w.Missions.GetStatus(m.id), m.id);
                    if (m.id == "eng_01_toolbox") { pastChoice = true; Assert.IsTrue(w.Save.choiceTags.Contains(tag), tag); }
                    if (m.id == "eng_05_open_school") break;
                }
                Assert.IsTrue(pastChoice, tag);
            }
        }
            /// <summary>Shipped data: the teacher's "help a student" activity carries reactions and the support detour.</summary>
        [Test]
        public void ShippedStudentHelpHasReactionsSupportStepAndStudentNames()
        {
            var acts = LoadActs();
            var def = acts.Find("teach_03_garden", "student_help");
            Assert.IsNotNull(def);
            Assert.IsTrue(def.HasReactions, "reactions missing in activities.json");
            Assert.IsTrue(def.HasSupport, "supportStep missing in activities.json");
            foreach (var sc in def.scenes) Assert.IsFalse(string.IsNullOrEmpty(sc.studentFa), "scene without studentFa: " + sc.titleFa);
            foreach (var v in def.variants)
                foreach (var sc in v.scenes) Assert.IsFalse(string.IsNullOrEmpty(sc.studentFa), "variant scene without studentFa: " + sc.titleFa);
            Assert.AreEqual(0, ActivityDatabaseValidator.Validate(acts, LoadDb()).Count);
        }

        /// <summary>Shipped data played for real: two wrong picks start the detour, the right encouragement ends it.</summary>
        [Test]
        public void ShippedStudentHelpTakesTheSupportDetourAfterTwoMistakes()
        {
            var db = LoadDb();
            var acts = LoadActs();
            var def = acts.Find("teach_03_garden", "student_help");
            Assert.IsNotNull(def);
            var w = Create(db, acts);
            // bring the teacher chain up to the mission that contains the student activity
            string target = "teach_03_garden";
            var chain = new List<string>();
            var cur = target;
            while (!string.IsNullOrEmpty(cur))
            {
                chain.Insert(0, cur);
                MissionDefinition m = null;
                foreach (var x in db.missions) if (x.id == cur) m = x;
                cur = m == null ? null : m.requiresMissionId;
            }
            foreach (var id in chain)
            {
                Assert.IsTrue(w.Missions.Start(id), "cannot start " + id);
                if (id == target) break;
                MissionDefinition m = null;
                foreach (var x in db.missions) if (x.id == id) m = x;
                foreach (var o in m.objectives)
                {
                    for (int k = 0; k < o.RequiredCount; k++)
                    {
                        if (o.ParsedType() == ObjectiveType.Interact && acts.Has(id, o.targetId))
                        {
                            var ss = w.Activities.Begin(id, o.targetId, k);
                            Assert.IsNotNull(ss, id + "/" + o.targetId);
                            Solve(ss);
                        }
                        else w.Missions.Notify(o.ParsedType(), o.targetId);
                    }
                }
            }
            // objectives of the target mission that come before the student activity
            var tm = db.missions.Find(x => x.id == target);
            foreach (var o in tm.objectives)
            {
                if (o.ParsedType() == ObjectiveType.Interact && o.targetId == "student_help") break;
                for (int k = 0; k < o.RequiredCount; k++) w.Missions.Notify(o.ParsedType(), o.targetId);
            }
            var s = w.Activities.Begin(target, "student_help", 0);
            Assert.IsNotNull(s);
            s.Choose(s.WrongIndex());
            var second = s.Choose(s.WrongIndex());
            Assert.IsTrue(second.SupportStarted);
            Assert.IsTrue(s.InSupport);
            Assert.IsFalse(string.IsNullOrEmpty(second.ReactionFa));
            Assert.IsFalse(second.ReactionFa.Contains("{name}"));
            var kind = s.Choose(s.NextAcceptedIndex());
            Assert.IsTrue(kind.SupportDone);
            Assert.IsFalse(s.InSupport);
            Solve(s);
            Assert.IsTrue(s.Finished);
        }
    }
}
