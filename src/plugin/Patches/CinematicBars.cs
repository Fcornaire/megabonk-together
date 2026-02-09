using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(CinematicBars))]
    internal class CinematicBarsPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Reset Displayer
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CinematicBars.Hide))]
        public static void Hide_Postfix()
        {
            if (!synchronizationService.HasNetplaySessionStarted()) return;

            Plugin.Instance.NetPlayersDisplayer.ResetCards();

            var allNetPlayers = playerManagerService.GetAllPlayersExceptLocal();
            foreach (var netPlayer in allNetPlayers)
            {
                Plugin.Instance.NetPlayersDisplayer.AddPlayer(netPlayer);
            }
        }
    }

}
