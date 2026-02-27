using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches.Unity
{
    [HarmonyPatch(typeof(Component))]
    internal static class UnityComponentPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Intercept Component.transform getter to return the correct transform
        /// Needed for DragonBreath or for special attacks that target other players (Special attack always target the local player, if only they used the enemy.target rigidbody instead ¯\_(ツ)_/¯)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("get_transform")]
        public static bool get_transform_Prefix(Component __instance, ref Transform __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (__instance == null) //TODO: i'm pretty sure its a netplayer dangling reference but how do i even debug this...
            {
                __result = GameManager.Instance.player.transform; //Hack ¯\_(ツ)_/¯
                return false;
            }

            if (__instance.name == "Player" && playerManagerService.PeakNetplayerPositionRequest().HasValue)
            {
                var netPlayerId = playerManagerService.PeakNetplayerPositionRequest().Value;
                var localPlayerId = playerManagerService.GetLocalPlayer().ConnectionId;
                if (netPlayerId == localPlayerId)
                {
                    return true;
                }

                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(netPlayerId);
                if (netPlayer == null)
                {
                    Plugin.Log.LogWarning($"get_transform_Prefix: NetPlayer with NetplayId {netPlayerId} not found");
                    return true;
                }
                __result = netPlayer.Model.transform;

                return false;
            }


            return true;
        }
    }

    [HarmonyPatch(typeof(Transform))]
    internal static class TransformPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Intercept Component.transform getter to return the correct transform (Work like above but for Transform component)
        /// Used by LaserBeamGun 
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("get_position")]
        public static bool get_position_Prefix(Transform __instance, ref Vector3 __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (__instance == null)
            {
                __result = GameManager.Instance.player.transform.position;
                return false;
            }


            if (__instance.name == "Hips" && playerManagerService.PeakNetplayerPositionRequest().HasValue)
            {
                var netPlayerId = playerManagerService.PeakNetplayerPositionRequest().Value;
                var localPlayerId = playerManagerService.GetLocalPlayer().ConnectionId;
                if (netPlayerId == localPlayerId)
                {
                    return true;
                }

                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(netPlayerId);
                if (netPlayer == null)
                {
                    return true;
                }
                __result = netPlayer.Model.transform.position;
                __result.y += Plugin.PLAYER_FEET_OFFSET_Y;

                return false;
            }


            return true;
        }

        /// <summary>
        /// Intercept Component.transform getter to return the correct transform (Work like above)
        /// Used by ProjectileMelee (Sword) 
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("get_rotation")]
        public static bool get_rotation_Prefix(Transform __instance, ref Quaternion __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (__instance == null)
            {
                __result = GameManager.Instance.player.transform.rotation;
                return false;
            }

            if (__instance.name == "Renderer" && playerManagerService.PeakNetplayerPositionRequest().HasValue)
            {
                var netPlayerId = playerManagerService.PeakNetplayerPositionRequest().Value;
                var localPlayerId = playerManagerService.GetLocalPlayer().ConnectionId;

                if (netPlayerId == localPlayerId)
                {
                    return true;
                }

                var netPlayer = playerManagerService.GetNetPlayerByNetplayId(netPlayerId);
                if (netPlayer == null)
                {
                    return true;
                }

                __result = netPlayer.Model.transform.rotation;
                return false;
            }

            return true;
        }
    }
}
