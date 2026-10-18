using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Utils {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class FadeScreenHandler : MonoBehaviour
    {
        [SerializeField] private Image fadeScreen;

        private bool fadeActive = false;

        public void SetDarkScreen()
        {
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, 1f);
        }

        public void SetLightScreen()
        {
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, 0f);
        }

        public IEnumerator FadeInLightScreen(float duration)
        {
            yield return StartCoroutine(FadeBackground(1f, 0f, duration));
        }

        public IEnumerator FadeInDarkScreen(float duration)
        {
            yield return StartCoroutine(FadeBackground(0f, 1f, duration));
        }

        //set (@param darkenScene) true to fade **combat background** in, false to fade out
        private IEnumerator FadeCombatBackground(bool darkenScene)
        {

            float startValue = fadeScreen.color.a;
            float duration = 1f;

            float endValue;
            if (darkenScene)
            {
                endValue = 0.8f;
            }
            else
            {
                startValue = Mathf.Max(fadeScreen.color.a - 0.3f, 0f); //Clamped to prevent visual nausua with strange alpha change
                endValue = 0f;
            }

            yield return StartCoroutine(FadeBackground(startValue, endValue, duration));
        }

        //Fade Background that gives you more control over the level of fade
        private IEnumerator FadeBackground(float startAlpha, float endAlpha, float duration)
        {
            if (fadeActive) yield break;
            fadeActive = true;

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                // Use an ease-out function to slow the fade at the end
                float easedT = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out

                float newAlpha = Mathf.Lerp(startAlpha, endAlpha, easedT);
                fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, newAlpha);

                yield return null;
            }

            fadeActive = false;
        }

    }

}

public static class FadeHelpers {

    public static IEnumerator FadeSpriteRendererColor(SpriteRenderer fadeScreen, Color startValue, Color endValue, float duration) {

        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / duration;

            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out

            Color newColor = Color.Lerp(startValue, endValue, easedT);
            fadeScreen.color = newColor; 

            yield return null;
        }

        fadeScreen.color = endValue;
    }

    public static IEnumerator FadeSpriteRenderer(SpriteRenderer fadeScreen, float startAlpha, float endAlpha, float duration) {

        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Use an ease-out function to slow the fade at the end
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out

            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, easedT);
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, newAlpha);

            yield return null;
        }
    }

    public static IEnumerator FadeMaterialColor(Material material, int propertyID, Color baseColor, float startAlpha, float endAlpha, float duration) {

        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out

            baseColor.a = Mathf.Lerp(startAlpha, endAlpha, easedT);
            material.SetColor(propertyID, baseColor);

            yield return null;
        }

        baseColor.a = endAlpha;
        material.SetColor(propertyID, baseColor);
    }

}
