using System.Collections;

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class ScreenShakeManager : PersistentSingleton<ScreenShakeManager> {
    private CinemachineBasicMultiChannelPerlin perlinNoise;
    private Coroutine shakeCoroutine;

    protected override void OnAwake() {
        FindPerlinNoise();
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = null;
        FindPerlinNoise();
    }

    public void Shake(float intensity = 1.0f, float duration = 0.5f) {
        if (!PauseMenu.ScreenShake) return;

        if (!perlinNoise || !perlinNoise.IsValid) FindPerlinNoise();
        if (!perlinNoise) return;

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    private void FindPerlinNoise() {
        perlinNoise = FindAnyObjectByType<CinemachineBasicMultiChannelPerlin>();
        if (perlinNoise && !perlinNoise.IsValid) perlinNoise = null;
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration) {
        float elapsed = 0f;

        // Set the shake values
        perlinNoise.AmplitudeGain = intensity;
        perlinNoise.FrequencyGain = intensity;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            // Gradually reduce the amplitude over time (decay effect)
            float decayFactor = Mathf.Lerp(1f, 0f, elapsed / duration);
            perlinNoise.AmplitudeGain = intensity * decayFactor;

            yield return null;
        }

        perlinNoise.AmplitudeGain = 0;
        perlinNoise.FrequencyGain = 0;

        shakeCoroutine = null;
    }
}
