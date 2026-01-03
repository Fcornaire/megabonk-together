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
        private static bool hasShownUpdateModal = false;

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

                if (autoUpdaterService.IsThunderstoreBuild() && hasShownUpdateModal) return;

                Task.Run(async () =>
                {
                    await autoUpdaterService.CheckAndUpdate();

                    if (autoUpdaterService.IsAnUpdateAvailable())
                    {
                        Plugin.ShowUpdateAvailableModal();

                        if (autoUpdaterService.IsThunderstoreBuild())
                        {
                            hasShownUpdateModal = true;
                        }

                        if (Plugin.Instance.PlayTogetherButton == null)
                        {
                            return;
                        }
                        Plugin.Instance.PlayTogetherButton.enabled = false;
                    }
                });
            }
        }
    }
}
