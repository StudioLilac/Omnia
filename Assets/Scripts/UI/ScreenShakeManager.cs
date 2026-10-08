using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class ScreenShakeManager : PersistentSingleton<ScreenShakeManager> {
    private Unity.Cinemachine.CinemachineBasicMultiChannelPerlin perlinNoise;

    protected override void OnAwake() {
        perlinNoise = FindObjectOfType<Unity.Cinemachine.CinemachineVirtualCamera>()?.GetComponentInChildren<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>();
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        perlinNoise = FindObjectOfType<Unity.Cinemachine.CinemachineVirtualCamera>()?.GetComponentInChildren<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>();
    }

    public void Shake(float intensity = 1.0f, float duration = 0.5f) {
        if (!perlinNoise || !PauseMenu.ScreenShake) return;
        StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration) {
        float elapsed = 0f;

        // Set the shake values
        perlinNoise.m_AmplitudeGain = intensity;
        perlinNoise.m_FrequencyGain = intensity;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            // Gradually reduce the amplitude over time (decay effect)
            float decayFactor = Mathf.Lerp(1f, 0f, elapsed / duration);
            perlinNoise.m_AmplitudeGain = intensity * decayFactor;

            yield return null;
        }

        // Reset to 0 instead of original values as multiple screen shakes at a time causes issues
        perlinNoise.m_AmplitudeGain = 0;
        perlinNoise.m_FrequencyGain = 0;
    }
}
