using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Handles 3rd-person camera positioning, camera target orbit, and recoil offsets.
    /// Can be integrated with Cinemachine or custom smooth camera follow in future phases.
    /// 
    /// ATTACH TO: Main Camera or Cinemachine Camera Rig GameObject.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0.5f, 1.6f, -3.0f);

        [Header("Sensitivity & Limits")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;

        private float yaw;
        private float pitch;

        private void LateUpdate()
        {
            // Camera orbit and smooth follow logic will be implemented in Phase 2
        }
    }
}
