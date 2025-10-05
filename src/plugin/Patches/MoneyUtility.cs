using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(MoneyUtility))]
    internal static class MoneyUtilityPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Skip money flying if enemy was killed by another player
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MoneyUtility.OnEnemyDied))]
        public static bool OnEnemyDied_Prefix(Enemy enemy, DamageContainer deathSource)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var ownerId = DynamicData.For(deathSource).Get<uint?>("ownerId");

            if (ownerId.HasValue && playerManagerService.IsRemoteConnectionId(ownerId.Value))
            {
                return false;
            }

            return true;
        }

    }
}
