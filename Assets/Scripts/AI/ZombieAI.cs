using UnityEngine;
using UnityEngine.AI;
using ZombieApocalypse.Zombies;

namespace ZombieApocalypse.AI
{
    public enum AIState
    {
        Idle,
        Patrol,
        Search,
        Chase,
        Attack
    }

    /// <summary>
    /// Zombie AI state machine using Unity's NavMeshAgent.
    /// Handles player detection, pathfinding, chase, and attack behaviors.
    /// 
    /// ATTACH TO: Zombie prefab GameObjects containing NavMeshAgent.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ZombieData zombieData;
        [SerializeField] private AIState currentState = AIState.Idle;

        private NavMeshAgent navMeshAgent;

        public AIState CurrentState => currentState;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            // NavMesh AI state machine logic will be implemented in future phase
        }

        public void SetState(AIState newState)
        {
            currentState = newState;
        }
    }
}
