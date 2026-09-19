using UnityEngine;

namespace ZombieApocalypse.AI
{
    public enum NoiseType
    {
        Gunshot,
        Footstep,
        Environment
    }

    /// <summary>
    /// Encapsulates noise payload data emitted by weapon firing, player locomotion, or world events.
    /// </summary>
    public struct NoiseEvent
    {
        public Vector3 position;
        public float radius;
        public NoiseType type;

        public NoiseEvent(Vector3 position, float radius, NoiseType type)
        {
            this.position = position;
            this.radius = radius;
            this.type = type;
        }
    }
}
