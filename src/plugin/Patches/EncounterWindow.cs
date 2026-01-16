using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Utility;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(EncounterWindows))]
    internal static class EncounterWindowPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// When changing level, the game will try to pop reward from previous stage missed
        /// This can freeze the game as the queue will get modified while iterating
        /// To prevent that, we just make sure the player can move before popping the reward
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EncounterWindows.PopReward))]
        public static bool PopReward_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!GameManager.Instance.player.playerInput.CanInput())
            {
                //Plugin.Log.LogWarning($"Player can't move yet, skipping reward pop for now");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prevent adding an encounter if one is already in progress
        /// This should not only prevent missing some reward but hopefully also random crashes happening with encounter
        /// The encounter will be queued and popped later at LateUpdate
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EncounterWindows.AddEncounter))]
        public static bool AddEncounter_Prefix(EncounterWindows __instance, EEncounter rewardWindowType)
        {
            if (__instance.encounterInProgress)
            {
                //Plugin.Log.LogWarning($"Encounter in progress, queueing encounter {rewardWindowType}");
                __instance.rewardQueue.Enqueue(rewardWindowType);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prevent pause on netplay reward pop (Shady guy and other)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(EncounterWindows.PopReward))]
        public static void PopReward_Postfix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            MyTime.Unpause();
        }

        /// <summary>
        /// PopReward_Prefix prevent reward pop if player can't move yet.
        /// If we can move now, we should pop previously prevented reward as soon as possible.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EncounterWindows.LateUpdate))]
        public static void LateUpdate_Prefix(EncounterWindows __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!GameManager.Instance.player.playerInput.CanInput())
            {
                return;
            }

            if (GameManager.Instance.player.IsDead())
            {
                return;
            }

            var currentQueue = __instance.rewardQueue;
            if (currentQueue.Count > 0 && !__instance.encounterInProgress)
            {
                //Plugin.Log.LogInfo($"Pop previously missed reward");
                __instance.PopReward();
            }
        }
    }
}
