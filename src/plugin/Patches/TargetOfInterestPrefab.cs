using Assets.Scripts.Actors.Enemies;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(TargetOfInterestPrefab))]
    internal static class TargetOfInterestPrefabPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IEnemyManagerService enemyManagerService = Plugin.Services.GetService<IEnemyManagerService>();

        /// <summary>
        /// Use custom enemy name for reviver enemies
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(TargetOfInterestPrefab.SetEnemy))]
        public static void Update_Prefix(TargetOfInterestPrefab __instance, Enemy enemy)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (enemy == null)
            {
                return;
            }

            var netplayName = enemyManagerService.GetReviverEnemy_Name(enemy);

            if (!string.IsNullOrEmpty(netplayName))
            {
                __instance.t_name.text = netplayName;
            }

        }
    }
}
