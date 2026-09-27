using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class UseVerticalLookahead : MonoBehaviour {
        [FormerlySerializedAs("offsetYWhileFalling")] public float offset = -3f;
        public float lerpSpeed = 3f;              // Smoothing speed

        private CinemachineVirtualCamera virtualCam;
        private CinemachineFramingTransposer framingTransposer;
        private float defaultYOffset;

        void Start()
        {
            virtualCam = GetComponent<CinemachineVirtualCamera>();
            framingTransposer = virtualCam.GetCinemachineComponent<CinemachineFramingTransposer>();

            if (framingTransposer != null)
            {
                defaultYOffset = framingTransposer.m_TrackedObjectOffset.y;
            }
        }

        void LateUpdate()
        {
            if (framingTransposer == null) return;

            bool lookingDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            float targetYOffset = lookingDown ? this.offset : defaultYOffset;

            Vector3 offset = framingTransposer.m_TrackedObjectOffset;
            offset.y = Mathf.Lerp(offset.y, targetYOffset, Time.deltaTime * lerpSpeed);
            framingTransposer.m_TrackedObjectOffset = offset;
        }

    }
