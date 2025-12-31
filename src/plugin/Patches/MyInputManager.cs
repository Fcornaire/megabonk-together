using HarmonyLib;
using MegabonkTogether.Common;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(MyInputManager))]
    internal static class MyInputManagerPatches
    {
        private static readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();

        /// <summary>
        /// Prevent input when an update is available
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyInputManager.GetButtonDown))]
        public static bool GetButtonDown_Prefix(ref bool __result)
        {
            if (autoUpdaterService.IsAnUpdateAvailable())
            {
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prevent input when an update is available
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyInputManager.GetButtonUp))]
        public static bool GetButtonUp_Prefix(ref bool __result)
        {
            if (autoUpdaterService.IsAnUpdateAvailable())
            {
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prevent input when an update is available
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyInputManager.GetButton))]
        public static bool GetButton_Prefix(ref bool __result)
        {
            if (autoUpdaterService.IsAnUpdateAvailable())
            {
                __result = false;
                return false;
            }
            return true;
        }

    }


}
