using Assets.Scripts.Managers;
using HarmonyLib;
using MegabonkTogether.Common;
using MegabonkTogether.Common.Models;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Button;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using TextCopy;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(WindowManager))]
    internal static class WindowManagerPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
        private static readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();
        private static readonly IUdpClientService udpClientService = Plugin.Services.GetService<IUdpClientService>();
        private static bool hasShownUpdateModal = false;
        private static GameObject friendliesInfoDisplay;
        private static GameObject readyStatusDisplay;
        private static MyButton previousSelectedButton = null;

        /// <summary>
        /// Reset networking and net players displayer when going back to main menu/
        /// Check for updates
        /// Create friendlies info in character menu if in friendlies mode
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WindowManager.WindowOpened))]
        public static void WindowOpened_Postfix(Window newWindow)
        {
            if (newWindow.name == "Menu")
            {
                Plugin.Instance.NetworkHandler.ResetNetworking();
                Plugin.Instance.NetPlayersDisplayer.ResetCards();
                Plugin.Instance.HideModal();
                Plugin.Instance.ResetWorldSize();
                Plugin.Instance.CameraSwitcher.ResetToLocalPlayer();
                Plugin.Instance.ClearPrefabs();
                Plugin.Instance.RestoreDeath(false);

                GameObject.Destroy(Plugin.Instance.NetworkTab);
                Plugin.Instance.NetworkTab = null;

                DestroyFriendliesInfoDisplay();

                if (SpawnPlayerPortalPatches.WaitForLobbyCoroutine != null)
                {
                    CoroutineRunner.Instance.Stop(SpawnPlayerPortalPatches.WaitForLobbyCoroutine);
                }

                if (autoUpdaterService.IsThunderstoreBuild() && hasShownUpdateModal) return;

                Task.Run(async () =>
                {
                    await autoUpdaterService.CheckAndUpdate();

                    if (autoUpdaterService.IsAnUpdateAvailable())
                    {
                        Plugin.ShowUpdateAvailableModal();

                        if (autoUpdaterService.IsThunderstoreBuild())
                        {
                            hasShownUpdateModal = true;
                        }

                        if (Plugin.Instance.PlayTogetherButton == null)
                        {
                            return;
                        }
                        Plugin.Instance.PlayTogetherButton.enabled = false;
                    }
                });
            }

            if (newWindow.name == "W_Character")
            {
                ButtonManager.selectedButton2 = (newWindow as CharacterMenu).startBtn;

                if (Plugin.Instance.Mode.Mode == NetworkModeType.Friendlies)
                {
                    var mainMenu = Plugin.Instance.GetMainMenu();
                    CreateFriendliesInfoDisplay(mainMenu.tabCharacters.transform, newWindow);
                }
            }
            else
            {
                if (friendliesInfoDisplay != null)
                {
                    DestroyFriendliesInfoDisplay();
                }
            }
        }

        /// <summary>
        /// Re-enable confirm button on character menu when closed
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WindowManager.WindowClosed))]
        public static void WindowClosed_Postfix(Window closedWindow)
        {
            if (closedWindow.name == "W_Character")
            {
                var character = closedWindow as CharacterMenu;
                character.b_confirm.state = MyButton.EButtonState.Active;
                character.b_confirm.RefreshState();
            }
        }

        /// <summary>
        /// Make confirm button inactive on character menu if less than 2 players in friendlies host mode
        /// Make map confirm button inactive if less than 2 players or not all ready in friendlies host mode
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(WindowManager.Update))]
        public static void Update_Postix()
        {
            if (WindowManager.activeWindow?.name == "W_Character" && Plugin.Instance.Mode.Mode == NetworkModeType.Friendlies && Plugin.Instance.Mode.Role == Role.Host)
            {
                if (playerManagerService.GetAllPlayers().Count() < 2)
                {
                    var character = WindowManager.activeWindow as CharacterMenu;
                    character.b_confirm.state = MyButton.EButtonState.Inactive;
                    character.b_confirm.RefreshState();
                }
                else
                {
                    var character = WindowManager.activeWindow as CharacterMenu;
                    character.b_confirm.state = MyButton.EButtonState.Active;
                    character.b_confirm.RefreshState();
                }
            }

            if (WindowManager.activeWindow?.name == "Maps And Stats" && Plugin.Instance.Mode.Mode == NetworkModeType.Friendlies && Plugin.Instance.Mode.Role == Role.Host)
            {
                var mainMenu = Plugin.Instance.GetMainMenu();

                var confirmButton = mainMenu.mapSelectionUi.btnConfirm;

                if (confirmButton == null) return;

                if (playerManagerService.GetAllPlayers().Count() < 2 || !udpClientService.AreAllPeersReady() || udpClientService.IsHandlingConnection())
                {
                    confirmButton.state = MyButton.EButtonState.Inactive;
                    confirmButton.RefreshState();

                    if (readyStatusDisplay == null)
                    {
                        CreateReadyStatusDisplay(mainMenu.tabMaps.transform);
                    }

                    UpdateReadyStatusDisplay();
                }
                else
                {
                    confirmButton.state = MyButton.EButtonState.Active;
                    confirmButton.RefreshState();

                    DestroyReadyStatusDisplay();
                }
            }
            else
            {
                if (readyStatusDisplay != null)
                {
                    DestroyReadyStatusDisplay();
                }
            }
        }

        private static void CreateFriendliesInfoDisplay(Transform parent, Window newWindow)
        {
            var menu = newWindow as CharacterMenu;
            var btn = menu.b_confirm;
            DestroyFriendliesInfoDisplay();

            var mainMenu = Plugin.Instance.GetMainMenu();

            friendliesInfoDisplay = new GameObject("FriendliesInfoDisplay");
            friendliesInfoDisplay.transform.SetParent(parent, false);

            var rectTransform = friendliesInfoDisplay.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-250f, -50f);
            rectTransform.sizeDelta = new Vector2(300f, 120f);

            friendliesInfoDisplay.transform.SetAsLastSibling();

            var mode = Plugin.Instance.Mode;
            var roleText = mode.Role.ToString();

            var textObj = new GameObject("InfoText");
            textObj.transform.SetParent(friendliesInfoDisplay.transform, false);

            var textRectTransform = textObj.AddComponent<RectTransform>();
            textRectTransform.anchorMin = new Vector2(0f, 1f);
            textRectTransform.anchorMax = new Vector2(1f, 1f);
            textRectTransform.pivot = new Vector2(1f, 1f);
            textRectTransform.anchoredPosition = new Vector2(0f, 0f);
            textRectTransform.sizeDelta = new Vector2(0f, 80f);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"Role: {roleText}\nCode: ******";
            text.alignment = TextAlignmentOptions.TopLeft;
            text.fontSize = 36;
            text.color = Color.white;

            if (Plugin.Instance.Mode.Role != Role.Host)
            {
                return;
            }

            var copyButtonObj = GameObject.Instantiate(btn.gameObject);
            copyButtonObj.name = "CopyCodeButton";
            copyButtonObj.transform.SetParent(friendliesInfoDisplay.transform, false);
            copyButtonObj.SetActive(true);

            var originalButton = copyButtonObj.GetComponent<MyButtonNormal>();
            if (originalButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalButton);
            }

            UnityEngine.UI.Button button = copyButtonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new();
            }

            var localizeStringEvent = copyButtonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEvent != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEvent);
            }

            var copyButton = copyButtonObj.AddComponent<CustomButton>();
            copyButton.SetOnClickAction(OnCopyCodeClicked);
            copyButton.OverrideStartHoverAction(() =>
            {
                previousSelectedButton = ButtonManager.selectedButton2;
            });
            copyButton.OverrideEndHoverAction(() =>
            {
                ButtonManager.selectedButton2 = previousSelectedButton;
                previousSelectedButton = null;
            });

            var textWrapper = copyButtonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = "Copy Code";
            textWrapper.t_text.fontSize = 25;

            var copyButtonRect = copyButtonObj.GetComponent<RectTransform>();
            copyButtonRect.anchorMin = new Vector2(1f, 0f);
            copyButtonRect.anchorMax = new Vector2(1f, 0f);
            copyButtonRect.pivot = new Vector2(1f, 0f);
            copyButtonRect.anchoredPosition = new Vector2(0f, 10f);
            copyButtonRect.sizeDelta = new Vector2(200f, 40f);

            copyButtonObj.transform.SetAsLastSibling();
        }

        private static void OnCopyCodeClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            try
            {
                ClipboardService.SetText(Plugin.Instance.Mode.RoomCode);
                Plugin.Log.LogInfo("Room code copied to clipboard");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private static void DestroyFriendliesInfoDisplay()
        {
            if (friendliesInfoDisplay != null)
            {
                GameObject.Destroy(friendliesInfoDisplay);
                friendliesInfoDisplay = null;
            }
        }

        private static void CreateReadyStatusDisplay(Transform parent)
        {
            DestroyReadyStatusDisplay();

            readyStatusDisplay = new GameObject("ReadyStatusDisplay");
            readyStatusDisplay.transform.SetParent(parent, false);

            var rectTransform = readyStatusDisplay.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-200f, -40f);
            rectTransform.sizeDelta = new Vector2(400f, 80f);

            readyStatusDisplay.transform.SetAsLastSibling();

            var textObj = new GameObject("StatusText");
            textObj.transform.SetParent(readyStatusDisplay.transform, false);

            var textRectTransform = textObj.AddComponent<RectTransform>();
            textRectTransform.anchorMin = new Vector2(0f, 1f);
            textRectTransform.anchorMax = new Vector2(1f, 1f);
            textRectTransform.pivot = new Vector2(1f, 1f);
            textRectTransform.anchoredPosition = new Vector2(0f, 0f);
            textRectTransform.sizeDelta = new Vector2(0f, 80f);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Waiting for players to be ready...";
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.TopRight;
            text.fontSize = 36;
        }

        private static void UpdateReadyStatusDisplay()
        {
            if (readyStatusDisplay == null) return;

            var textObj = readyStatusDisplay.transform.Find("StatusText");
            if (textObj == null) return;

            var text = textObj.GetComponent<TextMeshProUGUI>();
            if (text == null) return;

            int readyCount = udpClientService.GetCurrentReadyPeersCount();
            int totalCount = playerManagerService.GetAllPlayers().Count() - 1;

            text.text = $"Waiting for players to be ready...\nReady: {readyCount}/{totalCount}";
        }

        private static void DestroyReadyStatusDisplay()
        {
            if (readyStatusDisplay != null)
            {
                GameObject.Destroy(readyStatusDisplay);
                readyStatusDisplay = null;
            }
        }
    }
}
