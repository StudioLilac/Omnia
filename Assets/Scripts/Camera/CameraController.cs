using System.Collections.Generic;

using Players;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Unity.Cinemachine.CinemachineCamera virtualCamera;
    [SerializeField] private Player player;
    [SerializeField] private float aimOffsetDistance = 3f;
    [SerializeField] private float smoothSpeed = 5f;

    private Unity.Cinemachine.CinemachinePositionComposer positionComposer;
    private Vector3 defaultOffset;
    private Vector3 currentOffset;

    void Start()
    {
        if (virtualCamera != null)
            positionComposer = virtualCamera.GetComponent<Unity.Cinemachine.CinemachinePositionComposer>();

        if (positionComposer != null)
            defaultOffset = positionComposer.TargetOffset;
    }

    void LateUpdate()
    {
        if (player == null || positionComposer == null) return;

        if (Input.GetMouseButton(1))
        {
            Vector3 desiredOffset = (Vector3)player.facing.normalized * aimOffsetDistance;
            currentOffset = Vector3.Lerp(currentOffset, desiredOffset, smoothSpeed * Time.deltaTime);
            positionComposer.TargetOffset = new Vector3(currentOffset.x, currentOffset.y, positionComposer.TargetOffset.z);
        }
        else
        {
            positionComposer.TargetOffset = Vector3.Lerp(
                positionComposer.TargetOffset,
                defaultOffset,
                smoothSpeed * Time.deltaTime
            );
        }
    }

}
