using UnityEngine;
using UnityEngine.Serialization;

namespace Camera {
    public class UseVerticalLookahead : MonoBehaviour {
        [FormerlySerializedAs("offsetYWhileFalling")] public float offset = -3f;
        public float lerpSpeed = 3f;              // Smoothing speed

        private Unity.Cinemachine.CinemachineCamera virtualCam;
        private Unity.Cinemachine.CinemachinePositionComposer positionComposer;
        private float defaultYOffset;

        void Start()
        {
            virtualCam = GetComponent<Unity.Cinemachine.CinemachineCamera>();
            positionComposer = GetComponent<Unity.Cinemachine.CinemachinePositionComposer>();

            if (positionComposer != null)
            {
                defaultYOffset = positionComposer.TargetOffset.y;
            }
        }

        void LateUpdate()
        {
            if (positionComposer == null) return;

            bool lookingDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            float targetYOffset = lookingDown ? offset : defaultYOffset;

            Vector3 targetOffset = positionComposer.TargetOffset;
            targetOffset.y = Mathf.Lerp(targetOffset.y, targetYOffset, Time.deltaTime * lerpSpeed);
            positionComposer.TargetOffset = targetOffset;
        }
    }
}
