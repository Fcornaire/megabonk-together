using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches.ConstantAttacks
{
    [HarmonyPatch(typeof(ProjectileDragonsBreath))]
    internal static class ProjectileDragonBreathPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Queue net player position used for the attack spawn position
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileDragonsBreath.Update))]
        public static void Prefix_Update(ProjectileDragonsBreath __instance)
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
            var playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
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
        [HarmonyPatch(nameof(ProjectileDragonsBreath.Update))]
        public static void Postfix_Update(ProjectileDragonsBreath __instance)
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
            var playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
            var ownerPlayer = playerManagerService.GetPlayer(ownerId.Value);
            var localPlayer = playerManagerService.GetLocalPlayer();
            if (ownerPlayer.ConnectionId != localPlayer.ConnectionId)
            {
                playerManagerService.UnqueueNetplayerPositionRequest();
            }
        }
    }
}
