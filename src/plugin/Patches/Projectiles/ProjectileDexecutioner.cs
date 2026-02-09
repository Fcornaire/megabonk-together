using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Projectiles
{
    [HarmonyPatch(typeof(ProjectileDexecutioner))]
    internal static class ProjectileDexecutionerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileDexecutioner.MyUpdate))]
        public static bool MyUpdate_Prefix(ProjectileDexecutioner __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return false;
            }

            var netPlayer = Plugin.Services.GetService<IPlayerManagerService>().GetNetPlayerByWeapon(__instance.weaponBase);
            if (netPlayer == null)
            {
                return true;
            }

            playerManagerService.AddGetNetplayerPositionRequest(netPlayer.ConnectionId);

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProjectileDexecutioner.MyUpdate))]
        public static void MyUpdate_Postfix(ProjectileDexecutioner __instance)
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
            var netPlayer = Plugin.Services.GetService<IPlayerManagerService>().GetNetPlayerByWeapon(__instance.weaponBase);
            if (netPlayer == null)
            {
                return;
            }
            playerManagerService.UnqueueNetplayerPositionRequest();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileDexecutioner.TryInit))]
        public static bool TryInit_Prefix(ProjectileDexecutioner __instance, int projectileIndex)
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

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProjectileDexecutioner.TryInit))]
        public static void TryInit_Postfix(ProjectileDexecutioner __instance, int projectileIndex)
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
            playerManagerService.UnqueueNetplayerPositionRequest();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProjectileDexecutioner.CheckZone))]
        public static bool CheckZone_Prefix(ProjectileDexecutioner __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            var isHost = synchronizationService.IsServerMode() ?? false;
            if (!isHost)
            {
                return false;
            }
            var netPlayer = playerManagerService.GetNetPlayerByWeapon(__instance.weaponBase);
            if (netPlayer == null)
            {
                return true;
            }
            playerManagerService.AddGetNetplayerPositionRequest(netPlayer.ConnectionId);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProjectileDexecutioner.CheckZone))]
        public static void CheckZone_Postfix(ProjectileDexecutioner __instance)
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
            playerManagerService.UnqueueNetplayerPositionRequest();
        }
    }
}
