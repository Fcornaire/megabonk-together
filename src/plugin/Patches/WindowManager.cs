using HarmonyLib;
using MegabonkTogether.Common;
using MegabonkTogether.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(WindowManager))]
    internal static class WindowManagerPatches
    {
        private static readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();

        /// <summary>
        /// Reset networking and net players displayer when going back to main menu/
        /// Also check for updates
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WindowManager.WindowOpened))]
        public static void WindowOpened_Postfix(Window newWindow)
        {
            if (newWindow.name == "Menu")
            {
                Plugin.Instance.NetworkHandler.ResetNetworking();
                Plugin.Instance.NetPlayersDisplayer.ResetCards();
                Plugin.Instance.HideModal();
                Plugin.Instance.ResetWorldSize();
                Plugin.Instance.CameraSwitcher.ResetToLocalPlayer();
                Plugin.Instance.ClearPrefabs();
                Plugin.Instance.RestoreDeath(false);

                GameObject.Destroy(Plugin.Instance.NetworkTab);
                Plugin.Instance.NetworkTab = null;

                if (SpawnPlayerPortalPatches.WaitForLobbyCoroutine != null)
                {
                    CoroutineRunner.Instance.Stop(SpawnPlayerPortalPatches.WaitForLobbyCoroutine);
                }

                Task.Run(async () =>
                {
                    if (Plugin.Instance.PlayTogetherButton == null)
                    {
                        return;
                    }

                    Plugin.Instance.PlayTogetherButton.enabled = false;
                    await autoUpdaterService.CheckAndUpdate();
                    Plugin.Instance.PlayTogetherButton.enabled = true;

                    if (autoUpdaterService != null && autoUpdaterService.IsAnUpdateAvailable())
                    {
                        Plugin.ShowUpdateAvailableModal();
                    }
                });
            }
        }
    }
}
