using System;
using System.IO;
using UnityEngine;

namespace ZombieApocalypse.Core
{
    /// <summary>
    /// Serializable container for saved player and mission progress data.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public float playerHealth = 100f;
        public float playerStamina = 100f;
        public Vector3 playerPosition;
        public int currentMissionIndex = 0;
        public int equippedWeaponIndex = 0;
    }

    /// <summary>
    /// Handles reading and writing game save files using JSON serialization.
    /// 
    /// ATTACH TO: Shared Manager GameObject or invoked statically.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        /// <summary>
        /// Saves data object to persistent local storage.
        /// </summary>
        public static void SaveGame(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveSystem] Game saved successfully to: {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save game: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads save data from persistent local storage. Returns new SaveData if not found.
        /// </summary>
        public static SaveData LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning("[SaveSystem] Save file not found. Returning default save data.");
                return new SaveData();
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveSystem] Game loaded successfully.");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load save file: {ex.Message}");
                return new SaveData();
            }
        }
    }
}
