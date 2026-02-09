using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.SpecialAttack
{
    [HarmonyPatch(typeof(EnemySpecialAttackTargetLaser))]
    internal static class EnemySpecialAttackTargetLaserPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Intercept projectile to target the correct player instead of the orignal function targeting always the local player.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EnemySpecialAttackTargetLaser.FixedUpdate))]
        public static void FixedUpdate_Prefix(EnemySpecialAttackTargetLaser __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }
            var targetId = MonoMod.Utils.DynamicData.For(__instance.enemy).Get<uint?>("targetId");
            if (targetId.HasValue)
            {
                playerManagerService.AddGetNetplayerPositionRequest(targetId.Value);
            }
        }

        /// <summary>
        /// Remove queued request after attack is over.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(EnemySpecialAttackTargetLaser.FixedUpdate))]
        public static void FixedUpdate_Postfix(EnemySpecialAttackTargetLaser __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }
            var targetId = MonoMod.Utils.DynamicData.For(__instance.enemy).Get<uint?>("targetId");
            if (targetId.HasValue)
            {
                playerManagerService.UnqueueNetplayerPositionRequest();
            }
        }
    }
}
