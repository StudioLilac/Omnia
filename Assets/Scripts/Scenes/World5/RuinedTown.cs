using System.Collections;
using NPC;
using Players;
using Players.Mixin;
using UnityEngine;
using Utils;

namespace Scenes.World5 {
    [RequireComponent(typeof(BoxCollider2D))]
    public class RuinedTown : MonoBehaviour {
        [SerializeField] private FadeScreenHandler fadescreen;

        [SerializeField] private LayerMask playerLayer;

        [SerializeField] private Player player;
        [SerializeField] private Uncle uncle;

        [SerializeField] private Yarn.Unity.DialogueReference beginDialogue;
        [SerializeField] private Yarn.Unity.DialogueReference uncleDialogue;
        [SerializeField] private Yarn.Unity.DialogueReference hugDialogue;

        private bool uncleTriggered;

        public void Start() {
            fadescreen.SetLightScreen();
            StartCoroutine(BeginSequence());
        }

        private IEnumerator BeginSequence() {
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(beginDialogue));
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (((1 << other.gameObject.layer) & playerLayer) != 0) {
                if (!uncleTriggered) {
                    StartCoroutine(OnPlayerEnter());
                }
            }
        }

        private IEnumerator OnPlayerEnter() {
            Player.controlsLocked = true;
            uncleTriggered = true;
            uncle.Walk(-5f, 3f);
            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(uncleDialogue));

            Player.controlsLocked = false;
            player.GetComponent<UseInput>().enabled = false;

            while (player.transform.position.x <= uncle.transform.position.x) {
                player.moving = new Vector2(1, 0);
                yield return null;
            }

            player.moving = Vector2.zero;
            player.GetComponent<UseInput>().enabled = true;


            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(hugDialogue));
            StartCoroutine(fadescreen.FadeInDarkScreen(2f));
            LevelManager.Instance.NextLevel();
        }
    }
}
