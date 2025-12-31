using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.ConstantAttacks
{
    [HarmonyPatch(typeof(CombatAura))]
    internal static class CombatAuraPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();


        /// <summary>
        /// Prevent non-local players to use Combat Aura
        /// </summary>

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CombatAura.FixedUpdate))]
        public static bool FixedUpdate_Prefix(CombatAura __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            var isHost = synchronizationService.IsServerMode() ?? false;
            return isHost;
        }
    }
}
