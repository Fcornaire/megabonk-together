using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(StealWeaponWui))]
    internal static class StealWeaponWuiPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Use remote player position when boss steals their weapon
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(StealWeaponWui.Update))]
        public static void Update_Prefix(StealWeaponWui __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var netPlayers = playerManagerService.GetAllSpawnedNetPlayers();
            foreach (var netPlayer in netPlayers)
            {
                if (netPlayer.StealWeaponWui == __instance)
                {
                    playerManagerService.AddGetNetplayerPositionRequest(netPlayer.ConnectionId);

                    return;
                }
            }
        }

        /// <summary>
        /// Remove the position request after updating
        /// </summary>  
        [HarmonyPostfix]
        [HarmonyPatch(nameof(StealWeaponWui.Update))]
        public static void Update_Postfix(StealWeaponWui __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var netPlayers = playerManagerService.GetAllSpawnedNetPlayers();
            foreach (var netPlayer in netPlayers)
            {
                if (netPlayer.StealWeaponWui == __instance)
                {
                    playerManagerService.UnqueueNetplayerPositionRequest();

                    return;
                }
            }
        }
    }
}
