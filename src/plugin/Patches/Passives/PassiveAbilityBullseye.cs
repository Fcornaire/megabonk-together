using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Passives
{
    [HarmonyPatch(typeof(PassiveAbilityBullseye))]
    internal static class PassiveAbilityBullseyePatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Skip Bullseye for net players
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PassiveAbilityBullseye.OnEnemySpawned))]
        private static bool OnEnemySpawned_Prefix(PassiveAbilityBullseye __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }

            if (playerManagerService.IsANetPlayerAbility(__instance))
            {
                return false;
            }

            return true;
        }

    }
}
