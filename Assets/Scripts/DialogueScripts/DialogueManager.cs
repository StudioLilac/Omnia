using System;
using System.Collections;
using UnityEngine;
using Utils;
using Yarn.Unity;

public class DialogueManager : PersistentSingleton<DialogueManager> {
    [SerializeField] private DialogueRunner yarnDialogueRunner;
    public static event Action LegacyDialogueEvent;
    public bool inDialogue;

    protected override void OnAwake() {
        yarnDialogueRunner.AddCommandHandler("legacy_dialogue_event", () => LegacyDialogueEvent?.Invoke());
    }

    void Start() {
        PauseMenu.OnPauseMenuActivate += OnPause;
        PauseMenu.OnPauseMenuDeactivate += OnUnpause;
        yarnDialogueRunner.gameObject.SetActive(false);
    }

    void OnDestroy() {
        PauseMenu.OnPauseMenuActivate -= OnPause;
        PauseMenu.OnPauseMenuDeactivate -= OnUnpause;
    }

    public bool IsInDialogue() => inDialogue;

    public IEnumerator StartDialogue(DialogueReference dialogueReference) {
        if (inDialogue || yarnDialogueRunner == null || dialogueReference == null || dialogueReference.project == null || string.IsNullOrWhiteSpace(dialogueReference.nodeName)) yield break;
        if (InventoryManager.Instance?.IsInventoryOpen ?? false) InventoryManager.Instance.CloseInventory();

        inDialogue = true;
        Time.timeScale = 0f;
        yarnDialogueRunner.gameObject.SetActive(true);
        yarnDialogueRunner.SetProject(dialogueReference.project);
        yield return YarnTask.ToCoroutine(async () => {
            await yarnDialogueRunner.StartDialogue(dialogueReference.nodeName);
            await yarnDialogueRunner.DialogueTask;
        });
        yarnDialogueRunner.gameObject.SetActive(false);
        inDialogue = false;
        Time.timeScale = 1f;
    }

    private void OnPause() {
        if (inDialogue) yarnDialogueRunner.gameObject.SetActive(false);
    }

    private void OnUnpause() {
        if (inDialogue) {
            Time.timeScale = 0f;
            yarnDialogueRunner.gameObject.SetActive(true);
        }
    }
}
