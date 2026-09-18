using System;
using System.IO;
using UnityEngine;

namespace ZombieApocalypse.Save
{
    /// <summary>
    /// Handles disk persistent I/O with atomic file creation to prevent file corruption.
    /// Uses application persistent data path (Application.persistentDataPath).
    /// </summary>
    public static class SaveFileUtility
    {
        public const int CURRENT_SAVE_VERSION = 1;
        private static readonly string saveFileName = "savegame.json";
        private static readonly string tempFileName = "savegame.json.tmp";
        private static readonly string backupFileName = "savegame.json.bak";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public static string TempFilePath => Path.Combine(Application.persistentDataPath, tempFileName);
        public static string BackupFilePath => Path.Combine(Application.persistentDataPath, backupFileName);

        public static bool Save(SaveData data)
        {
            if (data == null) return false;

            try
            {
                data.saveVersion = CURRENT_SAVE_VERSION;
                data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string json = JsonUtility.ToJson(data, true);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError("[SaveFileUtility] Generated JSON save string is null or empty.");
                    return false;
                }

                // Step 1: Write to temporary save file
                File.WriteAllText(TempFilePath, json);

                // Step 2: Verify temp file exists and has content
                FileInfo tempInfo = new FileInfo(TempFilePath);
                if (!tempInfo.Exists || tempInfo.Length == 0)
                {
                    Debug.LogError("[SaveFileUtility] Temporary save file write failed or produced 0 bytes.");
                    return false;
                }

                // Step 3: Atomic replace / copy to main save file
                if (File.Exists(SaveFilePath))
                {
                    File.Copy(SaveFilePath, BackupFilePath, true);
                    File.Delete(SaveFilePath);
                }

                File.Move(TempFilePath, SaveFilePath);
                Debug.Log($"[SaveFileUtility] Game saved successfully to: {SaveFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileUtility] Save file operation failed with exception: {ex.Message}");
                return false;
            }
        }

        public static SaveData Load(out string errorMessage)
        {
            errorMessage = "";
            if (!HasSave())
            {
                errorMessage = "No save game found.";
                Debug.LogWarning($"[SaveFileUtility] {errorMessage}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrEmpty(json))
                {
                    errorMessage = "Save file is empty.";
                    Debug.LogWarning($"[SaveFileUtility] {errorMessage}");
                    return null;
                }

                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    errorMessage = "Unable to load save data. Invalid JSON structure.";
                    Debug.LogWarning($"[SaveFileUtility] {errorMessage}");
                    return null;
                }

                if (data.saveVersion > CURRENT_SAVE_VERSION)
                {
                    errorMessage = $"Incompatible save version ({data.saveVersion}). Expected <= {CURRENT_SAVE_VERSION}.";
                    Debug.LogWarning($"[SaveFileUtility] {errorMessage}");
                    return null;
                }

                Debug.Log($"[SaveFileUtility] Save file loaded successfully from: {SaveFilePath} (Version {data.saveVersion})");
                return data;
            }
            catch (Exception ex)
            {
                errorMessage = "Unable to load save data. File corrupted or invalid.";
                Debug.LogError($"[SaveFileUtility] Load exception: {ex.Message}");
                return null;
            }
        }

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static bool DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
                if (File.Exists(TempFilePath)) File.Delete(TempFilePath);
                if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath);
                Debug.Log("[SaveFileUtility] Save data deleted.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileUtility] Failed to delete save files: {ex.Message}");
                return false;
            }
        }
    }
}
