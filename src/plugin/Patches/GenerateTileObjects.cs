using Assets.Scripts.Managers;
using HarmonyLib;
using MegabonkTogether.Helpers;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    /// <summary> 
    /// Used on Tiled Map 
    /// </summary>
    [HarmonyPatch(typeof(GenerateTileObjects))]
    internal class GenerateTileObjectsPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Add boss spawner prefabs and also specific prefab like coffin in graveyard (Spawned by server message)
        /// </summary>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GenerateTileObjects.Generate))]
        public static bool Generate_Prefix(GenerateTileObjects __instance, StageData stageData)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;

            if (!isServer)
            {
                foreach (var prefab in stageData.stageTilePrefabs.flatTilePrefabs)
                {
                    Plugin.Instance.AddPrefab(prefab);
                }

                foreach (var prefab in stageData.stageTilePrefabs.mapSpecificTilesPrefabs)
                {
                    Plugin.Instance.AddPrefab(prefab);
                }

                Plugin.Instance.AddPrefab(__instance.bossSpawner);
                Plugin.Instance.AddPrefab(__instance.bossSpawnerFinal);

                if (MapController.currentMap.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard)
                {
                    Plugin.Instance.AddPrefab(__instance.graveyardBossPortal);
                }

                Plugin.Log.LogInfo("Skipping tile object spawning on client");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Send Boss spawner , specific if any and graveyard portal if any info to clients
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GenerateTileObjects.Generate))]
        public static void Generate_Postfix(GenerateTileObjects __instance, StageData stageData)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;

            if (!isServer)
            {
                return;
            }

            var inGame = Il2CppFindHelper.FindAllGameObjects();
            foreach (var obj in inGame)
            {
                foreach (var prefab in stageData.stageTilePrefabs.flatTilePrefabs)
                {
                    if (obj.name.StartsWith(prefab.name))
                    {
                        var dynamic = DynamicData.For(obj);
                        var hasBeenSet = dynamic.Get<bool?>("hasBeenSetByServer");
                        if (!hasBeenSet.HasValue)
                        {
                            synchronizationService.OnSpawnedObject(obj);
                            dynamic.Set("hasBeenSetByServer", true);
                        }
                    }
                }

                foreach (var prefab in stageData.stageTilePrefabs.mapSpecificTilesPrefabs)
                {
                    if (obj.name.StartsWith(prefab.name))
                    {
                        var dynamic = DynamicData.For(obj);
                        var hasBeenSet = dynamic.Get<bool?>("hasBeenSetByServer");
                        if (!hasBeenSet.HasValue)
                        {
                            synchronizationService.OnSpawnedObject(obj);
                            dynamic.Set("hasBeenSetByServer", true);
                        }
                    }
                }

                if (obj.name.StartsWith(__instance.bossSpawner.name))
                {
                    var dynamic = DynamicData.For(obj);
                    var hasBeenSet = dynamic.Get<bool?>("hasBeenSetByServer");
                    if (!hasBeenSet.HasValue)
                    {
                        synchronizationService.OnSpawnedObject(obj);
                        dynamic.Set("hasBeenSetByServer", true);
                    }
                }

                if (obj.name.StartsWith(__instance.bossSpawnerFinal.name))
                {
                    var dynamic = DynamicData.For(obj);
                    var hasBeenSet = dynamic.Get<bool?>("hasBeenSetByServer");
                    if (!hasBeenSet.HasValue)
                    {
                        synchronizationService.OnSpawnedObject(obj);
                        dynamic.Set("hasBeenSetByServer", true);
                    }
                }

                if (MapController.currentMap.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard &&
                    obj.name.StartsWith(__instance.graveyardBossPortal.name))
                {
                    var dynamic = DynamicData.For(obj);
                    var hasBeenSet = dynamic.Get<bool?>("hasBeenSetByServer");
                    if (!hasBeenSet.HasValue)
                    {
                        synchronizationService.OnSpawnedObject(obj);
                        dynamic.Set("hasBeenSetByServer", true);
                    }
                }
            }
        }
    }
}
