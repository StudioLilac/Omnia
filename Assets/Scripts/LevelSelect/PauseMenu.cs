using System.Collections;
using System.Collections.Generic;
using Initializers;
using Players.Buff;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;

public class PauseMenu : PersistentSingleton<PauseMenu> {
    public static bool IsPaused = false;
    public static bool ScreenShake = true;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Canvas pauseCanvas;

    [SerializeField] private GameObject history;
    [SerializeField] private GameObject controls;
    [SerializeField] private GameObject buttonparent;


    [SerializeField] private TextMeshProUGUI historyText;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicToggle;
    public static float musicVol = 0.7f;


    public delegate void PauseMenuEventHandler();

    public static event PauseMenuEventHandler OnPauseMenuActivate;   // for more complex functions that cannot use isPaused
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

    protected override void OnAwake() {
        return;
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
        List<DialogueText> list = DialogueManager.Instance?.GetHistory();
        historyText.text = "";
        foreach (DialogueText dialogue in list) {
            historyText.text += "<b>" + dialogue.SpeakerName + "</b>" + "\n" + dialogue.BodyText + "\n\n";
        }
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

    private void Start() {
        musicSlider.value = PauseMenu.musicVol;
        MusicVolume();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePause();
        }
    }

    void TogglePause() {
        if (IsPaused)
        {
            ResumeScene();
        } else
        {
            PauseScene();
        }
    }


    public void ResumeScene()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
        OnPauseMenuDeactivate?.Invoke();
    }

    public void PauseScene() {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;

        if (isDialogueHistoryShowing) {
            RenderHistory();
        }
        OnPauseMenuActivate?.Invoke();
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
