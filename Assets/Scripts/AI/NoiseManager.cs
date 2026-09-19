using System;
using UnityEngine;

namespace ZombieApocalypse.AI
{
    /// <summary>
    /// Central static event manager for broadcasting and listening to noise events in the world.
    /// Used by weapon firing, player sprinting, and world events for zombie perception.
    /// </summary>
    public static class NoiseManager
    {
        public static event Action<NoiseEvent> OnNoiseEmitted;

        /// <summary>
        /// Emits a noise event at a specific position with radius and type.
        /// </summary>
        public static void EmitNoise(Vector3 position, float radius, NoiseType type)
        {
            if (radius <= 0.01f) return;

            NoiseEvent noiseEvent = new NoiseEvent(position, radius, type);
            OnNoiseEmitted?.Invoke(noiseEvent);

            Debug.Log($"[NoiseManager] Noise Emitted ({type}): Radius={radius:F1}m at {position}");
        }
    }
}
