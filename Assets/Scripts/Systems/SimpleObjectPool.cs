using System.Collections.Generic;
using UnityEngine;

namespace ZombieApocalypse.Systems
{
    /// <summary>
    /// Lightweight non-allocating Object Pool for Phase 19 performance optimization.
    /// Manages reusable pools for impact VFX, blood splatters, muzzle flashes, and temporary world objects,
    /// avoiding per-frame Instantiate/Destroy overhead during heavy combat or horde events.
    /// </summary>
    public class SimpleObjectPool : MonoBehaviour
    {
        public static SimpleObjectPool Instance { get; private set; }

        private Dictionary<GameObject, Queue<GameObject>> poolMap = new Dictionary<GameObject, Queue<GameObject>>();
        private Dictionary<GameObject, GameObject> instancePrefabMap = new Dictionary<GameObject, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            if (!poolMap.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                poolMap[prefab] = queue;
            }

            GameObject instance = null;
            while (queue.Count > 0)
            {
                GameObject candidate = queue.Dequeue();
                if (candidate != null)
                {
                    instance = candidate;
                    break;
                }
            }

            if (instance == null)
            {
                instance = Instantiate(prefab, position, rotation, parent != null ? parent : transform);
                instancePrefabMap[instance] = prefab;
            }
            else
            {
                instance.transform.SetParent(parent != null ? parent : transform, false);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.SetActive(true);
            }

            return instance;
        }

        public void Despawn(GameObject instance, float delay = 0f)
        {
            if (instance == null) return;

            if (delay > 0f)
            {
                StartCoroutine(DespawnDelayed(instance, delay));
            }
            else
            {
                RecycleInstance(instance);
            }
        }

        private System.Collections.IEnumerator DespawnDelayed(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            RecycleInstance(instance);
        }

        private void RecycleInstance(GameObject instance)
        {
            if (instance == null) return;

            if (instancePrefabMap.TryGetValue(instance, out GameObject prefab) && prefab != null)
            {
                instance.SetActive(false);
                if (poolMap.TryGetValue(prefab, out Queue<GameObject> queue))
                {
                    queue.Enqueue(instance);
                    return;
                }
            }

            // Fallback if not tracked in pool map
            Destroy(instance);
        }
    }
}
