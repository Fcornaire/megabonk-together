using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches.Projectiles
{
    [HarmonyPatch(typeof(ProjectileBase))]
    internal class ProjectileBasePatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();
        private static readonly Services.IPlayerManagerService playerManagerService = Plugin.Services.GetService<Services.IPlayerManagerService>();
        private static readonly Services.IProjectileManagerService projectileManagerService = Plugin.Services.GetService<Services.IProjectileManagerService>();

        /// <summary>
        /// Make sure to spawn projectiles at the net player's position
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileBase.TryInit))]
        public static void TryInit_Prefix(ProjectileBase __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var weapon = __instance.weaponBase;
            var netPlayer = playerManagerService.GetNetPlayerByWeapon(weapon);

            if (netPlayer != null)
            {
                var id = netPlayer.ConnectionId;

                __instance.transform.position = new UnityEngine.Vector3(
                    netPlayer.Model.transform.position.x,
                    netPlayer.Model.transform.position.y + Plugin.PLAYER_FEET_OFFSET_Y,
                    netPlayer.Model.transform.position.z
                );

                playerManagerService.AddProjectileToSpawn(id);
            }
            else
            {
                Plugin.Log.LogWarning("Weapon not found ?");
            }
        }


        /// <summary>
        /// Ignore HitEnemy for projectiles on clients (Simulated by server)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileBase.HitEnemy))]
        public static bool HitEnemy_Prefix(ProjectileBase __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;

            return isServer;
        }


        ///// <summary>
        ///// Ignore HitEnemy for projectiles not owned by local player (Prevent some null exceptions logs)
        ///// </summary>
        ///// <param name="__instance"></param>
        ///// <returns></returns>
        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(ProjectileBase.HitEnemy))]
        //public static bool HitEnemy_Prefix(ProjectileBase __instance)
        //{
        //    if (!synchronizationService.HasNetplaySessionStarted())
        //    {
        //        return true;
        //    }

        //    var projectileEntry = projectileManagerService.GetProjectileByReference(__instance);

        //    if (projectileEntry.Value == null)
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        /// <summary>
        /// Synchronize projectile destruction server-side only
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileBase.ProjectileDone))]
        public static bool ProjectileDone_Postfix(ProjectileBase __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            var netplayId = DynamicData.For(__instance).Get<uint?>("netplayId");
            if (netplayId.HasValue && !isServer)
            {
                return false;
            }

            synchronizationService.OnProjectileDone(__instance);

            return true;
        }
    }
}
