using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Interactables
{
    [HarmonyPatch(typeof(InteractableTumbleWeed))]
    internal static class InteractableTumbleWeedPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetRequiredService<ISynchronizationService>();


        /// <summary>
        /// Synchronize tumbleweed spawn event
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(InteractableTumbleWeed.OnEnable))]
        public static void OnEnable_Postfix(InteractableTumbleWeed __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return;
            }

            synchronizationService.OnTumbleWeedSpawned(__instance);
        }

        /// <summary>
        /// Synchronize tumbleweed despawn event
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(InteractableTumbleWeed.Despawn))]
        public static bool Despawn_Prefix(InteractableTumbleWeed __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return false;
            }

            synchronizationService.OnTumbleWeedDespawned(__instance);

            return true;
        }

        /// <summary>
        /// Prevent non-hosts from updating tumbleweed
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(InteractableTumbleWeed.FixedUpdate))]
        public static bool FixedUpdate_Prefix(InteractableTumbleWeed __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return false;
            }

            return true;
        }
    }
}
