using MegabonkTogether.Common.Models;
using MegabonkTogether.Configuration;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Button;
using MegabonkTogether.Scripts.Modal;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace MegabonkTogether.Scripts
{
    internal class NetworkMenuTab : ModalBase
    {
        private CustomButton randomButton;
        private CustomButton friendliesButton;
        private CustomButton closeButton;
        private CustomButton stopButton;
        private TMP_InputField playerNameInput;
        private MainMenu mainMenu;
        private GameObject label;
        private bool wasInputFocused;
        private Coroutine connectionCoroutine;
        private ProfanityFilter.ProfanityFilter filter;

        private GameObject friendliesTitle;
        private CustomButton hostButton;
        private CustomButton joinButton;
        private CustomButton friendliesBackButton;
        private TMP_InputField codeInput;
        private GameObject codeLabel;

        protected void Awake()
        {
            filter = new ProfanityFilter.ProfanityFilter();
        }

        public void SetMainMenu(MainMenu menu)
        {
            mainMenu = menu;
        }

        protected override void OnUICreated()
        {
            CreateCloseButton();
            CreatePlayerNameInput();
            CreateMatchButtons();
            CreateStopButton();
            CreateFriendliesUI();
        }

        private void CreateCloseButton()
        {
            var buttonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            buttonObj.transform.SetParent(panel.transform, false);

            var originalButton = buttonObj.GetComponent<MyButtonNormal>();
            if (originalButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalButton);
            }

            UnityEngine.UI.Button button = buttonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new();
            }

            var localizeStringEvent = buttonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEvent != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEvent);
            }

            closeButton = buttonObj.AddComponent<CustomButton>();
            closeButton.SetOnClickAction(OnCloseClicked);

            var textWrapper = buttonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = "Close";
            textWrapper.t_text.fontSize = 36;

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, -200f);
            rectTransform.sizeDelta = new Vector2(300, 70);
        }

        private void CreatePlayerNameInput()
        {
            var inputObj = new GameObject("PlayerNameInput");
            inputObj.transform.SetParent(panel.transform, false);

            var rectTransform = inputObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.7f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.7f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(300, 50);

            var image = inputObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            playerNameInput = inputObj.AddComponent<TMP_InputField>();
            playerNameInput.textComponent = CreateInputText(inputObj);
            playerNameInput.placeholder = CreatePlaceholderText(inputObj);
            playerNameInput.text = ModConfig.PlayerName.Value;
            playerNameInput.characterLimit = 20;

            playerNameInput.caretWidth = 3;
            playerNameInput.caretColor = Color.white;
            playerNameInput.customCaretColor = true;
            playerNameInput.caretBlinkRate = 0.85f;

            playerNameInput.selectionColor = new Color(0.65f, 0.8f, 1f, 0.5f);

            // Enable/disable to force caret initialization (Thanks random user on Stack exchange)
            playerNameInput.enabled = false;
            playerNameInput.enabled = true;

            label = new GameObject("Label");
            label.transform.SetParent(panel.transform, false);

            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.75f);
            labelRect.anchorMax = new Vector2(0.5f, 0.75f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0, 25);
            labelRect.sizeDelta = new Vector2(300, 40);

            var labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "Player Name:";
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 50;
            labelText.color = Color.white;
        }

        private TextMeshProUGUI CreateInputText(GameObject parent)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.fontSize = 35;
            text.color = Color.white;
            text.margin = new Vector4(10, 0, 10, 0);

            return text;
        }

        private TextMeshProUGUI CreatePlaceholderText(GameObject parent)
        {
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(parent.transform, false);

            var rectTransform = placeholderObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var text = placeholderObj.AddComponent<TextMeshProUGUI>();
            text.text = "Enter your name...";
            text.alignment = TextAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.fontSize = 24;
            text.color = new Color(1f, 1f, 1f, 0.3f);
            text.margin = new Vector4(10, 0, 10, 0);

            return text;
        }

        private void CreateStopButton()
        {
            var buttonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            buttonObj.transform.SetParent(panel.transform, false);

            var originalButton = buttonObj.GetComponent<MyButtonNormal>();
            if (originalButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalButton);
            }

            UnityEngine.UI.Button button = buttonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new();
            }

            var localizeStringEvent = buttonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEvent != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEvent);
            }

            stopButton = buttonObj.AddComponent<CustomButton>();
            stopButton.SetOnClickAction(OnStopClicked);

            var textWrapper = buttonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.fontSize = 36;

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, -175f);
            rectTransform.sizeDelta = new Vector2(300, 70);

            stopButton.gameObject.SetActive(false);
        }

        private void CreateMatchButtons()
        {
            var randomButtonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            randomButtonObj.transform.SetParent(panel.transform, false);

            var originalRandomButton = randomButtonObj.GetComponent<MyButtonNormal>();
            if (originalRandomButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalRandomButton);
            }

            UnityEngine.UI.Button button = randomButtonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new();
            }

            var localizeStringEvent = randomButtonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEvent != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEvent);
            }

            randomButton = randomButtonObj.AddComponent<CustomButton>();
            randomButton.SetOnClickAction(OnRandomClicked);

            var randomTextWrapper = randomButtonObj.GetComponent<ButtonTextWrapper>();
            randomTextWrapper.t_text.text = "Random";
            randomTextWrapper.t_text.fontSize = 36;

            var randomRectTransform = randomButtonObj.GetComponent<RectTransform>();
            randomRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            randomRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            randomRectTransform.pivot = new Vector2(0.5f, 0.5f);
            randomRectTransform.anchoredPosition = new Vector2(-180f, -50f);
            randomRectTransform.sizeDelta = new Vector2(300, 70);

            var friendliesButtonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            friendliesButtonObj.transform.SetParent(panel.transform, false);

            var originalFriendliesButton = friendliesButtonObj.GetComponent<MyButtonNormal>();
            if (originalFriendliesButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalFriendliesButton);
            }

            UnityEngine.UI.Button butt = friendliesButtonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (butt != null)
            {
                butt.onClick = new();
            }

            var localizeStringEventFriendlies = friendliesButtonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEventFriendlies != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEventFriendlies);
            }

            friendliesButton = friendliesButtonObj.AddComponent<CustomButton>();
            friendliesButton.SetOnClickAction(OnFriendliesClicked);

            var friendliesTextWrapper = friendliesButtonObj.GetComponent<ButtonTextWrapper>();
            friendliesTextWrapper.t_text.text = "Friendlies";
            friendliesTextWrapper.t_text.fontSize = 36;

            var friendliesRectTransform = friendliesButtonObj.GetComponent<RectTransform>();
            friendliesRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            friendliesRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            friendliesRectTransform.pivot = new Vector2(0.5f, 0.5f);
            friendliesRectTransform.anchoredPosition = new Vector2(180f, -50f);
            friendliesRectTransform.sizeDelta = new Vector2(300, 70);
        }

        private void CreateFriendliesUI()
        {
            friendliesTitle = new GameObject("FriendliesTitle");
            friendliesTitle.transform.SetParent(panel.transform, false);

            var titleRect = friendliesTitle.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.85f);
            titleRect.anchorMax = new Vector2(0.5f, 0.85f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(400, 60);

            var titleText = friendliesTitle.AddComponent<TextMeshProUGUI>();
            titleText.text = "Friendlies";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 60;
            titleText.color = Color.white;

            friendliesTitle.SetActive(false);

            var hostButtonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            hostButtonObj.transform.SetParent(panel.transform, false);

            var originalHostButton = hostButtonObj.GetComponent<MyButtonNormal>();
            if (originalHostButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalHostButton);
            }

            UnityEngine.UI.Button button = hostButtonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new();
            }

            var localizeStringEvent = hostButtonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEvent != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEvent);
            }

            hostButton = hostButtonObj.AddComponent<CustomButton>();
            hostButton.SetOnClickAction(OnHostClicked);

            var hostTextWrapper = hostButtonObj.GetComponent<ButtonTextWrapper>();
            hostTextWrapper.t_text.text = "Host";
            hostTextWrapper.t_text.fontSize = 36;

            var hostRectTransform = hostButtonObj.GetComponent<RectTransform>();
            hostRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hostRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hostRectTransform.pivot = new Vector2(0.5f, 0.5f);
            hostRectTransform.anchoredPosition = new Vector2(0, 50f);
            hostRectTransform.sizeDelta = new Vector2(300, 70);

            hostButton.gameObject.SetActive(false);

            codeLabel = new GameObject("CodeLabel");
            codeLabel.transform.SetParent(panel.transform, false);

            var codeLabelRect = codeLabel.AddComponent<RectTransform>();
            codeLabelRect.anchorMin = new Vector2(0.5f, 0.4f);
            codeLabelRect.anchorMax = new Vector2(0.5f, 0.4f);
            codeLabelRect.pivot = new Vector2(0.5f, 0.5f);
            codeLabelRect.anchoredPosition = new Vector2(0, 25);
            codeLabelRect.sizeDelta = new Vector2(300, 40);

            var codeLabelText = codeLabel.AddComponent<TextMeshProUGUI>();
            codeLabelText.text = "Room Code:";
            codeLabelText.alignment = TextAlignmentOptions.Center;
            codeLabelText.fontSize = 40;
            codeLabelText.color = Color.white;

            codeLabel.SetActive(false);

            var codeInputObj = new GameObject("CodeInput");
            codeInputObj.transform.SetParent(panel.transform, false);

            var codeInputRect = codeInputObj.AddComponent<RectTransform>();
            codeInputRect.anchorMin = new Vector2(0.5f, 0.35f);
            codeInputRect.anchorMax = new Vector2(0.5f, 0.35f);
            codeInputRect.pivot = new Vector2(0.5f, 0.5f);
            codeInputRect.anchoredPosition = new Vector2(-80f, -20f);
            codeInputRect.sizeDelta = new Vector2(250, 50);

            var codeInputImage = codeInputObj.AddComponent<Image>();
            codeInputImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            codeInput = codeInputObj.AddComponent<TMP_InputField>();
            codeInput.textComponent = CreateCodeInputText(codeInputObj);
            codeInput.placeholder = CreateCodePlaceholderText(codeInputObj);
            codeInput.characterLimit = 10;

            codeInput.caretWidth = 3;
            codeInput.caretColor = Color.white;
            codeInput.customCaretColor = true;
            codeInput.caretBlinkRate = 0.85f;

            codeInput.selectionColor = new Color(0.65f, 0.8f, 1f, 0.5f);

            codeInput.enabled = false;
            codeInput.enabled = true;

            codeInput.gameObject.SetActive(false);

            var joinButtonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            joinButtonObj.transform.SetParent(panel.transform, false);

            var originalJoinButton = joinButtonObj.GetComponent<MyButtonNormal>();
            if (originalJoinButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalJoinButton);
            }

            UnityEngine.UI.Button butt = joinButtonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (butt != null)
            {
                butt.onClick = new();
            }

            var localizeStringEventJoin = joinButtonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEventJoin != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEventJoin);
            }

            joinButton = joinButtonObj.AddComponent<CustomButton>();
            joinButton.SetOnClickAction(OnJoinClicked);

            var joinTextWrapper = joinButtonObj.GetComponent<ButtonTextWrapper>();
            joinTextWrapper.t_text.text = "Join";
            joinTextWrapper.t_text.fontSize = 36;

            var joinRectTransform = joinButtonObj.GetComponent<RectTransform>();
            joinRectTransform.anchorMin = new Vector2(0.5f, 0.35f);
            joinRectTransform.anchorMax = new Vector2(0.5f, 0.35f);
            joinRectTransform.pivot = new Vector2(0.5f, 0.5f);
            joinRectTransform.anchoredPosition = new Vector2(140f, -20f);
            joinRectTransform.sizeDelta = new Vector2(150, 50);

            joinButton.gameObject.SetActive(false);

            var backButtonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            backButtonObj.transform.SetParent(panel.transform, false);

            var originalBackButton = backButtonObj.GetComponent<MyButtonNormal>();
            if (originalBackButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalBackButton);
            }

            UnityEngine.UI.Button backButton = backButtonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (backButton != null)
            {
                backButton.onClick = new();
            }

            var localizeStringEventBack = backButtonObj.GetComponentInChildren<LocalizeStringEvent>();
            if (localizeStringEventBack != null)
            {
                UnityEngine.Object.DestroyImmediate(localizeStringEventBack);
            }

            friendliesBackButton = backButtonObj.AddComponent<CustomButton>();
            friendliesBackButton.SetOnClickAction(OnFriendliesBackClicked);

            var backTextWrapper = backButtonObj.GetComponent<ButtonTextWrapper>();
            backTextWrapper.t_text.text = "Back";
            backTextWrapper.t_text.fontSize = 36;

            var backRectTransform = backButtonObj.GetComponent<RectTransform>();
            backRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            backRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            backRectTransform.pivot = new Vector2(0.5f, 0.5f);
            backRectTransform.anchoredPosition = new Vector2(0, -200f);
            backRectTransform.sizeDelta = new Vector2(300, 70);

            friendliesBackButton.gameObject.SetActive(false);
        }

        private TextMeshProUGUI CreateCodeInputText(GameObject parent)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.fontSize = 30;
            text.color = Color.white;
            text.margin = new Vector4(10, 0, 10, 0);

            return text;
        }

        private TextMeshProUGUI CreateCodePlaceholderText(GameObject parent)
        {
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(parent.transform, false);

            var rectTransform = placeholderObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var text = placeholderObj.AddComponent<TextMeshProUGUI>();
            text.text = "Enter code...";
            text.alignment = TextAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.fontSize = 22;
            text.color = new Color(1f, 1f, 1f, 0.3f);
            text.margin = new Vector4(10, 0, 10, 0);

            return text;
        }

        protected override void Update()
        {
            base.Update();

            if (playerNameInput == null) return;

            bool isFocused = playerNameInput.isFocused;

            if (wasInputFocused && !isFocused)
            {
                OnPlayerNameEndEdit();
            }
            wasInputFocused = isFocused;
        }

        private void OnPlayerNameEndEdit()
        {
            if (playerNameInput == null) return;

            string newName = playerNameInput.text;

            if (string.IsNullOrWhiteSpace(newName))
            {
                playerNameInput.text = ModConfig.PlayerName.Value;
                return;
            }

            if (filter.IsProfanity(newName))
            {
                newName = filter.CensorString(newName);
                playerNameInput.text = newName;
            }

            newName = newName.Trim();

            ModConfig.PlayerName.Value = newName;
            ModConfig.Save();
            Plugin.Log.LogInfo($"Player name updated to: {ModConfig.PlayerName.Value}");
        }

        private void OnCloseClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);
            CloseModal();
        }

        private void OnRandomClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            Plugin.Instance.Mode.Mode = NetworkModeType.Random;

            UpdateModalContents(false);

            ShowLoader("Connecting...");

            Plugin.Instance.NetworkHandler.HandleNetworking();
            connectionCoroutine = CoroutineRunner.Instance.Run(HandleConnectionStatus());
        }

        private void OnFriendliesClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            Plugin.Instance.Mode.Mode = NetworkModeType.Friendlies;

            UpdateModalContents(false);
            UpdateFriendliesUI(true);
        }

        private void OnHostClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            Plugin.Instance.Mode.Mode = NetworkModeType.Friendlies;
            Plugin.Instance.Mode.Role = Role.Host;

            UpdateFriendliesUI(false);

            ShowLoader("Connecting...");

            Plugin.Instance.NetworkHandler.HandleNetworking();
            connectionCoroutine = CoroutineRunner.Instance.Run(HandleFriendlies());
        }

        private void OnJoinClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            var code = codeInput.text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatusText("Please enter a room code");
                return;
            }

            Plugin.Instance.Mode.Mode = NetworkModeType.Friendlies;
            Plugin.Instance.Mode.Role = Role.Client;
            Plugin.Instance.Mode.RoomCode = code;

            UpdateFriendliesUI(false);

            ShowLoader("Joining room...");

            Plugin.Instance.NetworkHandler.HandleNetworking();
            connectionCoroutine = CoroutineRunner.Instance.Run(HandleFriendlies());
        }

        private void OnFriendliesBackClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            UpdateFriendliesUI(false);
            UpdateModalContents(true);
        }

        private void OnStopClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            if (connectionCoroutine != null)
            {
                CoroutineRunner.Instance.StopCoroutine(connectionCoroutine);
                connectionCoroutine = null;
            }

            Plugin.Instance.NetworkHandler.ResetNetworking();

            HideLoader();

            UpdateModalContents(true);
            stopButton.gameObject.SetActive(false);

            var closeTextWrapper = closeButton.gameObject.GetComponent<ButtonTextWrapper>();
            closeTextWrapper.t_text.text = "Close";
            SetStatusText("");
        }

        private void UpdateModalContents(bool isVisible)
        {
            randomButton.gameObject.SetActive(isVisible);
            friendliesButton.gameObject.SetActive(isVisible);
            closeButton.gameObject.SetActive(isVisible);
            playerNameInput.gameObject.SetActive(isVisible);
            label.SetActive(isVisible);
        }

        private void UpdateFriendliesUI(bool isVisible)
        {
            friendliesTitle.SetActive(isVisible);
            hostButton.gameObject.SetActive(isVisible);
            codeLabel.SetActive(isVisible);
            codeInput.gameObject.SetActive(isVisible);
            joinButton.gameObject.SetActive(isVisible);
            friendliesBackButton.gameObject.SetActive(isVisible);
        }

        private IEnumerator HandleConnectionStatus()
        {
            stopButton.gameObject.SetActive(true);
            var stopTextWrapper = stopButton.gameObject.GetComponent<ButtonTextWrapper>();
            stopTextWrapper.t_text.text = "Stop";

            float timeout = 30f;
            float elapsed = 0f;

            while (elapsed < timeout && !Plugin.Instance.NetworkHandler.IsConnectedToMatchMaker.HasValue)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (!Plugin.Instance.NetworkHandler.IsConnectedToMatchMaker.Value)
            {
                HideLoader();
                SetStatusText($"Failed to connect to server : {Plugin.Instance.NetworkHandler.MatchMakerFailureMessage}");
                Plugin.Instance.NetworkHandler.ResetNetworking();

                stopButton.gameObject.SetActive(false);
                yield return new WaitForSeconds(4f);
                UpdateModalContents(true);
                SetStatusText("");

                yield break;
            }

            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);
            SetStatusText("Waiting for a match...");

            while (!Plugin.Instance.NetworkHandler.HasFoundMatch.HasValue)
            {
                if (Plugin.Instance.NetworkHandler.IsNetworkInterruptedStatus)
                {
                    HideLoader();
                    SetStatusText($"Network interrupted. Please try again : {Plugin.Instance.NetworkHandler.MatchMakerFailureMessage}");
                    Plugin.Instance.NetworkHandler.ResetNetworking();

                    stopButton.gameObject.SetActive(false);

                    yield return new WaitForSeconds(3f);
                    UpdateModalContents(true);
                    SetStatusText("");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            if (!Plugin.Instance.NetworkHandler.HasFoundMatch.HasValue || !Plugin.Instance.NetworkHandler.HasFoundMatch.Value)
            {
                HideLoader();
                SetStatusText($"Failed to connect: {Plugin.Instance.NetworkHandler.MatchMakerFailureMessage}");
                Plugin.Instance.NetworkHandler.ResetNetworking();
                stopButton.gameObject.SetActive(false);
                yield return new WaitForSeconds(4f);
                UpdateModalContents(true);
                SetStatusText("");
                yield break;
            }

            AudioManager.Instance.PlaySfx(AudioManager.Instance.purchaseSfx.sounds[0]);
            HideLoader();
            SetStatusText("Match found!");
            stopButton.gameObject.SetActive(false);

            mainMenu.GoToCharacterSelection();

            var characterMenu = WindowManager.activeWindow as CharacterMenu;
            if (characterMenu != null)
            {
                characterMenu.selectedButton = characterMenu.characterButtons[0];
                characterMenu.b_confirm.SetInteractable(false);
            }

            var role = Plugin.Instance.NetworkHandler.IsHost ? "Host" : "Client";
            var lobbySize = Plugin.Instance.NetworkHandler.GetLobbySize();
            Plugin.StartNotification(("MegabonkTogether", "MatchSuccess"), ("MegabonkTogether", "MatchSuccessDesc"), [role, lobbySize.ToString()]);

            yield return new WaitForSeconds(1f);
            CloseModal();
        }

        private IEnumerator HandleFriendlies()
        {
            stopButton.gameObject.SetActive(true);
            var stopTextWrapper = stopButton.gameObject.GetComponent<ButtonTextWrapper>();
            stopTextWrapper.t_text.text = "Stop";

            float timeout = 30f;
            float elapsed = 0f;

            while (elapsed < timeout && !Plugin.Instance.NetworkHandler.IsConnectedToMatchMaker.HasValue)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (!Plugin.Instance.NetworkHandler.IsConnectedToMatchMaker.Value)
            {
                HideLoader();
                SetStatusText($"Failed to connect to server : {Plugin.Instance.NetworkHandler.MatchMakerFailureMessage}");
                Plugin.Instance.NetworkHandler.ResetNetworking();

                stopButton.gameObject.SetActive(false);
                yield return new WaitForSeconds(4f);
                UpdateFriendliesUI(true);
                SetStatusText("");

                yield break;
            }

            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            elapsed = 0f;
            
            while (elapsed < timeout && !Plugin.Instance.NetworkHandler.HasFoundMatch.HasValue)
            {
                if (Plugin.Instance.NetworkHandler.IsNetworkInterruptedStatus)
                {
                    HideLoader();
                    SetStatusText($"Network interrupted. Please try again : {Plugin.Instance.NetworkHandler.MatchMakerFailureMessage}");
                    Plugin.Instance.NetworkHandler.ResetNetworking();

                    stopButton.gameObject.SetActive(false);

                    yield return new WaitForSeconds(3f);
                    UpdateFriendliesUI(true);
                    SetStatusText("");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (!Plugin.Instance.NetworkHandler.HasFoundMatch.HasValue || !Plugin.Instance.NetworkHandler.HasFoundMatch.Value)
            {
                HideLoader();
                SetStatusText($"Failed to connect: {Plugin.Instance.NetworkHandler.MatchMakerFailureMessage}");
                Plugin.Instance.NetworkHandler.ResetNetworking();
                stopButton.gameObject.SetActive(false);
                yield return new WaitForSeconds(4f);
                UpdateFriendliesUI(true);
                SetStatusText("");
                yield break;
            }

            AudioManager.Instance.PlaySfx(AudioManager.Instance.purchaseSfx.sounds[0]);
            HideLoader();
            SetStatusText("Joined!");
            stopButton.gameObject.SetActive(false);

            mainMenu.GoToCharacterSelection();

            var characterMenu = WindowManager.activeWindow as CharacterMenu;
            if (characterMenu != null)
            {
                characterMenu.selectedButton = characterMenu.characterButtons[0];
                characterMenu.b_confirm.SetInteractable(false);
            }

            if (Plugin.Instance.NetworkHandler.IsHost)
            {
                Plugin.StartNotification(("MegabonkTogether", "FriendliesHostSuccess"), ("MegabonkTogether", "FriendliesHostSuccessDesc"), []);
            }
            else
            {
                Plugin.StartNotification(("MegabonkTogether", "FriendliesClientSuccess"), ("MegabonkTogether", "FriendliesClientSuccessDesc"), [""]);
            }

            yield return new WaitForSeconds(1f);
            CloseModal();
        }
    }
}