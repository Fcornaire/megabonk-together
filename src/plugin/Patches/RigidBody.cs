using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(Rigidbody))]
    internal class RigidBodyPatches
    {
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
        /// <summary>
        /// Rigidbody.get_position is used in SpawnPositions.GetEnemySpawnPosition
        /// This is used for spawning enemys closer to a netplayer
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("get_position")]
        public static void Get_position_PostFix(ref Vector3 __result, Transform __instance)
        {
            var netplayerIdNullable = playerManagerService.PeakNetplayerPosition();
            if (netplayerIdNullable.HasValue)
            {
                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(netplayerIdNullable.Value);
                if (netPlayer != null)
                {
                    __result = netPlayer.Model.transform.position;
                }

                return;
            }
        }
    }
}
