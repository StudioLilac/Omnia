using Players;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Scenes {
    public class ArmadilloRoom2 : MonoBehaviour {
        [SerializeField] private Yarn.Unity.DialogueReference beginDialogue;
        [SerializeField] private Transform fireFliesLocation;
        [SerializeField] private Collider2D fireFlyTrigger;

        private bool fireFlyTriggered = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.GetComponent<Player>() == null) return;
            if (fireFlyTriggered) return;
            fireFlyTriggered = true;
            GameplayAssistance.Instance.interval = 4;
            GameplayAssistance.SetPathHintTarget(fireFliesLocation);
        }

        public void Start() {
            StartCoroutine(BeginSequence());
            AudioManager.Instance.SwitchBGM(AudioTracks.CaveSpeak);
        }

        private IEnumerator BeginSequence() {
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(beginDialogue));
        }

    }
}
