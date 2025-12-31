using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(GraveyardBossRoom))]
    internal static class GraveyardBossRoomPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly ISpawnedObjectManagerService spawnedObjectManagerService = Plugin.Services.GetService<ISpawnedObjectManagerService>();

        /// <summary>
        /// Set another custom world size specifically because the boss spawn low enough to be outside normal world bounds
        /// Also register all lamps and the boss leave interactable for synchronization
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GraveyardBossRoom.Activate))]
        public static void Activate_Prefix(GraveyardBossRoom __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            Plugin.Instance.SetWorldSize(new UnityEngine.Vector3(7500f, 7500f, 7500f));

            var isHost = synchronizationService.IsServerMode() ?? false;

            //Should be the same order across all players
            foreach (var lamp in __instance.lamps)
            {
                uint netplayId = 0;
                if (isHost)
                {
                    netplayId = spawnedObjectManagerService.AddSpawnedObject(lamp.gameObject);
                }
                else
                {
                    netplayId = spawnedObjectManagerService.SetSpawnedObjectAtLast(lamp.gameObject);
                }

                if (netplayId == 0)
                {
                    Plugin.Log.LogWarning("Failed to assign netplayId to graveyard boss room lamp!");
                    return;
                }
                DynamicData.For(lamp.gameObject).Set("netplayId", netplayId);
            }

            uint bossLeaveNetplayId = 0;
            if (isHost)
            {
                bossLeaveNetplayId = spawnedObjectManagerService.AddSpawnedObject(__instance.interactableGhostBossLeave.gameObject);
            }
            else
            {
                bossLeaveNetplayId = spawnedObjectManagerService.SetSpawnedObjectAtLast(__instance.interactableGhostBossLeave.gameObject);
            }

            if (bossLeaveNetplayId == 0)
            {
                Plugin.Log.LogWarning("Failed to assign netplayId to graveyard boss room boss leave!");
                return;
            }

            DynamicData.For(__instance.interactableGhostBossLeave.gameObject).Set("netplayId", bossLeaveNetplayId);
        }
    }
}
