using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.MapGeneration
{
    [HarmonyPatch(typeof(MapGenerator))]
    internal static class MapGeneratorPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetRequiredService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetRequiredService<IPlayerManagerService>();

        /// <summary>
        /// Use the same seed for all players in a netplay session
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MapGenerator.GenerateMap), [typeof(int)])]
        public static void GenerateMap_Prefix(ref int seed)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            seed = playerManagerService.GetSeed();
        }

    }
}
