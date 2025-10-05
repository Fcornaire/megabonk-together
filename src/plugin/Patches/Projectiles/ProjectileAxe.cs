using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using HarmonyLib;
using MegabonkTogether.Scripts.NetPlayer;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches.Projectiles
{
    [HarmonyPatch(typeof(ProjectileAxe))]
    internal static class ProjectileAxePatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Use the correct player (local / remote) transform
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileAxe.TryInit))]
        public static bool TryInit_Prefix(ProjectileAxe __instance, int projectileIndex)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return true;
            }

            var netPlayer = playerManagerService.GetNetPlayerByWeapon(__instance.weaponBase);
            if (netPlayer == null)
            {
                return true;
            }

            playerManagerService.AddGetNetplayerPositionRequest(netPlayer.ConnectionId);

            return true;
        }

        /// <summary>
        /// Restore original transform after prefix
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProjectileAxe.TryInit))]
        public static void TryInit_Postfix(ProjectileAxe __instance, int projectileIndex)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return;
            }

            var netPlayer = playerManagerService.GetNetPlayerByWeapon(__instance.weaponBase);
            if (netPlayer == null)
            {
                return;
            }

            CorrectProjectileAxeRotation(__instance, netPlayer, projectileIndex);

            playerManagerService.UnqueueNetplayerPositionRequest();
        }

        /// <summary>
        /// Reconstructed attempt at the original game's ProjectileAxe rotation
        /// </summary>
        private static void CorrectProjectileAxeRotation(ProjectileAxe axe, NetPlayer netPlayer, int projectileIndex)
        {
            int attackQuantity = WeaponUtility.GetAttackQuantity(axe.weaponBase);
            float angleOffset = CalculateAngleOffset(projectileIndex, attackQuantity);

            Vector3 localDirection = Quaternion.Euler(0, angleOffset, 0) * Vector3.forward;

            Vector3 worldDirection = netPlayer.Model.transform.rotation * localDirection;

            Quaternion correctedRotation = Quaternion.LookRotation(worldDirection);

            axe.transform.rotation = correctedRotation;

            Vector3 startPos = netPlayer.Model.transform.position;
            float distance = axe.projectileRadius * 1.1f + 3.4f;
            axe.desiredPosition = startPos + axe.transform.forward * distance;
        }

        /// <summary>
        /// Reconstructed attempt at the original game's angle offset calculation for ProjectileAxe
        /// </summary>
        private static float CalculateAngleOffset(int projectileIndex, int attackQuantity)
        {
            if (attackQuantity < 2) return 0f;

            int maxSpreadAngle = attackQuantity * 12; //0xc

            if (maxSpreadAngle < 0)
                maxSpreadAngle = 0;
            else if (maxSpreadAngle > 348) //0x15c
                maxSpreadAngle = 348;

            float t = projectileIndex / (attackQuantity - 1);

            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;

            float maxAngle = maxSpreadAngle;
            float minAngle = -maxAngle;

            return minAngle + (maxAngle - minAngle) * t;
        }
    }
}
