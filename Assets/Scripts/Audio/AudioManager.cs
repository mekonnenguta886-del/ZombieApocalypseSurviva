using UnityEngine;

namespace ZombieApocalypse.Audio
{
    /// <summary>
    /// Central sound and music controller, handling SFX playback, 3D spatial audio, and music tracks.
    /// 
    /// ATTACH TO: Persistent Audio GameObject (e.g. "[AudioManager]").
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        public void PlayMusic(AudioClip musicClip, bool loop = true)
        {
            if (musicSource == null || musicClip == null) return;
            musicSource.clip = musicClip;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }
}
