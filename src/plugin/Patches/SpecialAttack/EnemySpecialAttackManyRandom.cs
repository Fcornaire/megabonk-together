using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;
using static EnemySpecialAttackManyRandom;

namespace MegabonkTogether.Patches.SpecialAttack
{
    [HarmonyPatch(typeof(_DoAttack_d__8))]
    internal static class EnemySpecialAttackManyRandomPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Intercept projectile to target the correct player instead of the orignal function targeting always the local player.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(_DoAttack_d__8.MoveNext))]
        public static void MoveNext_Prefix(_DoAttack_d__8 __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var targetId = DynamicData.For(__instance.__4__this.enemy).Get<uint?>("targetId");
            if (targetId.HasValue)
            {
                playerManagerService.AddGetNetplayerPositionRequest(targetId.Value);
            }
        }

        /// <summary>
        /// Remove queued request after attack is over.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(_DoAttack_d__8.MoveNext))]
        public static void MoveNext_Postfix(_DoAttack_d__8 __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }
            var targetId = DynamicData.For(__instance.__4__this.enemy).Get<uint?>("targetId");
            if (targetId.HasValue)
            {
                playerManagerService.UnqueueNetplayerPositionRequest();
            }
        }

    }
}
