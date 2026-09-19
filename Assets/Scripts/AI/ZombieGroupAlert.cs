using UnityEngine;

namespace ZombieApocalypse.AI
{
    /// <summary>
    /// Debounced, non-recursive group alert system for Phase 8.
    /// Broadcasts alert signals to nearby zombies when a zombie first spots the player or receives damage.
    /// Does NOT re-trigger recursive alert cascades or pull chasing/attacking zombies back into investigation.
    /// </summary>
    public static class ZombieGroupAlert
    {
        private static readonly Collider[] hitBuffer = new Collider[16];

        /// <summary>
        /// Alerts nearby zombies within alertRadius of originPos.
        /// </summary>
        public static void AlertNearbyZombies(Vector3 originPos, float alertRadius, Vector3 targetPosition, ZombieAI senderZombie)
        {
            if (alertRadius <= 0.01f) return;

            int hitCount = Physics.OverlapSphereNonAlloc(originPos, alertRadius, hitBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hitBuffer[i];
                if (col == null) continue;

                ZombieAI zombie = col.GetComponent<ZombieAI>();
                if (zombie == null) zombie = col.GetComponentInParent<ZombieAI>();

                if (zombie != null && zombie != senderZombie)
                {
                    zombie.ReceiveGroupAlert(targetPosition);
                }

                hitBuffer[i] = null; // Clear buffer reference
            }
        }
    }
}
