using Assets.Scripts.Utility;
using HarmonyLib;
using MegabonkTogether.Helpers;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using TMPro;
using UnityEngine;
using Utility;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(SpawnPlayerPortal))]
    internal static class SpawnPlayerPortalPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
        private static TextMeshProUGUI synchronizeText;
        private static float dotAnimTimer = 0f;
        private static int dotCount = 0;
        public static Coroutine WaitForLobbyCoroutine;

        /// <summary>
        /// Wait for all players to be ready before starting the game
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SpawnPlayerPortal.StartPortal))]
        public static void StartPortal_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            if (!synchronizationService.IsLobbyReady())
            {
                MyTime.Pause();

                if (WaitForLobbyCoroutine == null)
                {
                    WaitForLobbyCoroutine = CoroutineRunner.Instance.Run(WaitForLobbyReady());
                }
            }

        }

        private static IEnumerator WaitForLobbyReady()
        {
            Plugin.Log.LogInfo("Waiting for lobby to be ready");

            if (synchronizeText == null)
            {
                synchronizeText = new GameObject("synchronizeText").AddComponent<TMPro.TextMeshProUGUI>();
            }

            synchronizeText.enabled = true;
            synchronizeText.transform.SetParent(UiManager.Instance.transform);
            synchronizeText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            synchronizeText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            synchronizeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            synchronizeText.rectTransform.anchoredPosition = new Vector2(0, 0);
            synchronizeText.alignment = TMPro.TextAlignmentOptions.Center;
            synchronizeText.text = "Waiting for other players";
            synchronizeText.fontSize = 48;

            dotAnimTimer = 0f;
            dotCount = 0;

            synchronizationService.TransitionToState(GameEvent.Ready);

            while (!synchronizationService.IsLobbyReady())
            {
                dotAnimTimer += Time.unscaledDeltaTime;
                if (dotAnimTimer >= 1f)
                {
                    dotAnimTimer = 0f;
                    dotCount = (dotCount + 1) % 4;
                    string dots = new string('.', dotCount);
                    synchronizeText.text = $"Waiting for other players {dots}";
                }

                Plugin.Log.LogInfo("Lobby not ready yet, waiting...");

                yield return new WaitForSeconds(0.17f);
            }

            Plugin.Log.LogInfo("Lobby is ready, starting the game");

            synchronizationService.TransitionToState(GameEvent.Start);
            var seed = playerManagerService.GetSeed();
            MyRandom.random = new Il2CppSystem.Random(seed);

            synchronizeText.enabled = false;
            WaitForLobbyCoroutine = null;

            MyTime.Unpause();
        }
    }
}
