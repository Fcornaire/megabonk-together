using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(DetectInteractables))]
    public static class DetectInteractablesPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        /// <summary>
        /// Send interaction to other players
        /// </summary>

        [HarmonyPrefix]
        [HarmonyPatch(nameof(DetectInteractables.TryInteract))]
        public static void TryInteract_Postfix(DetectInteractables __instance)
        {
            if (!Plugin.CAN_SEND_MESSAGES) //Prevent sending messages on received events
            {
                return;
            }

            if (__instance.currentInteractable == null)
            {
                return;
            }

            if (!CanSynchronize(__instance))
            {
                return;
            }

            synchronizationService.OnInteractableUsed(__instance.currentInteractable);
        }

        private static bool CanSynchronize(DetectInteractables __instance)
        {
            if (!__instance.CanInteract())
            {
                return false;
            }

            var chest = __instance.currentInteractable.GetComponentInChildren<InteractableChest>();
            if (chest != null)
            {
                if (!chest.CanAfford())
                {
                    return false;
                }
            }

            var portal = __instance.currentInteractable.GetComponentInChildren<InteractablePortal>();
            if (portal != null)
            {
                if (Plugin.Instance.IS_MANUAL_INVINCIBLE)
                {
                    Plugin.Instance.IS_MANUAL_INVINCIBLE = false;
                    GameManager.Instance.player.isTeleporting = false;
                    return true;
                }

                if (GameManager.Instance.player.isTeleporting)
                {
                    return false;
                }
            }

            var finalPortal = __instance.currentInteractable.GetComponentInChildren<InteractableBossSpawnerFinal>();
            if (finalPortal != null)
            {
                if (Plugin.Instance.IS_MANUAL_INVINCIBLE)
                {
                    Plugin.Instance.IS_MANUAL_INVINCIBLE = false;
                    GameManager.Instance.player.isTeleporting = false;
                    return true;
                }

                if (GameManager.Instance.player.isTeleporting)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
