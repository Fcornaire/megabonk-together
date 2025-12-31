using Assets.Scripts.Game.Combat.ConstantAttacks;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches.ConstantAttacks
{
    [HarmonyPatch(typeof(AegisAttack))]
    internal static class AegisAttackPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        /// <summary>
        /// Prevent update on remote player
        /// </summary>

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AegisAttack.FixedUpdate))]
        public static bool FixedUpdate_Prefix(AegisAttack __instance)
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

            var playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
            var ownerPlayer = playerManagerService.GetPlayer(ownerId.Value);
            var localPlayer = playerManagerService.GetLocalPlayer();
            return ownerPlayer.ConnectionId == localPlayer.ConnectionId;
        }
    }
}
