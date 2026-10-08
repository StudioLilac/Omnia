using System.Collections.Generic;

using Players;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Unity.Cinemachine.CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Player player;
    [SerializeField] private float aimOffsetDistance = 3f;
    [SerializeField] private float smoothSpeed = 5f;

    private Unity.Cinemachine.CinemachineFramingTransposer framingTransposer;
    private Vector3 defaultOffset;
    private Vector3 currentOffset;

    void Start()
    {
        if (virtualCamera != null)
            framingTransposer = virtualCamera.GetCinemachineComponent<Unity.Cinemachine.CinemachineFramingTransposer>();

        if (framingTransposer != null)
            defaultOffset = framingTransposer.m_TrackedObjectOffset;
    }

    void LateUpdate()
    {
        if (player == null || framingTransposer == null) return;

        if (Input.GetMouseButton(1))
        {
            Vector3 desiredOffset = (Vector3)player.facing.normalized * aimOffsetDistance;
            currentOffset = Vector3.Lerp(currentOffset, desiredOffset, smoothSpeed * Time.deltaTime);
            framingTransposer.m_TrackedObjectOffset = new Vector3(currentOffset.x, currentOffset.y, framingTransposer.m_TrackedObjectOffset.z);
        }
        else
        {
            framingTransposer.m_TrackedObjectOffset = Vector3.Lerp(
                framingTransposer.m_TrackedObjectOffset,
                defaultOffset,
                smoothSpeed * Time.deltaTime
            );
        }
    }

}
