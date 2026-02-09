using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(PlayerCamera))]
    internal class PlayerCameraPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();

        /// <summary>
        /// Intercept camera input to apply it to our camera switcher when following another player
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerCamera.CameraInput))]
        public static bool CameraInput_Prefix(PlayerCamera __instance, Vector3 playerRotation)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (Plugin.Instance.CameraSwitcher.IsFollowingTarget)
            {
                Plugin.Instance.CameraSwitcher.UpdateFromPlayerRotation(playerRotation);
                return false;
            }

            return true;
        }
    }
}
