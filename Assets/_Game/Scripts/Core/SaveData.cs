using System;
using System.Collections.Generic;

namespace MyFutureCity.Core
{
    [Serializable]
    public class MissionProgress
    {
        public string id;
        public bool active;
        public int timesCompleted;
        public List<int> objectiveCounts = new List<int>();
    }

    [Serializable]
    public class BuildingProgress
    {
        public string id;
        public int progress;
    }

    [Serializable]
    public class SettingsData
    {
        public float cameraSensitivity = 1f;
        public int targetFps = 30;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string characterId = "";
        public string lastScene = "";
        public bool tutorialDone;
        public int coins;
        public int xp;
        public List<MissionProgress> missions = new List<MissionProgress>();
        public List<string> badges = new List<string>();
        public List<BuildingProgress> buildings = new List<BuildingProgress>();
        public SettingsData settings = new SettingsData();

        public MissionProgress GetMission(string id, bool create)
        {
            if (missions == null) missions = new List<MissionProgress>();
            for (int i = 0; i < missions.Count; i++)
                if (missions[i] != null && missions[i].id == id) return missions[i];
            if (!create) return null;
            var p = new MissionProgress { id = id };
            missions.Add(p);
            return p;
        }

        public BuildingProgress GetBuilding(string id, bool create)
        {
            if (buildings == null) buildings = new List<BuildingProgress>();
            for (int i = 0; i < buildings.Count; i++)
                if (buildings[i] != null && buildings[i].id == id) return buildings[i];
            if (!create) return null;
            var b = new BuildingProgress { id = id };
            buildings.Add(b);
            return b;
        }

        /// <summary>
        /// Repairs partial / damaged data so the game never has to crash on a bad save.
        /// Safe to call repeatedly.
        /// </summary>
        public void Sanitize()
        {
            if (version <= 0) version = CurrentVersion;
            if (characterId == null) characterId = "";
            if (lastScene == null) lastScene = "";
            if (coins < 0) coins = 0;
            if (xp < 0) xp = 0;
            if (missions == null) missions = new List<MissionProgress>();
            if (badges == null) badges = new List<string>();
            if (buildings == null) buildings = new List<BuildingProgress>();
            if (settings == null) settings = new SettingsData();
            if (settings.cameraSensitivity < 0.2f || settings.cameraSensitivity > 3f) settings.cameraSensitivity = 1f;
            if (settings.targetFps != 30 && settings.targetFps != 60) settings.targetFps = 30;

            missions.RemoveAll(m => m == null || string.IsNullOrEmpty(m.id));
            buildings.RemoveAll(b => b == null || string.IsNullOrEmpty(b.id));
            badges.RemoveAll(string.IsNullOrEmpty);

            // Drop duplicates (keep the first entry).
            var seenM = new HashSet<string>();
            missions.RemoveAll(m => !seenM.Add(m.id));
            var seenB = new HashSet<string>();
            buildings.RemoveAll(b => !seenB.Add(b.id));
            var seenBadge = new HashSet<string>();
            badges.RemoveAll(b => !seenBadge.Add(b));

            foreach (var m in missions)
            {
                if (m.objectiveCounts == null) m.objectiveCounts = new List<int>();
                if (m.timesCompleted < 0) m.timesCompleted = 0;
                for (int i = 0; i < m.objectiveCounts.Count; i++)
                    if (m.objectiveCounts[i] < 0) m.objectiveCounts[i] = 0;
            }
            foreach (var b in buildings)
            {
                if (b.progress < 0) b.progress = 0;
                if (b.progress > CityProgressService.MaxProgress) b.progress = CityProgressService.MaxProgress;
            }

            // Only one mission may be active at a time.
            bool foundActive = false;
            foreach (var m in missions)
            {
                if (!m.active) continue;
                if (foundActive) m.active = false;
                foundActive = true;
            }
        }
    }
}
