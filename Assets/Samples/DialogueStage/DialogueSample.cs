using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

namespace Samples.DialogueStage {
    public class DialogueSample : MonoBehaviour {
        [SerializeField] private DialogueRunner runner;

        private void Start() => StartDialogue();

        private void Update() {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) StartDialogue();
        }

        private void StartDialogue() {
            if (!runner.IsDialogueRunning) runner.StartDialogue("Start");
        }
    }
}
