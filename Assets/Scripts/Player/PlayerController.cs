using UnityEngine;

namespace ZombieApocalypse.Player
{
    /// <summary>
    /// Controls player locomotion, crouch, sprint, and jump states using Unity's CharacterController.
    /// Integrated with Unity Input System for 3rd-person controls in later phases.
    /// 
    /// ATTACH TO: Player prefab GameObject (e.g. "Player").
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 7.0f;
        [SerializeField] private float crouchSpeed = 2.0f;

        [Header("Physics")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float jumpHeight = 1.2f;

        private CharacterController characterController;
        private Vector3 velocity;
        private bool isGrounded;

        public float CurrentSpeed { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Locomotion logic will be wired to Input System in Phase 2
        }
    }
}
