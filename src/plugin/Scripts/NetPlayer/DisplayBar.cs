using MegabonkTogether.Helpers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MegabonkTogether.Scripts.NetPlayer
{
    public class DisplayBar : MonoBehaviour
    {
        private GameObject background;
        private GameObject fill;
        private Image fillImage;
        private TextMeshProUGUI text;
        private RectTransform fillRect;
        private RectTransform backgroundRect;

        private Coroutine pulseCoroutine;
        private Coroutine shakeCoroutine;

        private Color originalColor;

        private float maxWidth;
        //private float height;

        private float currentFillAmount = 1f;
        private float targetFillAmount = 1f;
        private float animationSpeed = 5f;

        public void Initialize(Transform parent, Vector2 position, float width, float barHeight, Color fillColor)
        {
            maxWidth = width;
            //height = barHeight;

            background = new GameObject("BarBackground");
            background.transform.SetParent(parent, false);
            backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 1f);
            backgroundRect.anchorMax = new Vector2(0, 1f);
            backgroundRect.pivot = new Vector2(0, 1f);
            backgroundRect.anchoredPosition = position;
            backgroundRect.sizeDelta = new Vector2(width, barHeight);

            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            fill = new GameObject("BarFill");
            fill.transform.SetParent(background.transform, false);
            fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(width, 0);

            fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;

            originalColor = fillColor;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(background.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "100/100";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = Mathf.Max(12, barHeight * 0.6f);
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
        }

        private void Update()
        {
            if (fillRect != null && !Mathf.Approximately(currentFillAmount, targetFillAmount))
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * animationSpeed);
                fillRect.sizeDelta = new Vector2(maxWidth * currentFillAmount, 0);
            }
        }

        public void PulseColor(Color pulseColor, float duration)
        {
            if (pulseCoroutine != null)
            {
                CoroutineRunner.Instance.Stop(pulseCoroutine);
            }
            pulseCoroutine = CoroutineRunner.Instance.Run(PulseColorRoutine(pulseColor, duration));
        }

        public void Shake(float intensity, float duration)
        {
            if (shakeCoroutine != null)
            {
                CoroutineRunner.Instance.Stop(shakeCoroutine);
            }
            shakeCoroutine = CoroutineRunner.Instance.Run(ShakeRoutine(intensity, duration));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            if (backgroundRect == null) yield break;

            Vector2 originalPos = backgroundRect.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (backgroundRect == null) yield break;

                float x = originalPos.x + Random.Range(-intensity, intensity);
                float y = originalPos.y + Random.Range(-intensity, intensity);
                backgroundRect.anchoredPosition = new Vector2(x, y);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (backgroundRect != null)
            {
                backgroundRect.anchoredPosition = originalPos;
            }
        }

        private IEnumerator PulseColorRoutine(Color pulseColor, float duration)
        {
            if (fillImage == null) yield break;

            float elapsed = 0f;
            Color currentColor = fillImage.color;
            while (elapsed < duration)
            {
                if (fillImage == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                fillImage.color = Color.Lerp(pulseColor, currentColor, t);
                yield return null;
            }

            if (fillImage != null)
            {
                fillImage.color = originalColor;
            }
        }

        public void UpdateBar(float current, float max)
        {
            if (fillRect == null || text == null) return;

            targetFillAmount = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            text.text = $"{(int)current}/{(int)max}";
        }

        public void UpdateBarImmediate(float current, float max)
        {
            if (fillRect == null || text == null) return;

            float fillAmount = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            currentFillAmount = fillAmount;
            targetFillAmount = fillAmount;
            fillRect.sizeDelta = new Vector2(maxWidth * fillAmount, 0);
            text.text = $"{(int)current}/{(int)max}";
        }

        public void Resize(float width, float barHeight)
        {
            if (backgroundRect != null)
            {
                backgroundRect.sizeDelta = new Vector2(width, barHeight);
            }

            if (fillRect != null)
            {
                maxWidth = width;
                fillRect.sizeDelta = new Vector2(maxWidth * currentFillAmount, 0);
            }

            if (text != null)
            {
                text.fontSize = Mathf.Max(12, barHeight * 0.6f);
            }
        }

        public RectTransform GetRectTransform()
        {
            return backgroundRect;
        }

        public void Destroy()
        {
            if (background != null)
            {
                Object.Destroy(background);
            }
        }
    }
}
