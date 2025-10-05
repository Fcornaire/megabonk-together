using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(WeaponAttack))]
    internal static class WeaponAttackPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Synchronize projectile spawn
        /// </summary>  
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WeaponAttack.SuccessfullySpawnedProjectile))]
        public static void SuccessfullySpawnedProjectile_Postfix(WeaponAttack __instance, Il2CppObjectBase projectile)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (__instance.weaponBase.weaponData.eWeapon == EWeapon.LightningStaff)
            {
                // Dealt in LightningBolt patch
                return;
            }

            var netplayer = playerManagerService.GetNetPlayerByWeapon(__instance.weaponBase);
            synchronizationService.OnSpawnedProjectile(projectile, netplayer?.ConnectionId);
        }

        /// <summary>
        /// Use remote player position when they are the one spawning the projectile
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(WeaponAttack.SpawnProjectile))]
        public static void SpawnProjectile_Prefix(WeaponAttack __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var netplayer = playerManagerService.GetNetPlayerByWeapon(__instance.weaponBase);
            if (netplayer == null)
            {
                return;
            }

            playerManagerService.AddGetNetplayerPositionRequest(netplayer.ConnectionId);

        }

        /// <summary>
        /// Remove the previous request after spawning the projectile
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WeaponAttack.SpawnProjectile))]
        public static void SpawnProjectile_Postfix(WeaponAttack __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var netplayer = playerManagerService.GetNetPlayerByWeapon(__instance.weaponBase);
            if (netplayer == null)
            {
                return;
            }

            playerManagerService.UnqueueNetplayerPositionRequest();
        }
    }
}