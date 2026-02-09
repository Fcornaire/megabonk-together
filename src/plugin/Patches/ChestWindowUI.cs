using Assets.Scripts.Utility;
using HarmonyLib;
using MegabonkTogether.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(ChestOpening))]
    internal static class ChestOpeningPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();

        /// <summary>
        /// Skip the opening animation when in a netplay session
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestOpening.OpenChest))]
        public static void OpenChest_Postfix(ChestOpening __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            __instance.skipped = true;
        }
    }


    //TODO: Refacto with LevelUpScreenPatch
    [HarmonyPatch(typeof(ChestWindowUi))]
    internal static class ChestWindowUiPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();
        public static Coroutine CurrentRoutine;
        private static TMPro.TextMeshProUGUI infoText;
        private static MyButton openButton;

        /// <summary>
        /// Didn't find a proper way to hide the open button ¯\_(ツ)_/¯
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestWindowUi.Open))]
        public static void Open_Postfix(ChestWindowUi __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (openButton == null)
            {
                openButton = __instance.b_open;
            }

            __instance.OpenButton();
            __instance.b_open = null;
        }

        /// <summary>
        /// Add a 5 seconds of invulnerability and a warning after opening a chest in netplay
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestWindowUi.OpeningFinished))]
        public static void OpeningFinished_Postfix(ChestWindowUi __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!GameManager.Instance.player.playerInput.CanInput())
            {
                Plugin.Log.LogInfo("Player cannot input, skipping invulnerability after chest opening.");
                return;
            }

            MyTime.Unpause();

            CoroutineRunner.Instance.Stop(CurrentRoutine);
            CurrentRoutine = CoroutineRunner.Instance.Run(Wait5SecBeforeCanTakeDamageAgain(__instance));
        }

        /// <summary>
        /// Remove the invulnerability after closing the chest window on netplay
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestWindowUi.OnClose))]
        public static void OnClose_Postfix(ChestWindowUi __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (CurrentRoutine == null)
            {
                Plugin.Log.LogInfo("No active invulnerability routine, skipping.");
                return;
            }

            __instance.b_open = openButton;

            CoroutineRunner.Instance.Stop(CurrentRoutine);
            CurrentRoutine = CoroutineRunner.Instance.Run(CancelWaitForVulnerabilityAfter1sec());
        }

        private static IEnumerator CancelWaitForVulnerabilityAfter1sec()
        {
            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            if (infoText == null)
            {
                yield break;
            }

            GameManager.Instance.player.isTeleporting = true;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = true;

            infoText.enabled = true;
            infoText.transform.SetParent(UiManager.Instance.encounterWindows.transform);
            infoText.rectTransform.anchorMin = new Vector2(0, 0);
            infoText.rectTransform.anchorMax = new Vector2(0, 0);
            infoText.rectTransform.pivot = new Vector2(0, 0);
            infoText.rectTransform.anchoredPosition = new Vector2(100, 100);
            infoText.alignment = TMPro.TextAlignmentOptions.Center;
            infoText.fontSize = 36;
            infoText.color = Color.white;

            float timer = 0.3f;
            while (timer > 0f)
            {
                int seconds = Mathf.FloorToInt(timer);
                int milliseconds = Mathf.FloorToInt((timer - seconds) * 1000);
                infoText.text = $"You will be vulnerable in {seconds.ToString("D2")}:{milliseconds.ToString("D3")} secondes";
                yield return null;
                timer -= Time.deltaTime;

                if (!Plugin.Instance.IS_MANUAL_INVINCIBLE)
                {
                    infoText.enabled = false;
                    Plugin.Instance.NetPlayersDisplayer.Show();
                    yield break;
                }
            }

            GameManager.Instance.player.isTeleporting = false;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = false;
            infoText.enabled = false;

            Plugin.Instance.NetPlayersDisplayer.Show();
        }

        private static IEnumerator Wait5SecBeforeCanTakeDamageAgain(ChestWindowUi __instance)
        {
            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            if (infoText == null)
            {
                yield break;
            }

            GameManager.Instance.player.isTeleporting = true;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = true;

            Plugin.Instance.NetPlayersDisplayer.Hide();

            infoText.enabled = true;
            infoText.transform.SetParent(__instance.transform);
            infoText.rectTransform.anchorMin = new Vector2(0, 0);
            infoText.rectTransform.anchorMax = new Vector2(0, 0);
            infoText.rectTransform.pivot = new Vector2(0, 0);
            infoText.rectTransform.anchoredPosition = new Vector2(100, 100);
            infoText.alignment = TMPro.TextAlignmentOptions.Center;
            infoText.fontSize = 36;

            float timer = 5f;
            while (timer > 0f)
            {
                int seconds = Mathf.FloorToInt(timer);
                int milliseconds = Mathf.FloorToInt((timer - seconds) * 1000);
                infoText.text = $"You will be vulnerable in {seconds.ToString("D2")}:{milliseconds.ToString("D3")} secondes";
                yield return null;
                timer -= Time.deltaTime;

                if (!Plugin.Instance.IS_MANUAL_INVINCIBLE)
                {
                    infoText.enabled = false;
                    yield break;
                }
            }

            GameManager.Instance.player.isTeleporting = false;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = false;

            infoText.text = "You are now vulnerable!";
            infoText.color = Color.red;
        }
    }
}
