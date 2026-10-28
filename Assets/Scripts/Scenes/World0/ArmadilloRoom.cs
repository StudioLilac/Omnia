using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Scenes {
    public class ArmadilloRoom : MonoBehaviour {
        [SerializeField] private Yarn.Unity.DialogueReference beginDialogue;
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
