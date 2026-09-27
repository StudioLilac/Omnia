using System.Collections;
using System.Collections.Generic;
using Players;
using Players.Mixin;
using UnityEngine;

namespace Scenes.World5 {
    public class Lab_Prefight : MonoBehaviour {
        [SerializeField] private Player jamie;
        [SerializeField] private GameObject dinky;
        [SerializeField] private GameObject tankDinky;
        [SerializeField] private GameObject boss;

        [SerializeField] Tank sundewTank;
        [SerializeField] Tank crabTank;
        [SerializeField] Tank birdTank;
        [SerializeField] Tank armadilloTankk;

        [SerializeField] DialogueWrapper beginDialogue;
        [SerializeField] DialogueWrapper dinkyThrownDialogue;
        [SerializeField] DialogueWrapper dinkyHurtDialogue;
        public void Start() {
            Player.controlsLocked = true;
            StartCoroutine(BeginSequence());
        }

        private IEnumerator BeginSequence() {
            List<Tank> tanks = new() {sundewTank,crabTank,armadilloTankk,birdTank };
            tanks.ForEach(t => t.TurnOff());

            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(beginDialogue.Dialogue));

            yield return new WaitForSeconds(1f);
            Rigidbody2D rb = dinky.GetComponent<Rigidbody2D>();
            if (rb == null) {
                rb = dinky.AddComponent<Rigidbody2D>();
            }

            rb.gravityScale = 1f;
            float horizontalForce = 0.3f;
            rb.linearVelocity = new Vector2(horizontalForce, 16);

            float spinForce = Random.Range(250f, 400f) * (Random.value > 0.5f ? 1 : -1);
            rb.angularVelocity = spinForce;

            yield return new WaitUntil(() => rb.linearVelocity.y < -1f);

            Destroy(dinky);

            tankDinky.AddComponent<Rigidbody2D>();


            tanks.ForEach(t => t.FadeOn());

            yield return new WaitForSeconds(2f);


            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(dinkyThrownDialogue.Dialogue));

            StartCoroutine(AudioManager.Instance.StopBGM());
            // machine starts

            boss.AddComponent<BobbingBehaviour>();

            yield return new WaitForSeconds(3f);

            // something goes wrong

            AudioManager.Instance.PlaySFX(AudioTracks.DinkyScream);

            yield return new WaitForSeconds(0.75f);

            tanks.ForEach(t => t.Deactivate());

            yield return new WaitForSeconds(0.75f);

            StartCoroutine(BlareAlarm());

            yield return new WaitForSeconds(2f);

            yield return StartCoroutine(DialogueManager.Instance.StartDialogue(dinkyHurtDialogue.Dialogue));

            Player.controlsLocked = false;
            LevelManager.Instance.NextLevel();
        }

        private IEnumerator BlareAlarm() {
            while (true) {
                AudioManager.Instance.PlaySFX(AudioTracks.MachineMalfunction);
                yield return new WaitForSeconds(7f);
            }
        }
    }

}
