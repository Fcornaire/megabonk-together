using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning.New.Timelines;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(SummonerController))]
    internal static class SummonerControllerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Get the current timeline event before it is updated...
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SummonerController.TickTimeline))]
        public static void TickTimeline_Prefix(SummonerController __instance, ref int __state)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer)
            {
                return;
            }

            __state = __instance.currentTimelineEvent;
        }

        /// <summary>
        /// ... to synchronize swarm events on server
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SummonerController.TickTimeline))]
        public static void TickTimeline_Postfix(SummonerController __instance, int __state)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;

            if (!isServer)
            {
                return;
            }

            if (__state == __instance.currentTimelineEvent)
            {
                return;
            }

            if (__instance.currentTimelineEvent < 0)
            {
                Plugin.Log.LogWarning("Current timeline event is less than 0");
                return;
            }

            var currentEvent = __instance.timeline.events[__instance.currentTimelineEvent];

            if (currentEvent.eTimelineEvent == ETimelineEvent.ESwarm)
            {
                synchronizationService.OnSwarmEvent(currentEvent);
            }
        }
    }
}
