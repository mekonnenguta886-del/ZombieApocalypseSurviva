using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Professional 3rd-person camera follower and mouse orbit controller.
    /// Supports pitch/yaw mouse rotation, distance damping, target height offset,
    /// pitch clamping, and collision obstruction check.
    /// 
    /// ATTACH TO: Main Camera GameObject.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Distance & Limits")]
        [SerializeField] private float defaultDistance = 3.5f;
        [SerializeField] private float minDistance = 1.0f;
        [SerializeField] private float maxDistance = 6.0f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;

        [Header("Sensitivity & Smoothing")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float rotationSmoothTime = 0.05f;
        [SerializeField] private float positionSmoothTime = 0.05f;

        [Header("Camera Collision")]
        [SerializeField] private bool enableCollisionCheck = true;
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private LayerMask collisionLayers = ~0; // Default all

        // Rotation & Movement Tracking
        private float yaw;
        private float pitch;
        private float currentDistance;
        private Vector3 currentRotationVelocity;
        private Vector3 currentPositionVelocity;
        private Vector3 targetRotation;
        private Vector3 currentRotation;

        public Transform TargetTransform => targetTransform;

        private void Start()
        {
            currentDistance = defaultDistance;

            if (targetTransform == null)
            {
                // Auto-find Player GameObject if target non-assigned
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Transform cameraTarget = player.transform.Find("CameraTarget");
                    targetTransform = cameraTarget != null ? cameraTarget : player.transform;
                }
            }

            Vector3 euler = transform.eulerAngles;
            pitch = euler.x;
            yaw = euler.y;
        }

        private void LateUpdate()
        {
            if (targetTransform == null) return;

            HandleInput();
            UpdateCameraPositionAndRotation();
        }

        private void HandleInput()
        {
            if (PlayerInputHandler.Instance == null) return;

            Vector2 lookInput = PlayerInputHandler.Instance.LookInput;
            yaw += lookInput.x * mouseSensitivity;
            pitch -= lookInput.y * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void UpdateCameraPositionAndRotation()
        {
            // Smooth Rotation
            targetRotation = new Vector3(pitch, yaw, 0f);
            currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref currentRotationVelocity, rotationSmoothTime);
            transform.eulerAngles = currentRotation;

            // Target Position Calculation
            Vector3 targetCenter = targetTransform.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(currentRotation);
            Vector3 desiredCameraPos = targetCenter - (rotation * Vector3.forward * defaultDistance);

            // Camera Obstruction / Collision check
            float finalDistance = defaultDistance;
            if (enableCollisionCheck)
            {
                Vector3 rayDirection = desiredCameraPos - targetCenter;
                if (Physics.SphereCast(targetCenter, collisionRadius, rayDirection.normalized, out RaycastHit hit, defaultDistance, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    finalDistance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, maxDistance);
                }
            }

            currentDistance = Mathf.Lerp(currentDistance, finalDistance, Time.deltaTime * 15f);
            Vector3 finalCameraPos = targetCenter - (rotation * Vector3.forward * currentDistance);

            transform.position = Vector3.SmoothDamp(transform.position, finalCameraPos, ref currentPositionVelocity, positionSmoothTime);
        }

        /// <summary>
        /// Explicitly binds target transform (e.g. Player's CameraTarget).
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            targetTransform = newTarget;
        }
    }
}
