using Assets.Scripts.Actors.Player;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches.Player
{
    [HarmonyPatch(typeof(MyPlayer))]
    internal class MyPlayerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Use our netplayer player position instead of the default one if needed.
        /// Useful for example with spawning projectiles
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyPlayer.GetFeetPosition))]
        public static bool GetFeetPosition_Prefix(ref Vector3 __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (playerManagerService.TryGetProjectileToSpawn(out uint netplayerId))
            {
                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(netplayerId);

                if (netPlayer == null)
                {
                    return true;
                }
                __result = netPlayer.Model.transform.position;
                return false;
            }

            return true;
        }
    }
}
