using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Extended 3rd-person camera controller supporting over-the-shoulder Aim mode,
    /// smooth FOV zoom, shoulder offset, camera recoil kick, and obstruction raycasting.
    /// 
    /// ATTACH TO: Main Camera GameObject.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 normalTargetOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private Vector3 aimTargetOffset = new Vector3(0.5f, 1.5f, 0f);

        [Header("Distance & FOV")]
        [SerializeField] private float normalDistance = 3.5f;
        [SerializeField] private float aimDistance = 1.8f;
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float aimFOV = 45f;
        [SerializeField] private float aimTransitionSpeed = 10f;

        [Header("Rotation Limits")]
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;

        [Header("Sensitivity & Smoothing")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float rotationSmoothTime = 0.05f;
        [SerializeField] private float positionSmoothTime = 0.05f;

        [Header("Collision & Obstruction")]
        [SerializeField] private bool enableCollisionCheck = true;
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionLayers = ~0;

        // Internal State
        private Camera mainCamera;
        private float yaw;
        private float pitch;
        private float recoilPitchOffset;
        private float currentDistance;
        private bool isAiming;
        private Vector3 currentRotationVelocity;
        private Vector3 currentPositionVelocity;
        private Vector3 targetRotation;
        private Vector3 currentRotation;

        public Transform TargetTransform => targetTransform;
        public bool IsAiming => isAiming;
        public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = Mathf.Clamp(value, 0.1f, 10f); }
        public bool InvertY { get; set; } = false;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            currentDistance = normalDistance;

            if (targetTransform == null)
            {
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
            UpdateAimMode();
            UpdateCameraPositionAndRotation();
        }

        private void HandleInput()
        {
            if (PlayerInputHandler.Instance == null) return;

            Vector2 lookInput = PlayerInputHandler.Instance.LookInput;
            yaw += lookInput.x * mouseSensitivity;
            pitch += (InvertY ? lookInput.y : -lookInput.y) * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Smoothly recover recoil impulse offset back to zero
            if (recoilPitchOffset > 0.001f)
            {
                recoilPitchOffset = Mathf.MoveTowards(recoilPitchOffset, 0f, 15f * Time.deltaTime);
            }

            // Read Aim state from InputHandler
            isAiming = PlayerInputHandler.Instance.AimHeld;
        }

        private void UpdateAimMode()
        {
            if (mainCamera != null)
            {
                float targetFOV = isAiming ? aimFOV : normalFOV;
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * aimTransitionSpeed);
            }
        }

        private void UpdateCameraPositionAndRotation()
        {
            // Smooth Rotation (includes recoil impulse offset without altering base mouse pitch)
            targetRotation = new Vector3(pitch - recoilPitchOffset, yaw, 0f);
            currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref currentRotationVelocity, rotationSmoothTime);
            transform.eulerAngles = currentRotation;

            // Determine Target Position with Shoulder Offset
            Vector3 offset = isAiming ? aimTargetOffset : normalTargetOffset;
            Vector3 targetCenter = targetTransform.position + targetTransform.TransformDirection(offset);

            float desiredDist = isAiming ? aimDistance : normalDistance;
            Quaternion rotation = Quaternion.Euler(currentRotation);
            Vector3 desiredCameraPos = targetCenter - (rotation * Vector3.forward * desiredDist);

            // Collision check
            float finalDistance = desiredDist;
            if (enableCollisionCheck)
            {
                Vector3 rayDirection = desiredCameraPos - targetCenter;
                if (Physics.SphereCast(targetCenter, collisionRadius, rayDirection.normalized, out RaycastHit hit, desiredDist, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    finalDistance = Mathf.Clamp(hit.distance - collisionRadius, 0.8f, desiredDist);
                }
            }

            currentDistance = Mathf.Lerp(currentDistance, finalDistance, Time.deltaTime * aimTransitionSpeed);
            Vector3 finalCameraPos = targetCenter - (rotation * Vector3.forward * currentDistance);

            transform.position = Vector3.SmoothDamp(transform.position, finalCameraPos, ref currentPositionVelocity, positionSmoothTime);
        }

        /// <summary>
        /// Applies temporary vertical camera recoil kick on firing.
        /// </summary>
        public void ApplyRecoil(float recoilAmount)
        {
            recoilPitchOffset += recoilAmount;
            recoilPitchOffset = Mathf.Clamp(recoilPitchOffset, 0f, 10f);
        }

        public void SetTarget(Transform newTarget)
        {
            targetTransform = newTarget;
        }
    }
}
