using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Players.Fragments {
    public class TransferMomentum : Fragment {
        [SerializeField] private float increase;
        [SerializeField] private float duration;
        [SerializeField] private float decayTime;

		private OrbitObject orbitObject;
        private Coroutine spinCoroutine;
        private float originalSpeed;

        private void SpinFaster() {
            Debug.Log("spinning faster");
            orbitObject = player.GetComponentInChildren<OrbitObject>();
            if (orbitObject == null) return;

            if (spinCoroutine != null) {
                StopCoroutine(spinCoroutine);
            }

            spinCoroutine = StartCoroutine(SpinFasterCoroutine());
        }

        private IEnumerator SpinFasterCoroutine() {
            orbitObject.orbitSpeed = originalSpeed + increase;

            yield return new WaitForSeconds(duration);

            float elapsed = 0f;
            while (elapsed < decayTime) {
                orbitObject.orbitSpeed = Mathf.Lerp(originalSpeed + increase, originalSpeed, elapsed / decayTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            orbitObject.orbitSpeed = originalSpeed;
        }

        public override void ApplyBuff() {
            base.ApplyBuff();
            orbitObject = player.GetComponentInChildren<OrbitObject>();
            Player.OnSkill += SpinFaster;
            originalSpeed = orbitObject.orbitSpeed;

        }

        public override void RevokeBuff() {
            orbitObject = player.GetComponentInChildren<OrbitObject>();
            Player.OnSkill -= SpinFaster;
        }
    }
}
