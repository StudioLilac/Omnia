using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using Utils;
using Yarn.Unity;

public class DialogueManager : PersistentSingleton<DialogueManager> {
    [SerializeField] private DialogueRunner yarnDialogueRunnerPrefab;

    private DialogueRunner yarnDialogueRunner;
    private Canvas[] dialogueCanvases;
    private CanvasGroup[] dialogueCanvasGroups;
    public static event Action LegacyDialogueEvent;
    public bool inDialogue;
    private TimeManager TimeManager => this.Inject<TimeManager>();

    protected override void OnAwake() {
        if (yarnDialogueRunnerPrefab == null) {
            Debug.LogError("Dialogue Manager requires a Dialogue System Variant prefab.", this);
            return;
        }

        yarnDialogueRunner = Instantiate(yarnDialogueRunnerPrefab);
        DontDestroyOnLoad(yarnDialogueRunner.gameObject);
        dialogueCanvases = yarnDialogueRunner.GetComponentsInChildren<Canvas>(true);
        dialogueCanvasGroups = yarnDialogueRunner.GetComponentsInChildren<CanvasGroup>(true);
        SetDialogueUiVisible(false);
        yarnDialogueRunner.AddCommandHandler("legacy_dialogue_event", () => LegacyDialogueEvent?.Invoke());
    }

    void Start() {
        PauseMenu.OnPauseMenuActivate += OnPause;
        PauseMenu.OnPauseMenuDeactivate += OnUnpause;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() {
        PauseMenu.OnPauseMenuActivate -= OnPause;
        PauseMenu.OnPauseMenuDeactivate -= OnUnpause;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public bool IsInDialogue() => inDialogue;

    public IEnumerator StartDialogue(DialogueReference dialogueReference) {
        if (inDialogue || yarnDialogueRunner == null || dialogueReference == null || dialogueReference.project == null || string.IsNullOrWhiteSpace(dialogueReference.nodeName)) yield break;
        if (InventoryManager.Instance?.IsInventoryOpen ?? false) InventoryManager.Instance.CloseInventory();

        inDialogue = true;
        TimeManager.Freeze(this);
        yarnDialogueRunner.SetProject(dialogueReference.project);
        SetDialogueUiVisible(true);
        yield return YarnTask.ToCoroutine(async () => {
            await yarnDialogueRunner.StartDialogue(dialogueReference.nodeName);
            await yarnDialogueRunner.DialogueTask;
        });
        SetDialogueUiVisible(false);
        inDialogue = false;
        TimeManager.Unfreeze(this);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (!inDialogue) return;
        TimeManager.Unfreeze(this);
        yarnDialogueRunner.Stop();
    }

    private void OnPause() {
        if (inDialogue) SetDialogueUiVisible(false);
    }

    private void OnUnpause() {
        if (inDialogue) {
            SetDialogueUiVisible(true);
        }
    }

    private void SetDialogueUiVisible(bool isVisible) {
        if (dialogueCanvases == null || dialogueCanvasGroups == null) return;

        foreach (var dialogueCanvas in dialogueCanvases) {
            // Keep the canvas itself enabled so Unity registers it as a render
            // surface before the first dialogue begins.
            dialogueCanvas.enabled = true;
        }

        foreach (var dialogueCanvasGroup in dialogueCanvasGroups) {
            dialogueCanvasGroup.alpha = isVisible ? 1f : 0f;
        }
    }
}
