using NUnit.Framework;
using StatusWindow.Progression;
using UnityEngine;

namespace StatusWindow.Tests.EditMode
{
    public sealed class GameSaveServiceTests
    {
        private const string TestSaveKey = "StatusWindow.Tests.GameSaveService";
        private GameSaveService saveService;

        [SetUp]
        public void SetUp()
        {
            saveService = new GameSaveService(TestSaveKey);
            saveService.DeleteSave();
        }

        [TearDown]
        public void TearDown()
        {
            saveService.DeleteSave();
        }

        [Test]
        public void TryLoad_WhenPrimarySaveIsValid_LoadsPrimaryPayload()
        {
            saveService.Save(CreateSave(12));

            Assert.That(saveService.TryLoad(out var loaded), Is.True);
            Assert.That(saveService.LastLoadStatus, Is.EqualTo(GameSaveLoadStatus.LoadedPrimary));
            Assert.That(loaded.gold, Is.EqualTo(12));
        }

        [Test]
        public void TryLoad_WhenPrimaryPayloadIsCorrupt_RecoversMostRecentBackup()
        {
            saveService.Save(CreateSave(12));
            saveService.Save(CreateSave(34));
            PlayerPrefs.SetString(TestSaveKey, "not valid json");
            PlayerPrefs.Save();

            Assert.That(saveService.TryLoad(out var loaded), Is.True);
            Assert.That(saveService.LastLoadStatus, Is.EqualTo(GameSaveLoadStatus.RecoveredBackup));
            Assert.That(loaded.gold, Is.EqualTo(12));
            Assert.That(PlayerPrefs.GetString(TestSaveKey), Does.Contain("\"gold\":12"));
        }

        [Test]
        public void TryLoad_WhenAllStoredPayloadsAreInvalid_ReportsInvalidWithoutDeletingThem()
        {
            PlayerPrefs.SetString(TestSaveKey, "invalid");
            PlayerPrefs.SetString($"{TestSaveKey}.backup", "also invalid");
            PlayerPrefs.Save();

            Assert.That(saveService.TryLoad(out _), Is.False);
            Assert.That(saveService.LastLoadStatus, Is.EqualTo(GameSaveLoadStatus.Invalid));
            Assert.That(PlayerPrefs.HasKey(TestSaveKey), Is.True);
            Assert.That(PlayerPrefs.HasKey($"{TestSaveKey}.backup"), Is.True);
        }

        [Test]
        public void TryLoad_WhenPrimaryPayloadMissesRequiredStats_UsesBackupInstead()
        {
            saveService.Save(CreateSave(12));
            saveService.Save(CreateSave(34));
            PlayerPrefs.SetString(TestSaveKey, "{\"version\":14,\"gold\":999}");
            PlayerPrefs.Save();

            Assert.That(saveService.TryLoad(out var loaded), Is.True);
            Assert.That(saveService.LastLoadStatus, Is.EqualTo(GameSaveLoadStatus.RecoveredBackup));
            Assert.That(loaded.gold, Is.EqualTo(12));
        }

        private static GameSaveData CreateSave(int gold)
        {
            return new GameSaveData
            {
                version = GameSaveData.CurrentVersion,
                gold = gold,
                stats = new int[5],
            };
        }
    }
}
