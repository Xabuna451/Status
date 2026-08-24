using StatusWindow.Progression;
using System;
using UnityEngine;

namespace StatusWindow
{
    public enum GameSaveLoadStatus
    {
        Missing,
        LoadedPrimary,
        RecoveredBackup,
        Invalid,
    }

    /// <summary>
    /// Keeps the last known-good save beside the active payload so an interrupted or corrupt
    /// PlayerPrefs write does not force a progression reset.
    /// </summary>
    public sealed class GameSaveService
    {
        private const string DefaultSaveKey = "StatusWindow.GameSave.v1";
        private readonly string saveKey;
        private readonly string backupKey;

        public GameSaveService(string saveKey = DefaultSaveKey)
        {
            if (string.IsNullOrWhiteSpace(saveKey)) throw new ArgumentException("A save key is required.", nameof(saveKey));
            this.saveKey = saveKey;
            backupKey = $"{saveKey}.backup";
        }

        public bool HasSave => PlayerPrefs.HasKey(saveKey) || PlayerPrefs.HasKey(backupKey);
        public GameSaveLoadStatus LastLoadStatus { get; private set; } = GameSaveLoadStatus.Missing;
        public bool LastSaveSucceeded { get; private set; }

        public void Save(GameSaveData saveData)
        {
            LastSaveSucceeded = false;
            if (saveData == null) return;

            try
            {
                var existingJson = PlayerPrefs.GetString(saveKey, string.Empty);
                if (TryDeserialize(existingJson, out _))
                {
                    PlayerPrefs.SetString(backupKey, existingJson);
                }

                saveData.version = GameSaveData.CurrentVersion;
                saveData.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
                PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(saveData));
                PlayerPrefs.Save();
                LastSaveSucceeded = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to save StatusWindow progression: {exception.Message}");
            }
        }

        public bool TryLoad(out GameSaveData saveData)
        {
            LastLoadStatus = GameSaveLoadStatus.Missing;
            if (TryDeserialize(PlayerPrefs.GetString(saveKey, string.Empty), out saveData))
            {
                LastLoadStatus = GameSaveLoadStatus.LoadedPrimary;
                return true;
            }

            if (TryDeserialize(PlayerPrefs.GetString(backupKey, string.Empty), out saveData))
            {
                LastLoadStatus = GameSaveLoadStatus.RecoveredBackup;
                RestorePrimaryFromBackup();
                return true;
            }

            saveData = null;
            LastLoadStatus = HasSave ? GameSaveLoadStatus.Invalid : GameSaveLoadStatus.Missing;
            return false;
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.DeleteKey(backupKey);
            PlayerPrefs.Save();
            LastLoadStatus = GameSaveLoadStatus.Missing;
        }

        private void RestorePrimaryFromBackup()
        {
            try
            {
                PlayerPrefs.SetString(saveKey, PlayerPrefs.GetString(backupKey, string.Empty));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Recovered the backup save, but could not restore the primary payload: {exception.Message}");
            }
        }

        private static bool TryDeserialize(string json, out GameSaveData saveData)
        {
            saveData = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                saveData = JsonUtility.FromJson<GameSaveData>(json);
                return saveData != null &&
                       saveData.version >= 1 &&
                       saveData.version <= GameSaveData.CurrentVersion &&
                       saveData.stats != null &&
                       saveData.stats.Length == Enum.GetValues(typeof(StatType)).Length;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is ArgumentNullException)
            {
                return false;
            }
        }
    }
}
