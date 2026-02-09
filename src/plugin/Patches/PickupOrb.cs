using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(PickupOrb))]
    internal static class PickupOrbPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// When pickup collide wiht ground, only the server is allowed to process it
        /// </summary>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PickupOrb.OnCollisionEnter))]
        public static bool OnCollisionEnter_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode();
            if (isServer.HasValue && isServer.Value)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Nothing to by the server here since it will spawn a new pickup ,which is already handled.
        /// We can safely destroy the pickup orb on client
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PickupOrb.OnCollisionEnter))]
        public static void OnCollisionEnter_Postfix(PickupOrb __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer)
            {
                GameObject.Destroy(__instance.gameObject);
            }
        }
    }
}
