// Useless class but if one day i want multiple coffins boss battle at the same time on the graveyard...

//using HarmonyLib;
//using MegabonkTogether.Services;
//using Microsoft.Extensions.DependencyInjection;

//namespace MegabonkTogether.Patches.Interactables
//{
//    [HarmonyPatch(typeof(InteractableCoffin))]
//    internal static class InteractableCoffinPatches
//    {
//        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

//        [HarmonyPrefix]
//        [HarmonyPatch(nameof(InteractableCoffin.CanInteract))]
//        public static bool CanInteract_Prefix(ref bool __result)
//        {
//            if (!synchronizationService.HasNetplaySessionStarted())
//            {
//                return true;
//            }

//            __result = true;
//            return false;
//        }
//    }
//}
