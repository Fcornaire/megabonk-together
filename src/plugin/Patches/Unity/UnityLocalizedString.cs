using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine.Localization;

namespace MegabonkTogether.Patches.Unity
{
    [HarmonyPatch(typeof(LocalizedString))]
    internal static class UnityLocalizedStringPatches
    {
        private static readonly ILocalizationService localizationService = Plugin.Services.GetService<ILocalizationService>();

        /// <summary>
        /// Use custom localization service to get localized string if needed (used in notification)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LocalizedString.GetLocalizedString), new System.Type[0])]
        public static bool GetLocalizedString_Prefix(LocalizedString __instance, ref string __result)
        {
            var tableRef = __instance.TableReference?.TableCollectionName;
            var entryRef = __instance.TableEntryReference?.Key;
            var localizedValue = localizationService.GetCustomLocalizedString(tableRef, entryRef);

            if (!string.IsNullOrEmpty(localizedValue))
            {
                __result = localizedValue;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Use custom localization service to get localized string if needed (used in notification)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LocalizedString.GetLocalizedString), new System.Type[] { typeof(Il2CppReferenceArray<Il2CppSystem.Object>) })]
        public static bool GetLocalizedString_WithArgs_Prefix(LocalizedString __instance, ref string __result)
        {
            var tableRef = __instance.TableReference?.TableCollectionName;
            var entryRef = __instance.TableEntryReference?.Key;
            var localizedValue = localizationService.GetNextCustomLocalizedDescription(tableRef, entryRef);

            if (!string.IsNullOrEmpty(localizedValue))
            {
                __result = localizedValue;
                return false;
            }

            return true;
        }

    }
}
