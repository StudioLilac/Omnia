using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utils;

public class DialogueManager : PersistentSingleton<DialogueManager> {
    private DialogueBox activeDialogueBox;
    [SerializeField] private DialogueBox picturelessDialogueBoxComponent;
    [SerializeField] private DialogueBox pictureDialogueBox;
    [SerializeField] private InputActionReference advanceAction;
    [SerializeField] private InputActionReference fastForwardAction;
    private List<DialogueText> sentences = new();

    public bool inDialogue;

    List<DialogueText> dialogueHistory = new();

    protected override void OnAwake() {
        activeDialogueBox = pictureDialogueBox;
    }

    //Manager is going to listen to a bunch of events that then cause it to gain new sentences and
    void Start() {
        SceneManager.activeSceneChanged += ClearDialogueHistory;
        PauseMenu.OnPauseMenuActivate += OnPause;
        PauseMenu.OnPauseMenuDeactivate += OnUnpause;
        ClearPanel();
    }

    void OnDestroy() {
        SceneManager.activeSceneChanged -= ClearDialogueHistory;
        PauseMenu.OnPauseMenuActivate -= OnPause;
        PauseMenu.OnPauseMenuDeactivate -= OnUnpause;
    }

    public bool IsInDialogue() {
        return inDialogue;
    }

    public IEnumerator StartDialogue(List<DialogueText> newSentences) {
        if (activeDialogueBox.gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (InventoryManager.Instance?.IsInventoryOpen ?? false) {
            InventoryManager.Instance.CloseInventory();
        }

        activeDialogueBox.gameObject.SetActive(true);

        sentences = newSentences;

        inDialogue = true;

        Time.timeScale = 0f;

        DisplayNextSentence();
        yield return new WaitUntil(() => !inDialogue);
    }


    void AddLineToHistory(DialogueText text) {
        dialogueHistory.Add(text);
    }

    public List<DialogueText> GetHistory() {
        return dialogueHistory;
    }


    void ClearPanel() {
        activeDialogueBox.gameObject.SetActive(false);
        sentences.Clear();
        inDialogue = false;
    }

    public void MoveBoxToBottom() {
        pictureDialogueBox.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
        pictureDialogueBox.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
        pictureDialogueBox.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        if (picturelessDialogueBoxComponent != null) {
            picturelessDialogueBoxComponent.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
            picturelessDialogueBoxComponent.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            picturelessDialogueBoxComponent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        }
    }

    public void MoveBoxToTop() {
        pictureDialogueBox.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
        pictureDialogueBox.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1);
        pictureDialogueBox.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1 * pictureDialogueBox.GetComponent<RectTransform>().rect.height);

        if (picturelessDialogueBoxComponent != null) {
            picturelessDialogueBoxComponent.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
            picturelessDialogueBoxComponent.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1);
            picturelessDialogueBoxComponent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1 * picturelessDialogueBoxComponent.GetComponent<RectTransform>().rect.height);
        }
    }

    void DisplayNextSentence() {
        if (sentences.Count == 0) {
            EndDialogue();
            return;
        }

        DialogueText sentence = sentences[0];
        sentences.RemoveAt(0); //Uses a list instead of a Queue so we can see it in the Unity Editor

        AddLineToHistory(sentence);

        if (sentence.DisplayingImage == null && picturelessDialogueBoxComponent != null) {
            SetDialogueBoxActive(picturelessDialogueBoxComponent);
            picturelessDialogueBoxComponent.SetLine(sentence);
        } else {
            SetDialogueBoxActive(pictureDialogueBox);
            pictureDialogueBox.SetLine(sentence);
        }
    }

    void SetDialogueBoxActive(DialogueBox dialogueBox) {
        if (picturelessDialogueBoxComponent != null) {
            picturelessDialogueBoxComponent.gameObject.SetActive(dialogueBox == picturelessDialogueBoxComponent);
            pictureDialogueBox.gameObject.SetActive(dialogueBox == pictureDialogueBox);
            activeDialogueBox = dialogueBox;
        } else {
            pictureDialogueBox.gameObject.SetActive(true);
            activeDialogueBox = pictureDialogueBox;
        }
    }

    void EndDialogue() {
        ClearPanel();
        Time.timeScale = 1f;
    }

    void Update() {
        if (!inDialogue) return;

        bool advance = advanceAction.action.WasPressedThisFrame()
                       || fastForwardAction.action.IsPressed();

        if (advance && !PauseMenu.IsPaused) {
            if (activeDialogueBox.FinishedLine()) {
                DisplayNextSentence();
            } else {
                activeDialogueBox.StopScrollingText();
            }
        }
    }

    private void ClearDialogueHistory(Scene current, Scene next) {
        dialogueHistory.Clear();
    }

    private void OnPause() {
        if (!inDialogue) return;
        activeDialogueBox.gameObject.SetActive(false);
    }

    private void OnUnpause() {
        if (!inDialogue) return;
        Time.timeScale = 0f;
        activeDialogueBox.gameObject.SetActive(true);
        activeDialogueBox.StopScrollingText();
    }
}
