using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Omnia.Rendering {
    // responsible for the ambient (global) light 2d object in every gameplay scene.
    // Exposes methods to play with ambient lighting dynamically.
    public class LightManager : MonoBehaviour {
        [SerializeField] private Light2D globalAmbientLight = null!;
        private Coroutine? fade;

        public void Apply(AmbientSettings settings) {
            globalAmbientLight.color = settings.color;
            globalAmbientLight.intensity = settings.intensity;
        }

        public void FadeTo(AmbientSettings target, float duration) {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(DoFade(target, duration));
        }

        private IEnumerator DoFade(AmbientSettings target, float duration) {
            var startColor = globalAmbientLight.color;
            var startIntensity = globalAmbientLight.intensity;
            for (float t = 0; t < duration; t += Time.deltaTime) {
                float k = t / duration;
                globalAmbientLight.color = Color.Lerp(startColor, target.color, k);
                globalAmbientLight.intensity = Mathf.Lerp(startIntensity, target.intensity, k);
                yield return null;
            }

            Apply(target);
        }
    }

    public struct AmbientSettings {
        public Color color;
        public float intensity;
    }
}
