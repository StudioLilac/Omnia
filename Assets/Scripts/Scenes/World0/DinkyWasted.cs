using System.Collections;
using System.Collections.Generic;
using NPC.Dinky;
using Players;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Scenes {
    public class DinkyWasted : MonoBehaviour {
        [SerializeField] private Yarn.Unity.DialogueReference beginDialogue;
        [SerializeField] private Yarn.Unity.DialogueReference shotgunDialogue;
        [SerializeField] private Player player;

        private bool changedWeapon = false;

        private bool beginDialoguePlayed = false;

        public void Start() {
            StartCoroutine(BeginSequence());
            Dinky.OnInteract += EndScene;
        }

        // this is garbage code. it should be taken out and shot. but i really don't feel like adding more events
        // who cares about performance anyways right
        public void Update() {
            if (player.selectedWeapon == 1 && !changedWeapon && beginDialoguePlayed) {
                changedWeapon = true;
                StartCoroutine(ShotgunTutorialSequence());
            }
        }

        private IEnumerator BeginSequence() {
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(beginDialogue));
            beginDialoguePlayed = true;
        }

        private IEnumerator ShotgunTutorialSequence() {
            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(shotgunDialogue));
        }

        private void EndScene() {
            LevelManager.Instance.NextLevel();
        }
    }
}
