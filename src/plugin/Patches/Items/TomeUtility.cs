using Assets.Scripts.Inventory__Items__Pickups;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches.Items
{
    [HarmonyPatch(typeof(TomeUtility))]
    internal static class TomeUtilityPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Prevent triggering tome effect if the tome is added by a netplayer. (Chaos tome)
        /// </summary>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TomeUtility.CheckSpecialTomes))]
        public static bool CheckSpecialTomes_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (Plugin.Instance.IS_NETPLAYER_ADDING_TOME)
            {
                Plugin.Instance.IS_NETPLAYER_ADDING_TOME = false;
                return false;
            }

            return true;
        }
    }
}
