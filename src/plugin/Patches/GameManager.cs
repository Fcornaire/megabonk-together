using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(GameManager))]
    internal static class GameManagerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Use the remote player's inventory when requested instead of local player's inventory
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.GetPlayerInventory))]
        public static bool GetPlayerInventory_Prefix(ref PlayerInventory __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var peaked = playerManagerService.PeakNetplayerPositionRequest();
            if (peaked.HasValue && playerManagerService.IsRemoteConnectionId(peaked.Value))
            {
                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(peaked.Value);
                if (netPlayer == null)
                {
                    return true;
                }

                __result = netPlayer.Inventory;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Update world size for dungeon (Dungeon are outside normal world bounds)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.StartDungeon))]
        public static void StartDungeon_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            Plugin.Instance.SetWorldSize(new UnityEngine.Vector3(5000f, 5000f, 5000f));
        }

        /// <summary>
        /// Restore back original world size when exiting dungeon
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.StopDungeon))]
        public static void StopDungeon_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            Plugin.Instance.SetWorldSize(Plugin.Instance.OriginalWorldSize);
        }

        /// <summary>
        /// Synchronize dungeon timer start for all players
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameManager.Update))]
        public static void Update_Prefix(GameManager __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (Plugin.Instance.HasDungeonTimerStarted)
            {
                return;
            }

            if (!__instance.isDungeonTimerStarted)
            {
                return;
            }

            Plugin.Instance.HasDungeonTimerStarted = true;
            synchronizationService.OnTimerStarted();
        }
    }
}
