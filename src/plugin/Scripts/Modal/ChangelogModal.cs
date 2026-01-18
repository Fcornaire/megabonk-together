using MegabonkTogether.Common;
using MegabonkTogether.Configuration;
using MegabonkTogether.Helpers;
using MegabonkTogether.Scripts.Button;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MegabonkTogether.Scripts.Modal
{
    internal class ChangelogModal : ModalBase
    {
        private CustomButton closeButton;
        private ScrollRect scrollRect;
        private readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();
        private ICollection<VersionChanges> changes;

        protected override Vector2 PanelSize => new Vector2(700, 600);

        public void SetChanges(ICollection<VersionChanges> changes)
        {
            this.changes = changes;
        }

        protected override void OnUICreated()
        {
            if (changes != null && changes.Count > 0)
            {
                BuildUI();
            }
        }

        private void BuildUI()
        {
            CreateTitle();
            CreateScrollableContent();
            CreateCloseButton();
        }

        private void CreateTitle()
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);

            var rectTransform = titleObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -20);
            rectTransform.sizeDelta = new Vector2(600, 60);

            var text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "MegabonkTogether : What's New ?";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 30;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.3f, 0.8f, 0.3f);
        }

        private void CreateScrollableContent()
        {
            var scrollViewObj = new GameObject("ScrollView");
            scrollViewObj.transform.SetParent(panel.transform, false);

            var scrollViewRect = scrollViewObj.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.05f, 0.15f);
            scrollViewRect.anchorMax = new Vector2(0.95f, 0.85f);
            scrollViewRect.offsetMin = Vector2.zero;
            scrollViewRect.offsetMax = Vector2.zero;

            scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 30f;

            var scrollViewImage = scrollViewObj.AddComponent<Image>();
            scrollViewImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

            var scrollViewMask = scrollViewObj.AddComponent<Mask>();
            scrollViewMask.showMaskGraphic = true;

            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);

            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10, 10);
            viewportRect.offsetMax = new Vector2(-30, -10);

            scrollRect.viewport = viewportRect;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 1000);

            scrollRect.content = contentRect;

            var changelogText = BuildChangelogText();

            var textObj = new GameObject("ChangelogText");
            textObj.transform.SetParent(contentObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 1);
            textRect.anchoredPosition = new Vector2(0, 0);
            textRect.sizeDelta = new Vector2(0, 800);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = changelogText;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.fontSize = 25;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.margin = new Vector4(10, 10, 10, 10);
            tmp.overflowMode = TextOverflowModes.Overflow;

            var contentSizeFitter = textObj.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            CoroutineRunner.Instance.Run(UpdateContentSize(contentRect, tmp));

            CreateScrollbar(scrollViewObj);
        }

        private static IEnumerator UpdateContentSize(RectTransform contentRect, TextMeshProUGUI tmp)
        {
            yield return new WaitForEndOfFrame();

            tmp.ForceMeshUpdate();

            yield return new WaitForEndOfFrame();

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            var textHeight = tmp.textBounds.size.y;
            var preferredHeight = tmp.preferredHeight;
            var finalHeight = Mathf.Max(textHeight, preferredHeight) + 60;

            contentRect.sizeDelta = new Vector2(0, finalHeight);
        }

        private string BuildChangelogText()
        {
            if (changes == null || changes.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.AppendLine("<color=#00D9FF><size=25>Thanks for playing!</size></color>");
            sb.AppendLine();
            sb.AppendLine("<color=#00D9FF><size=20>If you like the mod, consider a small support on Patreon at</size></color>");
            sb.AppendLine("<color=#00D9FF><size=20>https://www.patreon.com/c/DShadModdingAdventure</size></color>");
            sb.AppendLine();
            sb.AppendLine("<color=#555555>────────────────────────</color>");
            sb.AppendLine();

            foreach (var versionChange in changes)
            {
                sb.AppendLine($"<color=#FFD700><size=32><b>v{versionChange.Version}</b></size></color>"); // Version header in gold (#FFD700)
                sb.AppendLine();

                foreach (var change in versionChange.Changes)
                {
                    sb.AppendLine($"  <color=#AAAAAA>•</color> {change}"); // Each change with in gray (#AAAAAA)
                }

                sb.AppendLine();
                sb.AppendLine("<color=#555555>────────────────────────</color>"); // Separator line in dark gray (#555555)
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void CreateScrollbar(GameObject scrollView)
        {
            var scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(scrollView.transform, false);

            var scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-20, 10);
            scrollbarRect.offsetMax = new Vector2(0, -10);
            scrollbarRect.sizeDelta = new Vector2(20, 0);

            var scrollbarImage = scrollbarObj.AddComponent<Image>();
            scrollbarImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var verticalScrollbar = scrollbarObj.AddComponent<Scrollbar>();
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Handle Area
            var handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(scrollbarObj.transform, false);

            var handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            // Handle
            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);

            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(1, 0.3f);
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);

            verticalScrollbar.targetGraphic = handleImage;
            verticalScrollbar.handleRect = handleRect;

            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }

        private void CreateCloseButton()
        {
            var mainMenu = Plugin.Instance.GetMainMenu();

            var buttonObj = GameObject.Instantiate(mainMenu.btnPlay.gameObject);
            buttonObj.transform.SetParent(panel.transform, false);

            var originalButton = buttonObj.GetComponent<MyButtonNormal>();
            if (originalButton != null)
            {
                Object.DestroyImmediate(originalButton);
            }

            UnityEngine.UI.Button button = buttonObj.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            }

            var localizeStringEvent = buttonObj.GetComponentInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>();
            if (localizeStringEvent != null)
            {
                Object.DestroyImmediate(localizeStringEvent);
            }

            closeButton = buttonObj.AddComponent<CustomButton>();
            closeButton.SetOnClickAction(OnCloseClicked);

            var textWrapper = buttonObj.GetComponent<ButtonTextWrapper>();
            textWrapper.t_text.text = "Got it!";
            textWrapper.t_text.fontSize = 36;

            var rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.anchoredPosition = new Vector2(0, 20);
            rectTransform.sizeDelta = new Vector2(250, 60);
        }

        protected void Update()
        {
            if (closeButton != null)
            {
                closeButton.SetInteractable(true);
            }
        }

        private void OnCloseClicked()
        {
            foreach (var button in WindowManager.activeWindow.allButtons)
            {
                button.SetInteractable(true);
            }

            AudioManager.Instance?.PlaySfx(AudioManager.Instance.uiSelect.sounds[0]);
            ModConfig.PreviousVersion.Value = autoUpdaterService.GetCurrentVersion();
            ModConfig.ShowChangelog.Value = false;
            ModConfig.Save();
            CloseModal();
        }

        public static void Show(ICollection<VersionChanges> changes)
        {
            var go = new GameObject("ChangelogModal");

            var modal = go.AddComponent<ChangelogModal>();
            modal.SetChanges(changes);

            foreach (var button in WindowManager.activeWindow.allButtons)
            {
                button.SetInteractable(false);
            }
        }
    }
}
