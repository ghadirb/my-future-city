using System;
using System.IO;
using MyFutureCity.Core;
using UnityEngine;

namespace MyFutureCity
{
    /// <summary>Local JSON save with a rolling backup. A damaged file never crashes the game.</summary>
    public static class SaveStore
    {
        private static string MainPath { get { return Path.Combine(Application.persistentDataPath, "save.json"); } }
        private static string BackupPath { get { return Path.Combine(Application.persistentDataPath, "save.bak.json"); } }

        public static SaveData Load()
        {
            SaveData data = TryRead(MainPath);
            if (data == null) data = TryRead(BackupPath);
            if (data == null) data = new SaveData();
            data.Sanitize();
            return data;
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string text = File.ReadAllText(path);
                if (string.IsNullOrEmpty(text) || text.Trim().Length == 0) return null;
                return JsonUtility.FromJson<SaveData>(text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MyFutureCity] Could not read " + path + ": " + e.Message);
                return null;
            }
        }

        public static void Write(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                string tmp = MainPath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(MainPath))
                {
                    File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                }
                File.Move(tmp, MainPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[MyFutureCity] Save failed: " + e.Message);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(MainPath)) File.Delete(MainPath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MyFutureCity] Could not delete save: " + e.Message);
            }
        }
    }
}
