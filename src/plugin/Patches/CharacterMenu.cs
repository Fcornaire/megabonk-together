using HarmonyLib;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(CharacterMenu))]
    internal static class CharacterMenuPatches
    {
        /// <summary>
        /// Save character icons for displayer
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CharacterMenu.TryInit))]
        public static void TryInit_Postfix(CharacterMenu __instance)
        {
            Plugin.Instance.ClearCharacterIcons();
            Plugin.Instance.AddCharacterIcons(__instance.characterButtons);
        }
    }
}
