using Assets.Scripts.Game.Spawning.New.Summoners;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(ChallengeSummoner))]
    internal static class ChallengeSummonerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Only spend credits on server
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChallengeSummoner.SpendCredits))]
        public static bool SpendCredits_Prefix(ChallengeSummoner __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode();
            if (!isServer.HasValue || (isServer.HasValue && !isServer.Value))
            {
                return false;
            }
            return true;
        }
    }
}
