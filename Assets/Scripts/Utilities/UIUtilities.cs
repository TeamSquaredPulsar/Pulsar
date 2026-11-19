using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pulsar
{
    public static class UIUtilities
    {
        public static IEnumerator FadeIn<T>(T graphic, float duration) where T : Graphic
        {
            return Fade(graphic, 0f, 1f, duration);
        }

        public static IEnumerator FadeOut<T>(T graphic, float duration) where T : Graphic
        {
            return Fade(graphic, 1f, 0f, duration);
        }

        private static IEnumerator Fade<T>(T graphic, float from, float to, float duration)
            where T : Graphic
        {
            float elapsed = 0f;
            while (graphic && elapsed < duration)
            {
                Color color = graphic.color;
                color.a = Mathf.Lerp(from, to, elapsed / duration);
                graphic.color = color;
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            if (graphic)
            {
                Color color = graphic.color;
                color.a = to;
                graphic.color = color;
            }
        }
    }
}
