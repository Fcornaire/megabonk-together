using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory.Stats;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Enemies
{
    [HarmonyPatch(typeof(EnemyStats))]
    internal static class EnemyStatsPatches
    {
        private static readonly IGameBalanceService gameBalanceService = Plugin.Services.GetService<IGameBalanceService>();
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Adjust enemy HP based on netplay configuration
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(EnemyStats.GetHp))]
        private static void GetHpPostfix(Enemy enemy, ref float __result)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer)
            {
                return;
            }

            var multiplier = gameBalanceService.GetEnemyHpMultiplier(enemy.enemyFlag);
            __result *= multiplier;
        }
    }
}
