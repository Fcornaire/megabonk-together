using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using HarmonyLib;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Enemies;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace MegabonkTogether.Patches.Enemies
{
    [HarmonyPatch(typeof(Enemy))]
    internal static class EnemyPatch
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
        private static readonly IEnemyManagerService enemyManagerService = Plugin.Services.GetService<IEnemyManagerService>();
        private static readonly ITrackerService trackerService = Plugin.Services.GetService<ITrackerService>();

        public static readonly string[] AllowedDamageSource = Enum.GetNames(typeof(EItem));

        public static readonly DistanceThrottler EnemiesDistanceThrottler = new();

        /// <summary>
        /// Randomly target a player (Not necessarly the local player)
        /// Also adds TargetSwitcher component to the enemy for target switching
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Enemy.InitEnemy))]
        public static void init_PostFix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode();
            if (isServer.HasValue && isServer.Value)
            {
                if (playerManagerService.TryGetGetNetplayerPosition(out uint id)) //TODO: this could be simplifed but too lazy 
                {
                    var host = playerManagerService.GetLocalPlayer();
                    if (host.ConnectionId == id)
                    {
                        DynamicData.For(__instance).Set("targetId", host.ConnectionId);
                    }
                    else
                    {
                        var randomPlayer = playerManagerService.GetNetPlayerByNetplayId(id);

                        __instance.target = randomPlayer.Rigidbody;
                        DynamicData.For(__instance).Set("targetId", randomPlayer.ConnectionId);
                    }
                }
                else
                {
                    var host = playerManagerService.GetLocalPlayer();
                    DynamicData.For(__instance).Set("targetId", host.ConnectionId);
                }

                var switcher = __instance.gameObject.AddComponent<TargetSwitcher>();
                if (__instance.enemyData.enemyName == Actors.Enemies.EEnemy.GhostGrave1
                    || __instance.enemyData.enemyName == Actors.Enemies.EEnemy.GhostGrave2
                    || __instance.enemyData.enemyName == Actors.Enemies.EEnemy.GhostGrave3
                    || __instance.enemyData.enemyName == Actors.Enemies.EEnemy.GhostGrave4)
                {
                    var targetId = switcher.StartSwitching(__instance, true);

                    DynamicData.For(__instance).Set("targetId", targetId);
                }
                else
                {
                    switcher.StartSwitching(__instance);
                }

                enemyManagerService.InitializeSwitcher(switcher, __instance.enemyFlag, __instance.enemyData.enemyName);
            }
            else
            {
                __instance.basePowerupDropChance = 0f;
                __instance.minStayAtDistance = 0f;
            }
        }

        /// <summary>
        /// Manually register a local kill to properly track item procs, money flying and stats
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.EnemyDied), typeof(DamageContainer))]
        public static void EnemyDied_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var currentTracker = trackerService.GetCurrentPlayerId();
            if (currentTracker.HasValue && currentTracker.Value != playerManagerService.GetLocalPlayer().ConnectionId) { return; }

            trackerService.RegisterTrack();
        }

        /// <summary>
        /// Synchronize enemy death
        /// </summary>

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Enemy.EnemyDied), typeof(DamageContainer))]
        public static void EnemyDied_Postfix(Enemy __instance, DamageContainer dc)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnEnemyDied(__instance, dc, trackerService.GetCurrentPlayerId());
        }

        /// <summary>
        /// Manually register a local kill to properly track item procs, money flying and stats
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.EnemyDied), [])]
        public static void EnemyDiedWithoutDc_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var currentTracker = trackerService.GetCurrentPlayerId();
            if (currentTracker.HasValue && currentTracker.Value != playerManagerService.GetLocalPlayer().ConnectionId) { return; }

            trackerService.RegisterTrack();
        }

        /// <summary>
        /// Same as above 
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Enemy.EnemyDied), [])]
        public static void EnemyDiedWithoutDc_Postfix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return;
            }

            synchronizationService.OnEnemyDied(__instance, ownerId: trackerService.GetCurrentPlayerId());
        }

        /// <summary>
        /// Damage applied server-side only
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.Damage))]
        public static bool Damage_Prefix(Enemy __instance, DamageContainer damageContainer)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (isServer)
            {
                return true;
            }

            if (!Plugin.Instance.CAN_DAMAGE_ENEMIES)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Synchronize enemy damage to all clients, synchronize item damage for client
        /// </summary>

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Enemy.Damage))]
        public static void Damage_Postfix(Enemy __instance, DamageContainer damageContainer)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var isServer = synchronizationService.IsServerMode() ?? false;
            if (isServer || AllowedDamageSource.Contains(damageContainer.damageSource))
            {
                synchronizationService.OnEnemyDamaged(__instance, damageContainer);
            }
        }

        /// <summary>
        /// Prevent clients from updating enemy movement
        /// </summary> 
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.TeleportToPlayer))]
        public static bool TeleportToPlayer_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
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
        /// Prevent clients from updating enemy movement
        /// </summary> 
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.StartTeleporting))]
        public static bool StartTeleporting_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
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
        /// Prevent clients from updating enemy movement
        /// </summary> 
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.TryTeleport))]
        public static bool TryTeleport_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
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
        /// Heal applied server-side only
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.Heal))]
        public static bool Heal_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
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
        ///  Prevent enemies from running away from the player in netplay
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.IsRunningFromPlayer))]
        public static bool IsRunningFromPlayer_Prefix(ref bool __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            __result = false;

            return false;
        }

        /// <summary>
        /// Prevent enemies from being stationary in netplay
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.IsStationary))]
        public static bool IsStationary_Prefix(ref bool __result)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            __result = false;
            return false;
        }

        /// <summary>
        /// Distance-based throttling for non boss enemies
        /// only disables renderer at far distance for server and full throttling for clients
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.MyUpdate))]
        public static bool MyUpdate_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (__instance.IsBoss() || __instance.IsStageBoss() || __instance.IsFinalBoss())
            {
                return true;
            }

            var instanceId = __instance.GetInstanceID();
            var isServer = synchronizationService.IsServerMode() ?? false;
            return EnemiesDistanceThrottler.ShouldUpdate(__instance.gameObject, instanceId, isServer);
        }

        /// <summary>
        /// Distance-based throttling for non boss enemies
        /// only disables renderer at far distance for server and full throttling for clients
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Enemy.MyFixedUpdate))]
        public static bool MyFixedUpdate_Prefix(Enemy __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (__instance.IsBoss() || __instance.IsStageBoss() || __instance.IsFinalBoss())
            {
                var renderer = __instance.GetComponentInChildren<Renderer>();
                if (renderer != null && !renderer.enabled)
                {
                    renderer.enabled = true;
                }

                return true;
            }

            var instanceId = __instance.GetInstanceID();
            var isServer = synchronizationService.IsServerMode() ?? false;
            return EnemiesDistanceThrottler.ShouldUpdate(__instance.gameObject, instanceId, isServer);
        }

        /// <summary>
        /// Cleanup throttler tracking when enemy dies
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Enemy.EnemyDied), typeof(DamageContainer))]
        public static void EnemyDied_Cleanup(Enemy __instance)
        {
            var instanceId = __instance.GetInstanceID();
            EnemiesDistanceThrottler.Cleanup(instanceId);
        }
    }
}
