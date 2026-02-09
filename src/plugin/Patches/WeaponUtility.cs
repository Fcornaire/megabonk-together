using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(WeaponUtility))]
    internal static class WeaponUtilityPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Host.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Host.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Synchronize lightning strike weapon
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WeaponUtility.LightningStrike))]
        public static void LightningStrike_Postfix(Enemy enemy, int bounces, DamageContainer dc, float bounceRange, float bounceProcCoefficient)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnLightningStrike(enemy, bounces, dc, bounceRange, bounceProcCoefficient);
        }

        /// <summary>
        /// Assign ownerId to DamageContainer (used to track who dealt the damage and prevent Money flying to the wrong player)
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WeaponUtility.GetDamageContainer), [typeof(WeaponBase), typeof(ProjectileBase), typeof(Enemy), typeof(Vector3), typeof(float)])]
        public static void GetDamageContainer_Postfix(WeaponBase weaponBase, DamageContainer __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }
            var dynDamageContainer = DynamicData.For(__result);
            var hasOwnerId = dynDamageContainer.Get<uint?>("ownerId"); //TODO: track DamageContainer so we dont have to do this check
            if (hasOwnerId.HasValue)
            {
                return;
            }

            var owner = playerManagerService.GetNetPlayerByWeapon(weaponBase);
            if (owner != null)
            {
                dynDamageContainer.Set("ownerId", owner.ConnectionId);
            }
            else
            {
                if (GameManager.Instance.player.inventory.weaponInventory.weapons.ContainsValue(weaponBase))
                {
                    var localPlayer = playerManagerService.GetLocalPlayer();
                    dynDamageContainer.Set("ownerId", localPlayer.ConnectionId);
                }
            }
        }
    }
}
