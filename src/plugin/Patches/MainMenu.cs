using HarmonyLib;
using MegabonkTogether.Common;
using MegabonkTogether.Scripts.Button;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(MainMenu))]
    internal static class MainMenuPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetRequiredService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetRequiredService<IPlayerManagerService>();
        private static readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetRequiredService<IAutoUpdaterService>();

        /// <summary>
        /// Add "TOGETHER!" button to main menu
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MainMenu.Start))]
        public static void Start_Postfix(MainMenu __instance)
        {
            var playBtn = __instance.btnPlay;

            var go = UnityEngine.Object.Instantiate(playBtn.gameObject);

            var originalButton = go.GetComponent<MyButtonNormal>();
            if (originalButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalButton);
            }

            UnityEngine.UI.Button button = go.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new();
            }

            var customButton = go.AddComponent<PlayTogetherButton>();
            customButton.SetMainMenu(__instance);

            var textWrapper = go.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = "TOGETHER!";

            var version = new UnityEngine.GameObject("VersionText");
            version.transform.SetParent(textWrapper.t_text.transform);
            version.transform.localPosition = new UnityEngine.Vector3(0, -35, 0);
            version.transform.localScale = UnityEngine.Vector3.one;

            var tmpText = textWrapper.t_text;
            var versionTmp = version.AddComponent<TMPro.TextMeshProUGUI>();
            versionTmp.font = tmpText.font;
            versionTmp.fontSize = tmpText.fontSize * 0.45f;
            versionTmp.alignment = tmpText.alignment;
            versionTmp.color = tmpText.color;
            versionTmp.text = $"v{MyPluginInfo.PLUGIN_VERSION}";


            go.transform.SetParent(playBtn.transform.parent);

            Plugin.Instance.PlayTogetherButton = customButton;
            Plugin.Instance.SetMainMenu(__instance);

            if (autoUpdaterService.IsThunderstoreBuild() && autoUpdaterService.IsAnUpdateAvailable())
            {
                customButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Synchronize the selected character and prevent going to map selection if not host (Wait for host)
        /// </summary>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MainMenu.GoToMapSelection))]
        public static bool GoToMapSelection_Prefix()
        {
            if (!Plugin.Instance.NetworkHandler.HasFoundMatch.HasValue || !Plugin.Instance.NetworkHandler.HasFoundMatch.Value)
            {
                return true;
            }

            if (synchronizationService.IsLoading())
            {
                return false;
            }

            synchronizationService.OnSelectedCharacter();

            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                Plugin.Instance.ShowModal("Waiting for the host...");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Restore "TOGETHER!" button text when going back to main menu (because its disappears for some reason)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MainMenu.GoToMenu))]
        public static void GoToMenu_Postfix()
        {
            if (Plugin.Instance.PlayTogetherButton != null)
            {
                var textWrapper = Plugin.Instance.PlayTogetherButton.GetComponent<ButtonTextWrapper>();
                textWrapper.t_text.text = "TOGETHER!";
            }
        }
    }
}
