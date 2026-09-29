using Players;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Utils;

namespace FX {
    public class PlayerHealthVignettePPFX : MonoBehaviour {
        [SerializeField] internal Volume volume;
        [SerializeField] internal int hpThreshold;
        [SerializeField] internal float fadeSpeed;
        [SerializeField] internal Color hurtColor;
        [SerializeField] internal float hurtIntensity;

        private Vignette vignette;
        private Color normalColor;
        private Color targetColor;
        private float normalIntensity;
        private float targetIntensity;

        public void Awake() {
            if (volume == null || !volume.profile.TryGet(out vignette)) {
                Debug.LogError("PlayerHealthVignettePPFX: no Vignette override found on the Volume profile.", this);
                return;
            }

            // Values are ignored by the volume system unless the override is enabled.
            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.active = true;

            normalColor = vignette.color.value;
            normalIntensity = vignette.intensity.value;
            targetColor = normalColor;
            targetIntensity = normalIntensity;
        }

        public void OnEnable() {
            Player.OnHealthChanged += OnPlayerHurt;
        }

        public void OnDisable() {
            Player.OnHealthChanged -= OnPlayerHurt;
        }

        private void OnPlayerHurt(int health) {
            targetIntensity = health > hpThreshold ? normalIntensity : hurtIntensity;
            targetColor = health > hpThreshold ? normalColor : hurtColor;
        }

        public void Update() {
            if (vignette == null) return;

            vignette.color.value = MathUtils.Lerpish(vignette.color.value, targetColor, Time.deltaTime * fadeSpeed);
            vignette.intensity.value = MathUtils.Lerpish(vignette.intensity.value, targetIntensity, Time.deltaTime * fadeSpeed);
        }
    }
}
