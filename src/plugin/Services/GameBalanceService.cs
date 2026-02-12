using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using System.Linq;

namespace MegabonkTogether.Services
{
    public interface IGameBalanceService
    {
        public float GetCreditsTimerMultiplier();
        public float GetEnemyHpMultiplier(EEnemyFlag enemyFlag);
        public float GetFreeChestSpawnRateMultiplier();
        public int GetPickupXpValue();
        public void Initialize();
        public int GetMaxEnemiesSpawnable();
        public float GetBossLampRequiredCharge();
    }

    public enum DifficultyLevel
    {
        None,
        Duo,
        Trio,
        Quad,
        Five,
        Six
    }

    internal class GameBalanceService(IPlayerManagerService playerManagerService) : IGameBalanceService
    {
        private const float hpScalingPerAdditionalPlayer = 0.1f;
        private int PlayersCount => playerManagerService.GetAllPlayersAlive().Count();
        private static int StageIndex => MapController.runConfig?.mapData.stages.IndexOf(MapController.currentStage) ?? 0;
        private const float baseBossLampInitialChargeTimeSeconds = 3.0f;


        public int GetMaxEnemiesSpawnable()
        {
            if (GameManager.Instance.IsFinalSwarm())
            {
                return 400; // Keep the original cap during Final Swarm
            }

            return GetDifficultyLevelByPlayers() switch
            {
                DifficultyLevel.Quad or DifficultyLevel.Five or DifficultyLevel.Six => 600,
                _ => 500,
            };
        }

        public float GetCreditsTimerMultiplier()
        {
            float baseMultiplier = GetDifficultyLevelByPlayers() switch
            {
                DifficultyLevel.Duo => 1.01f,
                DifficultyLevel.Trio => 1.02f,
                DifficultyLevel.Quad => 1.03f,
                DifficultyLevel.Five => 1.04f,
                DifficultyLevel.Six => 1.05f,
                _ => 1.0f,
            };

            float stageMultiplier = StageIndex switch
            {
                0 => 1.0f,
                1 => 1.05f,
                2 => 1.07f,
                _ => 1.0f
            };

            return baseMultiplier * stageMultiplier;
        }

        public float GetEnemyHpMultiplier(EEnemyFlag enemyFlag)
        {
            float baseMultiplier = enemyFlag switch
            {
                EEnemyFlag.Boss => 1.2f,
                EEnemyFlag.Elite => 1.05f,
                EEnemyFlag.FinalBoss => 1.25f,
                EEnemyFlag.SummonerMiniboss => 1.1f,
                EEnemyFlag.AnyBoss => 1.1f,
                EEnemyFlag.Challenge => 1.15f,
                _ => 1f
            };

            float playerScaling = 1f + (PlayersCount - 1) * hpScalingPerAdditionalPlayer;

            float stageMultiplier = StageIndex switch
            {
                0 => 1.0f,
                1 => 1.1f,
                2 => 1.2f,
                _ => 1.0f
            };

            return baseMultiplier * playerScaling * stageMultiplier;
        }

        public float GetFreeChestSpawnRateMultiplier()
        {
            float baseMultiplier = GetDifficultyLevelByPlayers() switch
            {
                DifficultyLevel.Duo => 1.5f,
                DifficultyLevel.Trio => 1.8f,
                DifficultyLevel.Quad => 2.0f,
                DifficultyLevel.Five => 2.2f,
                DifficultyLevel.Six => 2.4f,
                _ => 1f,
            };

            float stageMultiplier = StageIndex switch
            {
                0 => 1.0f,
                1 => 1.1f,
                2 => 1.15f,
                _ => 1.0f
            };

            return baseMultiplier * stageMultiplier;
        }

        public int GetPickupXpValue()
        {
            return PlayersCount switch
            {
                1 => 1,
                >= 2 and <= 4 => 2,
                5 => 3,
                _ => 1
            };
        }

        public void Initialize()
        {
            var creditsMultiplier = GetCreditsTimerMultiplier();
            var enemyHpMultiplier = GetEnemyHpMultiplier(EEnemyFlag.None);
            var chestSpawnMultiplier = GetFreeChestSpawnRateMultiplier();
            var xpValue = GetPickupXpValue();

            Plugin.Log.LogInfo($"[GameBalance] Initialized for {PlayersCount} players, Stage {StageIndex + 1}, Difficulty: {GetDifficultyLevelByPlayers()}");
            Plugin.Log.LogInfo($"[GameBalance] Credits Timer Multiplier: {creditsMultiplier:F2}x");
            Plugin.Log.LogInfo($"[GameBalance] Basic Enemy HP Base Multiplier: {enemyHpMultiplier:F2}x");
            Plugin.Log.LogInfo($"[GameBalance] Free Chest Spawn Rate Multiplier: {chestSpawnMultiplier:F2}x");
            Plugin.Log.LogInfo($"[GameBalance] XP Value: {xpValue}");
        }

        private DifficultyLevel GetDifficultyLevelByPlayers()
        {
            return PlayersCount switch
            {
                2 => DifficultyLevel.Duo,
                3 => DifficultyLevel.Trio,
                4 => DifficultyLevel.Quad,
                5 => DifficultyLevel.Five,
                6 => DifficultyLevel.Six,
                _ => DifficultyLevel.None
            };
        }

        public float GetBossLampRequiredCharge()
        {
            return GetDifficultyLevelByPlayers() switch
            {
                DifficultyLevel.Duo => baseBossLampInitialChargeTimeSeconds * 2f,
                DifficultyLevel.Trio => baseBossLampInitialChargeTimeSeconds * 2.5f,
                DifficultyLevel.Quad => baseBossLampInitialChargeTimeSeconds * 3f,
                DifficultyLevel.Five => baseBossLampInitialChargeTimeSeconds * 3.5f,
                DifficultyLevel.Six => baseBossLampInitialChargeTimeSeconds * 4f,
                _ => baseBossLampInitialChargeTimeSeconds,
            };
        }

    }
}
