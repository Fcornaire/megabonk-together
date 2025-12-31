using Assets.Scripts.Utility;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(MyTime))]
    internal static class MyTimePatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// No pause during netplay
        /// </summary>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyTime.Pause))]
        public static bool Pause_Postfix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            return false;
        }

    }
}
