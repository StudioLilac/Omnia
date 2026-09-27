using System;
using System.Collections;
using Background;
using Initializers;
using Players;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utils;

namespace Scenes {
    public class Title : MonoBehaviour {
        [SerializeField] private GameObject titleSprite;         // Omnia (Sprite)
        [SerializeField] private GameObject subtitleSprite;      // The Journey Upwards (Sprite)

        [SerializeField] private TextMeshPro taglineTMP;     // Causa Fiunt (TMP UI)
        [SerializeField] private TextMeshPro quoteTMP;       // Everything happens for a reason (TMP UI)

        [SerializeField] private FadeScreenHandler fadeScreen;
        [SerializeField] private FadeScreenHandler strongFadeHandler;

        [SerializeField] private GameObject dustParent;

        [SerializeField] private Button[] buttons;

        [SerializeField] private GameObject launchableDinkyPrefab;
        [SerializeField] private Transform dinkyLaunchPoint;
        [SerializeField] private float launchVariation;
        [SerializeField] private float velocityVariation;

        private Image[] dustImages;

        private string taglineText;
        private string quoteText;

        [SerializeField] private Slider musicSlider;
        [SerializeField] private Toggle toggle;





        public void QuitGame() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        private void Awake() {
            strongFadeHandler.SetLightScreen();
        }

        private void Start() {
            if (AudioManager.Instance.IsMuted()) {
                AudioManager.Instance.SetBGMVolume(0f);
                AudioManager.Instance.SetSFXVolume(0f);
                AudioManager.Instance.SetAmbientVolume(0f);
                ToggleAudio();
                toggle.isOn = true;
                musicSlider.value = PauseMenu.musicVol;
                MusicVolume();
            }
            musicSlider.value = PauseMenu.musicVol;
            MusicVolume();
            PlayerDataManager.Instance.warpedDepthsProgress = 0;
            dustImages = dustParent.GetComponentsInChildren<Image>();

            foreach (var img in dustImages) {
                img.gameObject.AddComponent<FloatingImage>();
            }

            // Text is not displayed at the beginning, but we should store their values.
            taglineText = taglineTMP.text;
            taglineTMP.text = "";

            quoteText = quoteTMP.text;
            quoteTMP.text = "";

            AudioManager.Instance.PlayBGM(AudioTracks.LullabyForAScrapyard);
            fadeScreen.SetLightScreen();

            DisablePersistentSingletons.DisableHUD();
            DisablePersistentSingletons.DisableInventory();
            DisablePersistentSingletons.DisablePause();

        }

        public void StartGame() {
            foreach (Button b in buttons)
            {
                b.interactable = false;
            }
            StartCoroutine(StartGameSequence());
        }

        public void GoToLevelSelect() {
            SceneManager.LoadScene("LevelSelect");
        }

        public void GoToWarpedDepths() {
            SceneManager.LoadScene("WarpedDepths");
        }

        private IEnumerator StartGameSequence() {
            yield return StartCoroutine(fadeScreen.FadeInDarkScreen(2f));
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(Typewriter.TypewriterEffect(taglineTMP, taglineText, 0.15f));
            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(Typewriter.TypewriterEffect(quoteTMP, quoteText, 0.1f));
            yield return new WaitForSeconds(3f);
            yield return StartCoroutine(strongFadeHandler.FadeInDarkScreen(2f));

            // Done to avoid scene transition
            SceneManager.LoadScene("2_Opening");
        }

        public void LaunchDinky() {
            float launchX = UnityEngine.Random.Range(-launchVariation, launchVariation);
            Vector3 spawnPosition = new Vector3(dinkyLaunchPoint.position.x + launchX, dinkyLaunchPoint.position.y, dinkyLaunchPoint.position.z);

            GameObject dinkyInstance = Instantiate(launchableDinkyPrefab, spawnPosition, Quaternion.identity);
            Rigidbody2D rb = dinkyInstance.GetComponent<Rigidbody2D>();

            if (rb != null) {
                rb.gravityScale = 1f;

                float horizontalForce = UnityEngine.Random.Range(-velocityVariation, velocityVariation);
                rb.velocity = new Vector2(horizontalForce, 15);

                // Apply random spin
                float spinForce = UnityEngine.Random.Range(250f, 400f) * (UnityEngine.Random.value > 0.5f ? 1 : -1);
                rb.angularVelocity = spinForce;

                Destroy(dinkyInstance, 4f);
            }
            else {
                Debug.LogError("Launchable Dinky prefab is missing a Rigidbody2D component!");
            }
        }


        private IEnumerator FadeInSprite(SpriteRenderer sprite, float duration) {
            float elapsedTime = 0f;
            Color startColor = sprite.color;
            Color targetColor = startColor;
            targetColor.a = 1f;

            while (elapsedTime < duration) {
                elapsedTime += Time.deltaTime;
                sprite.color = Color.Lerp(startColor, targetColor, elapsedTime / duration);
                yield return null;
            }

            sprite.color = targetColor;
        }

        public void ToggleAudio() {
            AudioManager.Instance.ToggleAudio();
        }

        public void MusicVolume() {
            PauseMenu.musicVol = musicSlider.value;
            AudioManager.Instance.SetBGMVolume(musicSlider.value);
            AudioManager.Instance.SetSFXVolume(musicSlider.value);
            AudioManager.Instance.SetAmbientVolume(musicSlider.value);
        }


    }
}
