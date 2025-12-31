using MegabonkTogether.Configuration;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Button;
using MegabonkTogether.Scripts.Modal;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MegabonkTogether.Scripts
{
    internal class NetworkMenuTab : ModalBase
    {
        private CustomButton startButton;
        private CustomButton closeButton;
        private CustomButton stopButton;
        private TMP_InputField playerNameInput;
        private MainMenu mainMenu;
        private GameObject label;
        private bool wasInputFocused;
        private Coroutine connectionCoroutine;
        private ProfanityFilter.ProfanityFilter filter;

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
            CreateStartButton();
            CreateStopButton();
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

            closeButton = buttonObj.AddComponent<CustomButton>();
            closeButton.SetOnClickAction(OnCloseClicked);

            var textWrapper = buttonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = "Close";
            textWrapper.t_text.fontSize = 36;

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, -150f);
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
            Plugin.Instance.PlayTogetherButton.ResetSelected();
            CloseModal();
        }

        private void CreateStartButton()
        {
            var buttonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            buttonObj.transform.SetParent(panel.transform, false);

            var originalButton = buttonObj.GetComponent<MyButtonNormal>();
            if (originalButton != null)
            {
                UnityEngine.Object.DestroyImmediate(originalButton);
            }

            startButton = buttonObj.AddComponent<CustomButton>();
            startButton.SetOnClickAction(OnStartClicked);

            var textWrapper = buttonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = $"Start !";
            textWrapper.t_text.fontSize = 36;

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, -50f);
            rectTransform.sizeDelta = new Vector2(300, 70);
        }

        private void OnStartClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            UpdateModalContents(false);

            ShowLoader("Connecting...");

            Plugin.Instance.NetworkHandler.HandleNetworking();
            connectionCoroutine = CoroutineRunner.Instance.Run(HandleConnectionStatus());
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

            var startTextWrapper = startButton.gameObject.GetComponent<ButtonTextWrapper>();
            startTextWrapper.t_text.text = "Start !";
            var closeTextWrapper = closeButton.gameObject.GetComponent<ButtonTextWrapper>();
            closeTextWrapper.t_text.text = "Close";
            SetStatusText("");
        }

        private void UpdateModalContents(bool isVisible)
        {
            startButton.gameObject.SetActive(isVisible);
            closeButton.gameObject.SetActive(isVisible);
            playerNameInput.gameObject.SetActive(isVisible);
            label.SetActive(isVisible);
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
                var startTextWrapper = startButton.gameObject.GetComponent<ButtonTextWrapper>();
                startTextWrapper.t_text.text = $"Start !";
                var closeTextWrapper = closeButton.gameObject.GetComponent<ButtonTextWrapper>();
                closeTextWrapper.t_text.text = "Close";
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
                    var startTextWrapper = startButton.gameObject.GetComponent<ButtonTextWrapper>();
                    startTextWrapper.t_text.text = $"Start !";
                    var closeTextWrapper = closeButton.gameObject.GetComponent<ButtonTextWrapper>();
                    closeTextWrapper.t_text.text = "Close";
                    SetStatusText("");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            if (!Plugin.Instance.NetworkHandler.HasFoundMatch.Value)
            {
                HideLoader();
                SetStatusText($"Failed to connect: {Plugin.Instance.NetworkHandler.MatchMakerFailureMessage}");
                Plugin.Instance.NetworkHandler.ResetNetworking();
                stopButton.gameObject.SetActive(false);
                yield return new WaitForSeconds(4f);
                UpdateModalContents(true);
                var startTextWrapper = startButton.gameObject.GetComponent<ButtonTextWrapper>();
                startTextWrapper.t_text.text = $"Start !";
                var closeTextWrapper = closeButton.gameObject.GetComponent<ButtonTextWrapper>();
                closeTextWrapper.t_text.text = "Close";
                SetStatusText("");
                yield break;
            }

            AudioManager.Instance.PlaySfx(AudioManager.Instance.purchaseSfx.sounds[0]);
            HideLoader();
            SetStatusText("Match found!");
            stopButton.gameObject.SetActive(false);

            Plugin.Instance.PlayTogetherButton.ResetSelected();
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
    }
}
