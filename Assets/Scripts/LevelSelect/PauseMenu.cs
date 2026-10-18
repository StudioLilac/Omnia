using System.Collections;
using System.Collections.Generic;
using Initializers;
using Players.Buff;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;

public class PauseMenu : PersistentSingleton<PauseMenu> {
    public static bool IsPaused => Instance != null && Instance.paused;
    public static bool ScreenShake = true;
    private bool paused;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Canvas pauseCanvas;

    [SerializeField] private GameObject history;
    [SerializeField] private GameObject controls;
    [SerializeField] private GameObject buttonparent;


    [SerializeField] private TextMeshProUGUI historyText;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicToggle;


    [SerializeField] private InputActionReference pauseAction;

    // Workaround To Import Mute Icon's Toggle Image from Title since it is not possible to toggle the image without
    // the associated onValueChanged function being called
    private void OnEnable() {
        if (AudioManager.Instance.IsMuted() && !musicToggle.isOn) {
            AudioManager.Instance.SetBGMVolume(0f);
            AudioManager.Instance.SetSFXVolume(0f);
            AudioManager.Instance.SetAmbientVolume(0f);
            ToggleMusic();
            musicToggle.isOn = true;
            musicSlider.value = PauseMenu.musicVol;
            MusicVolume();
        }
    }

    public static float musicVol = 0.7f;



    public delegate void PauseMenuEventHandler();

    public static event PauseMenuEventHandler OnPauseMenuActivate;
    public static event PauseMenuEventHandler OnPauseMenuDeactivate;


    bool isDialogueHistoryShowing = false;
    bool isControlsShowing = false;

    public bool DialogueHistoryState {
        get { return isDialogueHistoryShowing; }
        set {
            if (value) {
                ShowHistory();
            } else {
                HideHistory();
            }

            isDialogueHistoryShowing = value;
        }
    }

    public bool ControlsState {
        get { return isControlsShowing; }
        set {
            if (value) {
                ShowControls();
            } else {
                HideControls();
            }

            isControlsShowing = value;
        }
    }

    void ShowHistory() {
        RenderHistory();
        history.SetActive(true);
        buttonparent.SetActive(false);
    }

    void HideHistory() {
        history.SetActive(false);
        buttonparent.SetActive(true);
    }

    void RenderHistory() {
        historyText.text = "Dialogue history is unavailable during the Yarn migration.";
    }

    public void SetHistoryState(bool state) {
        DialogueHistoryState = state;
    }

    void ShowControls() {
        controls.SetActive(true);
        buttonparent.SetActive(false);
    }

    void HideControls() {
        controls.SetActive(false);
        buttonparent.SetActive(true);
    }

    public void SetControlsState(bool state) {
        ControlsState = state;
    }

    private void Start() {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        musicSlider.value = PauseMenu.musicVol;
        MusicVolume();
    }

    void Update()
    {
        if (pauseAction.action.WasPressedThisFrame())
        {
            TogglePause();
        }
    }

    void TogglePause() {
        if (paused)
        {
            ResumeScene();
        } else
        {
            PauseScene();
        }
    }


    public void ResumeScene()
    {
        try {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        } finally {
            ReleasePause();
        }
    }

    private void ReleasePause() {
        if (!paused) return;

        paused = false;
        TimeManager.Unfreeze(this);
        OnPauseMenuDeactivate?.Invoke();
    }

    public void PauseScene() {
        if (paused) return;

        paused = true;
        try {
            TimeManager.Freeze(this);
            pauseMenuPanel.SetActive(true);
            if (!paused) return;

            if (isDialogueHistoryShowing) {
                RenderHistory();
            }
            OnPauseMenuActivate?.Invoke();
        } catch {
            if (paused) ResumeScene();
            throw;
        }
    }

    private void OnDisable() {
        ResumeScene();
    }

    private void OnDestroy() {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        ReleasePause();
    }

    private void OnActiveSceneChanged(Scene current, Scene next) {
        if (paused) ResumeScene();
    }

    public void LevelSelectScene() {
        ResumeScene();
        BuffManager.Instance.ClearAllBuffs();
        SceneInitializer.LoadScene("LevelSelect");
    }

    public void MainMenuScene() {
        ResumeScene();
        BuffManager.Instance.ClearAllBuffs();
        SceneInitializer.LoadScene("1_Title");
    }

    public void ResetScene() {
        ResumeScene();
        SceneInitializer.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SkipScene() {
        ResumeScene();
        LevelManager.Instance.NextLevel();
    }

    public void ToggleMusic() {
        AudioManager.Instance.ToggleAudio();
    }

    public void MusicVolume() {
        PauseMenu.musicVol = musicSlider.value;
        AudioManager.Instance.SetBGMVolume(musicSlider.value);
        AudioManager.Instance.SetSFXVolume(musicSlider.value);
        AudioManager.Instance.SetAmbientVolume(musicSlider.value);
    }

    public void ToggleScreenShake() {
        PauseMenu.ScreenShake = !PauseMenu.ScreenShake;
    }
}
