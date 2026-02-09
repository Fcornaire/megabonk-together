using MegabonkTogether.Common;
using MegabonkTogether.Scripts.Button;
using Microsoft.Extensions.DependencyInjection;
using TMPro;
using UnityEngine;

namespace MegabonkTogether.Scripts.Modal
{
    internal class UpdateAvailableModal : ModalBase
    {
        private CustomButton updateButton;
        private CustomButton closeButton;
        private readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();

        protected override Vector2 PanelSize => new(500, 300);

        protected override void OnUICreated()
        {
            foreach (var button in WindowManager.activeWindow.allButtons)
            {
                button.SetInteractable(false);
            }

            CreateVersionText();

            if (autoUpdaterService.IsThunderstoreBuild())
            {
                CreateCloseButton();
            }
            else
            {
                CreateUpdateButton();
            }
        }

        protected void Update()
        {
            if (updateButton != null)
            {
                updateButton.SetInteractable(true);
            }

            if (closeButton != null)
            {
                closeButton.SetInteractable(true);
            }
        }

        private void CreateVersionText()
        {
            var textObj = new GameObject("VersionText");
            textObj.transform.SetParent(panel.transform, false);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.6f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(450, 120);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            if (autoUpdaterService.IsThunderstoreBuild())
            {
                text.text = $"Megabonk together Version {autoUpdaterService.GetDownloadedVersion()}\nis available!\nPlease update through Thunderstore mod manager.";
            }
            else
            {
                text.text = $"Megabonk together Version {autoUpdaterService.GetDownloadedVersion()}\ndownloaded and ready\nto be applied! Restart the game after the update!";
            }
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 30;
            text.color = Color.white;
        }

        private void CreateCloseButton()
        {
            var mainMenu = Plugin.Instance.GetMainMenu();
            if (mainMenu == null)
            {
                Plugin.Log.LogError("MainMenu not found for close button!");
                return;
            }

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

            closeButton = buttonObj.AddComponent<CustomButton>();
            closeButton.SetOnClickAction(OnCloseClicked);

            var textWrapper = buttonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = "Close";
            textWrapper.t_text.fontSize = 32;

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.15f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.15f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(200, 50);
        }

        private void CreateUpdateButton()
        {
            var mainMenu = Plugin.Instance.GetMainMenu();
            if (mainMenu == null)
            {
                Plugin.Log.LogError("MainMenu not found for update button!");
                return;
            }

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

            updateButton = buttonObj.AddComponent<CustomButton>();
            updateButton.SetOnClickAction(OnUpdateClicked);

            var textWrapper = buttonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = "Quit & Update";
            textWrapper.t_text.fontSize = 36;

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, -30);
            rectTransform.sizeDelta = new Vector2(300, 70);
        }

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);
            CloseModal();

            foreach (var button in WindowManager.activeWindow.allButtons)
            {
                button.SetInteractable(true);
            }
        }

        private void OnUpdateClicked()
        {
            AudioManager.Instance.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);

            if (autoUpdaterService.IsThunderstoreBuild())
            {
                return;
            }

            Application.Quit();
        }
    }
}
