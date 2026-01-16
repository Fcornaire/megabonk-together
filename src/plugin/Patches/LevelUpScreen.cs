using Assets.Scripts.Utility;
using HarmonyLib;
using MegabonkTogether.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    //TODO: Refacto with ChestWindowUiPatch and EncounterUiPatch
    [HarmonyPatch(typeof(LevelupScreen))]
    internal static class LevelUpScreenPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();
        public static Coroutine CurrentRoutine;
        private static TMPro.TextMeshProUGUI infoText;

        /// <summary>
        /// Unpause timeStep (To prevent pause) and let the player breath for 5 seconds.
        /// Unless interacting with a shady guy
        /// </summary>

        [HarmonyPostfix]
        [HarmonyPatch(nameof(LevelupScreen.ShowLevelupScreen))]
        public static void ShowLevelupScreen_Postfix(LevelupScreen __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!GameManager.Instance.player.playerInput.CanInput())
            {
                Plugin.Log.LogInfo("Player cannot input, skipping invincibility on level up screen open.");
                return;
            }

            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            if (InteractableShadyGuy.currentlyInteracting != null)
            {
                infoText.enabled = true;
                infoText.transform.SetParent(__instance.transform);
                infoText.rectTransform.anchorMin = new Vector2(0, 0);
                infoText.rectTransform.anchorMax = new Vector2(0, 0);
                infoText.rectTransform.pivot = new Vector2(0, 0);
                infoText.rectTransform.anchoredPosition = new Vector2(100, 100);
                infoText.alignment = TMPro.TextAlignmentOptions.Center;
                infoText.fontSize = 36;
                infoText.color = Color.red;

                infoText.text = "You are vulnerable during shady guy interaction!";
                return;
            }

            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            MyTime.Unpause();

            CoroutineRunner.Instance.Stop(CurrentRoutine);
            CurrentRoutine = CoroutineRunner.Instance.Run(Wait5SecBeforeCanTakeDamageAgain(__instance));
        }

        /// <summary>
        /// Add again 0.3 second of invulnerability after closing the level up screen (unless interacting with shady guy)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(LevelupScreen.OnClose))]
        public static void OnClose_Postfix(LevelupScreen __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            if (InteractableShadyGuy.currentlyInteracting != null)
            {
                infoText.enabled = false;
                infoText.text = "";
                infoText.color = Color.white;
                InteractableShadyGuy.currentlyInteracting = null;
                return;
            }

            if (CurrentRoutine == null)
            {
                Plugin.Log.LogInfo("No active invulnerability routine, skipping.");
                return;
            }

            CoroutineRunner.Instance.Stop(CurrentRoutine);
            CurrentRoutine = CoroutineRunner.Instance.Run(CancelWaitForVulnerabilityAfter1sec(__instance));
        }

        private static IEnumerator CancelWaitForVulnerabilityAfter1sec(LevelupScreen __instance)
        {
            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            GameManager.Instance.player.isTeleporting = true;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = true;

            infoText.enabled = true;
            infoText.transform.SetParent(__instance.transform);
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

        private static IEnumerator Wait5SecBeforeCanTakeDamageAgain(LevelupScreen __instance)
        {
            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
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
