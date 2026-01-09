using MegabonkTogether.Helpers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MegabonkTogether.Scripts.Modal
{
    internal abstract class ModalBase : MonoBehaviour
    {
        protected GameObject blocker;
        protected GameObject panel;
        protected GameObject loader;
        protected TextMeshProUGUI statusText;
        protected bool isAnimating = false;
        private string nextMessage = "";

        protected virtual Vector2 PanelSize => new Vector2(700, 550);
        protected virtual Vector2 LoaderSize => new Vector2(80, 80);
        protected virtual Vector2 StatusTextSize => new Vector2(500, 70);
        protected virtual float StatusTextFontSize => 36f;
        protected virtual Color PanelBackgroundColor => new Color(0.1f, 0.1f, 0.1f, 0.9f);
        protected virtual Color BlockerColor => new Color(0, 0, 0, 0.5f);

        public virtual void Start()
        {
            CreateUI();
        }

        protected virtual void CreateUI()
        {
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Plugin.Log.LogError("Canvas not found!");
                return;
            }

            CreateBlocker(canvasObj);
            CreatePanel(canvasObj);
            CreateLoader();
            CreateStatusText();

            OnUICreated();
        }

        protected virtual void OnUICreated() { }

        protected void CreateBlocker(GameObject canvas)
        {
            blocker = new GameObject("ModalBlocker");
            blocker.transform.SetParent(canvas.transform, false);

            var rectTransform = blocker.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var image = blocker.AddComponent<Image>();
            image.color = BlockerColor;
            image.raycastTarget = true;

            blocker.transform.SetAsFirstSibling();
        }

        protected void CreatePanel(GameObject canvas)
        {
            panel = new GameObject("ModalPanel");
            panel.transform.SetParent(canvas.transform, false);

            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = PanelSize;

            var image = panel.AddComponent<Image>();
            image.color = PanelBackgroundColor;
        }

        protected void CreateLoader()
        {
            loader = GameObject.Instantiate(new GameObject("Loader"));
            loader.transform.SetParent(panel.transform, false);

            var rectTransform = loader.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = LoaderSize;

            var image = loader.AddComponent<Image>();
            image.color = Color.white;

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0.75f;

            loader.SetActive(false);
        }

        protected void CreateStatusText()
        {
            var textObj = new GameObject("StatusText");
            textObj.transform.SetParent(panel.transform, false);

            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = StatusTextSize;

            statusText = textObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = StatusTextFontSize;
            statusText.color = Color.white;
        }

        protected void ShowLoader(string message = "")
        {
            loader.SetActive(true);
            if (!string.IsNullOrEmpty(message))
            {
                statusText.text = message;
            }

            if (!isAnimating)
            {
                CoroutineRunner.Instance.Run(AnimateLoader());
            }
        }

        protected void HideLoader()
        {
            isAnimating = false;
            loader.SetActive(false);
        }

        protected void SetStatusText(string text)
        {
            if (statusText == null)
            {
                nextMessage = text;
                return;
            }
            statusText.text = text;
        }

        protected virtual void Update()
        {
            if (!string.IsNullOrEmpty(nextMessage) && statusText != null)
            {
                statusText.text = nextMessage;
                nextMessage = "";
            }
        }

        protected IEnumerator AnimateLoader()
        {
            isAnimating = true;
            float rotation = 0f;

            while (isAnimating && loader != null && loader.activeSelf)
            {
                rotation += 180f * Time.deltaTime;
                loader.transform.rotation = Quaternion.Euler(0, 0, rotation);
                yield return null;
            }

            isAnimating = false;
        }

        protected void CloseModal()
        {
            if (blocker != null)
            {
                Destroy(blocker);
            }
            if (panel != null)
            {
                Destroy(panel);
            }
            Destroy(gameObject);
        }

        public virtual void OnDestroy()
        {
            if (blocker != null)
            {
                Destroy(blocker);
            }
        }
    }
}
