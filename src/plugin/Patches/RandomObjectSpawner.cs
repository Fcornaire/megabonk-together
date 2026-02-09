using Assets.Scripts.Game.MapGeneration;
using HarmonyLib;
using MegabonkTogether.Helpers;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(RandomObjectPlacer))]
    internal class RandomObjectPlacerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Skip object spawning on clients
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(RandomObjectPlacer.RandomObjectSpawner))]
        public static bool RandomObjectSpawner_Prefix(RandomObjectPlacer __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (!isServer)
            {
                return false;
            }

            return true;

        }

        /// <summary>
        /// Send spawned objects to clients
        /// </summary>

        [HarmonyPostfix]
        [HarmonyPatch(nameof(RandomObjectPlacer.RandomObjectSpawner))]
        public static void RandomObjectSpawner_Postfix(RandomObjectPlacer __instance, RandomMapObject randomObject)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode();
            if (isServer.HasValue && !isServer.Value)
            {
                return;
            }

            foreach (var prefab in randomObject.prefabs)
            {
                var inGame = Il2CppFindHelper.FindAllGameObjects();
                foreach (var obj in inGame)
                {
                    if (obj.name.StartsWith(prefab.name) && obj.name != "BarrelMesh" && obj.name != "ArchCollider")
                    {
                        if (prefab.name.Contains("Microwave"))
                        {
                            continue; //Skip microwaves , handled in MicrowavePatches
                        }

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
}
