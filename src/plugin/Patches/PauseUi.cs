using Assets.Scripts.Utility;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(PauseUi))]
    internal static class PauseUiPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Prevent pause (shared experience or not) on pausing screen
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PauseUi.OnEnable))]
        public static void OnEnable_Postfix(PauseUi __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            MyTime.Unpause();
        }

    }
}
