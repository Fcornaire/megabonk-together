using Assets.Scripts.Managers;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using static MapGenerationController;

namespace MegabonkTogether.Patches.MapGeneration
{
    [HarmonyPatch(typeof(_GenerateMap_d__39))]
    internal class MapGenerationControllerGenMapPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Save prefabs (Spawned by server message)
        /// Also set custom seed for crypt generator on the graveyard map
        /// Also add specific prefabs if any in the map
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(_GenerateMap_d__39.MoveNext))]
        public static void MoveNext_Prefix(_GenerateMap_d__39 __instance, ref bool __result)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            if (MapController.runConfig.mapData.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard)
            {
                RsgController.SetCustomSeed(playerManagerService.GetSeed());
            }


            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer && __instance._stageData_5__3 != null)
            {
                foreach (var objs in __instance.__4__this.randomObjectPlacer.randomObjects)
                {
                    foreach (var prefab in objs.prefabs)
                    {
                        Plugin.Instance.AddPrefab(prefab);
                    }
                }

                foreach (var objs in __instance._stageData_5__3.randomMapObjects)
                {
                    foreach (var prefab in objs.prefabs)
                    {
                        Plugin.Instance.AddPrefab(prefab);
                    }
                }

                foreach (var prefab in __instance.__4__this.randomObjectPlacer.greedShrineSpawns.prefabs)
                {
                    Plugin.Instance.AddPrefab(prefab);
                }

                foreach (var prefab in __instance.__4__this.randomObjectPlacer.chargeShrineSpawns.prefabs)
                {
                    Plugin.Instance.AddPrefab(prefab);
                }

                foreach (var prefab in __instance._mapData_5__4.shrines)
                {
                    Plugin.Instance.AddPrefab(prefab);
                }

                foreach (var randomMapObject in __instance._mapData_5__4.randomObjectsOverride)
                {
                    foreach (var prefab in randomMapObject.prefabs)
                    {
                        if (prefab.name.Contains("Microwave"))
                        {
                            continue; //Skip microwaves , handled in MicrowavePatches
                        }

                        Plugin.Instance.AddPrefab(prefab);
                    }
                }

                if (__instance._mapData_5__4.mapType == Assets.Scripts._Data.MapsAndStages.EMapType.ProceduralMesh)
                {
                    Plugin.Instance.AddPrefab(__instance.__4__this.bossPortal);
                    Plugin.Instance.AddPrefab(__instance.__4__this.bossPortalFinal);
                }
            }
        }

        /// <summary>
        /// Store world size for later use
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(_GenerateMap_d__39.MoveNext))]
        public static void MoveNext_Postfix(_GenerateMap_d__39 __instance, ref bool __result)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            if (__instance.__1__state == 3)
            {
                if (MapController.runConfig.mapData.eMap == Assets.Scripts._Data.MapsAndStages.EMap.Graveyard)
                {
                    Plugin.Instance.SetWorldSize(new UnityEngine.Vector3(5000f, 5000f, 5000f)); //Is the crypt outside of the map ? Using a large size fix Quantization issues
                }
                else
                {
                    Plugin.Instance.SetWorldSize(__instance._worldSize_5__5);
                }

                Plugin.Instance.OriginalWorldSize = __instance._worldSize_5__5;
            }
        }
    }
}
