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
        private readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();

        protected override Vector2 PanelSize => new Vector2(500, 300);

        protected override void OnUICreated()
        {
            CreateVersionText();

            if (autoUpdaterService.IsThunderstoreBuild())
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(3500);
                    this.CloseModal();
                });
                return;
            }

            CreateUpdateButton();
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
                text.text = $"Version {autoUpdaterService.GetDownloadedVersion()}\nis available!\nPlease update through Thunderstore mod manager.";
            }
            else
            {
                text.text = $"Version {autoUpdaterService.GetDownloadedVersion()}\ndownloaded and ready\nto be applied! Restart the game after the update!";
            }
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 30;
            text.color = Color.white;
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
