using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches.ConstantAttacks
{
    //TODO: Synchronize laser beam attacks properly
    [HarmonyPatch(typeof(LaserBeamAttack))]
    internal static class LaserBeamAttackPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Queue net player position used for the attack spawn position
        /// </summary>
        /// 
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LaserBeamAttack.Update))]
        public static void FixedUpdate_Prefix(LaserBeamAttack __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }
            var ownerId = DynamicData.For(__instance).Get<uint?>("ownerId");
            if (!ownerId.HasValue)
            {
                return;
            }
            var playerManagerService = Plugin.Services.GetService<Services.IPlayerManagerService>();
            var ownerPlayer = playerManagerService.GetPlayer(ownerId.Value);
            var localPlayer = playerManagerService.GetLocalPlayer();
            if (ownerPlayer.ConnectionId != localPlayer.ConnectionId)
            {
                playerManagerService.AddGetNetplayerPositionRequest(ownerPlayer.ConnectionId);
            }
        }

        /// <summary>
        /// Unqueue net player position request after use
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(LaserBeamAttack.Update))]
        public static void FixedUpdate_Postfix(LaserBeamAttack __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }
            var ownerId = DynamicData.For(__instance).Get<uint?>("ownerId");
            if (!ownerId.HasValue)
            {
                return;
            }
            var playerManagerService = Plugin.Services.GetService<Services.IPlayerManagerService>();
            var ownerPlayer = playerManagerService.GetPlayer(ownerId.Value);
            var localPlayer = playerManagerService.GetLocalPlayer();
            if (ownerPlayer.ConnectionId != localPlayer.ConnectionId)
            {
                playerManagerService.UnqueueNetplayerPositionRequest();
            }
        }

        /// <summary>
        /// Prevent remote attacks from hitting enemies
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LaserBeamAttack.HitEnemy))]
        public static bool HitEnemy_Prefix(LaserBeamAttack __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            var ownerId = DynamicData.For(__instance).Get<uint?>("ownerId");
            if (!ownerId.HasValue)
            {
                return true;
            }
            var playerManagerService = Plugin.Services.GetService<Services.IPlayerManagerService>();
            var ownerPlayer = playerManagerService.GetPlayer(ownerId.Value);
            var localPlayer = playerManagerService.GetLocalPlayer();
            return ownerPlayer.ConnectionId == localPlayer.ConnectionId;
        }
    }
}
