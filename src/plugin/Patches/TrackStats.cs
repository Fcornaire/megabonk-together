using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(TrackStats))]
    internal static class TrackStatsPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetRequiredService<IPlayerManagerService>();

        /// <summary>
        ///  Track stats per player
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TrackStats.OnEnemyDied))]
        public static bool OnEnemyDied_Prefix(Enemy enemy, DamageContainer deathSource)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var ownerId = DynamicData.For(deathSource).Get<uint?>("ownerId");
            if (!ownerId.HasValue)
            {
                ownerId = DynamicData.For(enemy).Get<uint?>("ownerId");
            }

            if (ownerId.HasValue && playerManagerService.IsRemoteConnectionId(ownerId.Value))
            {
                return false;
            }

            return true;
        }
    }
}
