using Assets.Scripts.Game.Spawning.New;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Summoner
{
    [HarmonyPatch(typeof(BaseSummoner))]
    internal static class BaseSummonerPatches
    {
        private static readonly IGameBalanceService gameBalanceService = Plugin.Services.GetService<IGameBalanceService>();
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Adjust summoner credit gain rate based on netplay configuration
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BaseSummoner.Tick))]
        private static void Tick_Postfix(BaseSummoner __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer)
            {
                return;
            }

            __instance.giveCreditsTimer *= gameBalanceService.GetCreditsTimerMultiplier();
        }

    }
}
