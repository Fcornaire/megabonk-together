using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(TrackStats))]
    internal static class TrackStatsPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetRequiredService<IPlayerManagerService>();
        private static readonly ITrackerService trackerService = Plugin.Services.GetRequiredService<ITrackerService>();

        /// <summary>
        ///  Track stats per player (Skip for remote kill)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TrackStats.OnEnemyDied))]
        public static bool OnEnemyDied_Prefix(Enemy enemy, DamageContainer deathSource)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var tracks = trackerService.GetPlayerTrack();

            if (tracks.kills == 0)
            {
                return false;
            }

            tracks.kills--;

            return true;
        }
    }
}
