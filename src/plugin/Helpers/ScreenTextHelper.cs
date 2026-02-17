using Assets.Scripts.Utility;
using System.Collections;
using UnityEngine;

namespace MegabonkTogether.Helpers
{
    public static class ScreenTextHelper
    {
        private static TMPro.TextMeshProUGUI textComponent;
        private static Coroutine fadeRoutine;

        public static void Show(string text, Vector2 anchoredPosition, bool fade = true)
        {
            if (textComponent == null)
            {
                var go = new GameObject("ScreenTextHelper");
                textComponent = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            textComponent.text = text;
            textComponent.enabled = true;
            textComponent.transform.SetParent(UiManager.Instance.transform);
            textComponent.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textComponent.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textComponent.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            textComponent.rectTransform.anchoredPosition = anchoredPosition;
            textComponent.alignment = TMPro.TextAlignmentOptions.Center;
            textComponent.rectTransform.sizeDelta = new Vector2(600, 200);
            textComponent.fontSize = 40;
            textComponent.color = Color.white;

            StopFadeRoutine();

            if (fade)
            {
                fadeRoutine = CoroutineRunner.Instance.Run(FadeRoutine());
            }
        }

        public static void Clear()
        {
            StopFadeRoutine();

            if (textComponent != null)
            {
                textComponent.enabled = false;
            }
        }

        private static void StopFadeRoutine()
        {
            if (fadeRoutine != null)
            {
                CoroutineRunner.Instance.Stop(fadeRoutine);
                fadeRoutine = null;
            }
        }

        private static IEnumerator FadeRoutine()
        {
            while (textComponent != null && textComponent.enabled)
            {
                if (!MyTime.paused)
                {
                    textComponent.enabled = false;
                    yield break;
                }

                float t = Mathf.PingPong(Time.time * 1.5f, 1f);
                float alpha = Mathf.Lerp(0.2f, 1f, t);
                textComponent.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }
    }
}
