using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieApocalypse.Audio
{
    /// <summary>
    /// Authoritative AudioManager for Phase 19.
    /// Manages music, 3D spatial SFX, pooled AudioSource channels (preventing GC allocations),
    /// procedural fallback clip synthesis (ensuring sound feedback works even if inspector clips are unassigned),
    /// category-specific volume mixing, and anti-audio spam cooldown protection.
    /// 
    /// ATTACH TO: Persistent Audio GameObject (e.g. "[AudioManager]").
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Channel Pooling")]
        [SerializeField] private int maxPooledChannels = 16;

        // Channel Pool State
        private List<AudioSource> channelPool = new List<AudioSource>();
        private int nextChannelIndex = 0;

        // Anti-Spam Sound Cooldown Map
        private Dictionary<string, float> soundCooldowns = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        // Procedural Fallback Clips Cache
        private Dictionary<string, AudioClip> proceduralClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform, false);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = 0.5f;
            }

            if (ambienceSource == null)
            {
                GameObject ambObj = new GameObject("AmbienceSource");
                ambObj.transform.SetParent(transform, false);
                ambienceSource = ambObj.AddComponent<AudioSource>();
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = false;
                ambienceSource.volume = 0.4f;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform, false);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.volume = 0.8f;
            }

            // Create Pooled 3D SFX Channels
            GameObject poolRoot = new GameObject("AudioChannelPool");
            poolRoot.transform.SetParent(transform, false);

            for (int i = 0; i < maxPooledChannels; i++)
            {
                GameObject ch = new GameObject($"SFXChannel_{i}");
                ch.transform.SetParent(poolRoot.transform, false);
                AudioSource src = ch.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1.0f; // 3D Spatial Sound
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 2.0f;
                src.maxDistance = 45.0f;
                channelPool.Add(src);
            }
        }

        private AudioSource GetNextChannel()
        {
            if (channelPool.Count == 0) return sfxSource;

            for (int i = 0; i < channelPool.Count; i++)
            {
                int idx = (nextChannelIndex + i) % channelPool.Count;
                if (!channelPool[idx].isPlaying)
                {
                    nextChannelIndex = (idx + 1) % channelPool.Count;
                    return channelPool[idx];
                }
            }

            // If all channels active, steal oldest
            AudioSource stolen = channelPool[nextChannelIndex];
            nextChannelIndex = (nextChannelIndex + 1) % channelPool.Count;
            return stolen;
        }

        public bool CheckCooldown(string soundKey, float cooldownSeconds = 0.08f)
        {
            if (string.IsNullOrEmpty(soundKey)) return false;
            if (soundCooldowns.TryGetValue(soundKey, out float expiry))
            {
                if (Time.time < expiry) return true; // On cooldown
            }
            soundCooldowns[soundKey] = Time.time + cooldownSeconds;
            return false;
        }

        /// <summary>
        /// Plays spatial 3D SFX clip at specified position using non-allocating channel pool.
        /// </summary>
        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource src = GetNextChannel();
            if (src != null)
            {
                src.transform.position = position;
                src.clip = clip;
                src.volume = Mathf.Clamp01(volume);
                src.spatialBlend = 1.0f;
                src.Play();
            }
        }

        /// <summary>
        /// Plays 2D UI sound effect.
        /// </summary>
        public void Play2DSFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        public void PlayMusic(AudioClip musicClip, bool loop = true)
        {
            if (musicSource == null) return;
            if (musicClip == null)
            {
                musicClip = GetProceduralClip("music_ambient");
            }

            if (musicSource.clip == musicClip && musicSource.isPlaying) return;

            musicSource.clip = musicClip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void SetAmbience(AudioClip ambienceClip, float volume = 0.4f)
        {
            if (ambienceSource == null) return;
            if (ambienceClip == null)
            {
                ambienceClip = GetProceduralClip("ambience_day");
            }

            if (ambienceSource.clip == ambienceClip && ambienceSource.isPlaying) return;

            ambienceSource.clip = ambienceClip;
            ambienceSource.volume = volume;
            ambienceSource.Play();
        }

        // ==========================================
        // STAGE 2 — PROCEDURAL AUDIO FALLBACKS & FEEDBACK
        // ==========================================

        public void PlayWeaponFire(string weaponName, Vector3 position)
        {
            string key = $"fire_{weaponName?.ToLower()}";
            if (CheckCooldown(key, 0.05f)) return;

            AudioClip clip = GetProceduralClip(key);
            PlaySFX(clip, position, 0.9f);
        }

        public void PlayReload(string weaponName, Vector3 position)
        {
            string key = "weapon_reload";
            if (CheckCooldown(key, 0.2f)) return;

            AudioClip clip = GetProceduralClip(key);
            PlaySFX(clip, position, 0.7f);
        }

        public void PlayEmptyClick(Vector3 position)
        {
            if (CheckCooldown("empty_click", 0.15f)) return;
            AudioClip clip = GetProceduralClip("empty_click");
            PlaySFX(clip, position, 0.6f);
        }

        public void PlayFootstep(Vector3 position, bool isSprinting)
        {
            float cd = isSprinting ? 0.25f : 0.45f;
            if (CheckCooldown("player_footstep", cd)) return;

            AudioClip clip = GetProceduralClip("player_footstep");
            PlaySFX(clip, position, isSprinting ? 0.5f : 0.3f);
        }

        public void PlayZombieSound(string soundType, Vector3 position)
        {
            string key = $"zombie_{soundType.ToLower()}";
            if (CheckCooldown(key, 0.3f)) return;

            AudioClip clip = GetProceduralClip(key);
            PlaySFX(clip, position, 0.8f);
        }

        public void PlayUISound(string uiEventType)
        {
            string key = $"ui_{uiEventType.ToLower()}";
            if (CheckCooldown(key, 0.08f)) return;

            AudioClip clip = GetProceduralClip(key);
            Play2DSFX(clip, 0.7f);
        }

        /// <summary>
        /// Synthesizes procedural PCM AudioClips dynamically so that audio feedback works 100% reliably
        /// even without pre-imported external WAV/MP3 files.
        /// </summary>
        private AudioClip GetProceduralClip(string key)
        {
            if (proceduralClips.TryGetValue(key, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip generated = GeneratePCMClip(key);
            proceduralClips[key] = generated;
            return generated;
        }

        private AudioClip GeneratePCMClip(string key)
        {
            int sampleRate = 44100;
            float duration = 0.2f;
            System.Func<float, float> synthFunc = t => 0f;

            string k = key.ToLower();

            if (k.Contains("pistol") || k.Contains("fire_pistol"))
            {
                duration = 0.15f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * (400f - t * 2000f) * t) * Mathf.Exp(-20f * t) + (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-15f * t);
            }
            else if (k.Contains("shotgun"))
            {
                duration = 0.28f;
                synthFunc = t => (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-10f * t) + Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-8f * t);
            }
            else if (k.Contains("rifle") || k.Contains("assault"))
            {
                duration = 0.12f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * (600f - t * 3000f) * t) * Mathf.Exp(-25f * t) + (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-18f * t);
            }
            else if (k.Contains("reload"))
            {
                duration = 0.22f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * (800f + t * 400f) * t) * Mathf.Exp(-12f * t);
            }
            else if (k.Contains("empty") || k.Contains("click"))
            {
                duration = 0.05f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-40f * t);
            }
            else if (k.Contains("footstep"))
            {
                duration = 0.08f;
                synthFunc = t => (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-35f * t);
            }
            else if (k.Contains("zombie"))
            {
                duration = 0.35f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * (180f + Mathf.Sin(20f * t) * 40f) * t) * Mathf.Exp(-5f * t);
            }
            else if (k.Contains("click") || k.Contains("ui_button"))
            {
                duration = 0.04f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * 1000f * t) * Mathf.Exp(-50f * t);
            }
            else if (k.Contains("toast") || k.Contains("notification") || k.Contains("reward"))
            {
                duration = 0.3f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * (523.25f + t * 300f) * t) * Mathf.Exp(-6f * t);
            }
            else
            {
                // General fallback tone
                duration = 0.15f;
                synthFunc = t => Mathf.Sin(2f * Mathf.PI * 440f * t) * Mathf.Exp(-10f * t);
            }

            int numSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = Mathf.Clamp(synthFunc(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create($"PCM_{key}", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
