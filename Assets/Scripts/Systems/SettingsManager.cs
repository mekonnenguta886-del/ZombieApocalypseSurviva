using UnityEngine;
using ZombieApocalypse.Audio;
using ZombieApocalypse.Player;

namespace ZombieApocalypse.Settings
{
    /// <summary>
    /// Central persistent Settings Manager for Phase 20.
    /// Manages graphics quality levels, VSync, resolution, fullscreen, audio volumes,
    /// and mouse sensitivity/invert Y controls with PlayerPrefs persistence.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        // Preference Keys
        private const string KEY_QUALITY = "Settings_QualityLevel";
        private const string KEY_VSYNC = "Settings_VSync";
        private const string KEY_FULLSCREEN = "Settings_Fullscreen";
        private const string KEY_MASTER_VOL = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOL = "Settings_MusicVolume";
        private const string KEY_SFX_VOL = "Settings_SFXVolume";
        private const string KEY_SENSITIVITY = "Settings_MouseSensitivity";
        private const string KEY_INVERT_Y = "Settings_InvertY";

        // Current Settings State
        public int QualityLevel { get; private set; } = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        public bool VSyncEnabled { get; private set; } = true;
        public bool FullscreenEnabled { get; private set; } = true;
        public float MasterVolume { get; private set; } = 1.0f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SFXVolume { get; private set; } = 0.9f;
        public float MouseSensitivity { get; private set; } = 2.0f;
        public bool InvertY { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
        }

        private void Start()
        {
            ApplyAllSettings();
        }

        public void LoadSettings()
        {
            QualityLevel = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.names.Length > 2 ? 2 : QualitySettings.names.Length - 1);
            VSyncEnabled = PlayerPrefs.GetInt(KEY_VSYNC, 1) == 1;
            FullscreenEnabled = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1.0f);
            MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
            SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.9f);
            MouseSensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 2.0f);
            InvertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetInt(KEY_QUALITY, QualityLevel);
            PlayerPrefs.SetInt(KEY_VSYNC, VSyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_FULLSCREEN, FullscreenEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
            PlayerPrefs.SetFloat(KEY_SENSITIVITY, MouseSensitivity);
            PlayerPrefs.SetInt(KEY_INVERT_Y, InvertY ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyAllSettings()
        {
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            ApplyGameplaySettings();
        }

        public void ApplyGraphicsSettings()
        {
            if (QualityLevel >= 0 && QualityLevel < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(QualityLevel, true);
            }
            QualitySettings.vSyncCount = VSyncEnabled ? 1 : 0;
            Screen.fullScreen = FullscreenEnabled;
        }

        public void ApplyAudioSettings()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(MasterVolume);
                AudioManager.Instance.SetMusicVolume(MusicVolume);
                AudioManager.Instance.SetSFXVolume(SFXVolume);
            }
        }

        public void ApplyGameplaySettings()
        {
            PlayerCamera camera = FindObjectOfType<PlayerCamera>();
            if (camera != null)
            {
                camera.MouseSensitivity = MouseSensitivity;
                camera.InvertY = InvertY;
            }
        }

        public void SetQualityLevel(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(QualityLevel, true);
            SaveSettings();
        }

        public void SetPerformancePreset(string presetName)
        {
            switch (presetName.ToUpper())
            {
                case "LOW":
                    SetQualityLevel(0);
                    SetVSync(false);
                    break;
                case "MEDIUM":
                    SetQualityLevel(1);
                    SetVSync(true);
                    break;
                case "HIGH":
                    SetQualityLevel(2);
                    SetVSync(true);
                    break;
                case "ULTRA":
                    SetQualityLevel(QualitySettings.names.Length - 1);
                    SetVSync(true);
                    break;
            }
        }

        public void SetVSync(bool enabled)
        {
            VSyncEnabled = enabled;
            QualitySettings.vSyncCount = VSyncEnabled ? 1 : 0;
            SaveSettings();
        }

        public void SetFullscreen(bool enabled)
        {
            FullscreenEnabled = enabled;
            Screen.fullScreen = FullscreenEnabled;
            SaveSettings();
        }

        public void SetMasterVolume(float vol)
        {
            MasterVolume = Mathf.Clamp01(vol);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(MasterVolume);
            SaveSettings();
        }

        public void SetMusicVolume(float vol)
        {
            MusicVolume = Mathf.Clamp01(vol);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(MusicVolume);
            SaveSettings();
        }

        public void SetSFXVolume(float vol)
        {
            SFXVolume = Mathf.Clamp01(vol);
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(SFXVolume);
            SaveSettings();
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            MouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            ApplyGameplaySettings();
            SaveSettings();
        }

        public void SetInvertY(bool invert)
        {
            InvertY = invert;
            ApplyGameplaySettings();
            SaveSettings();
        }
    }
}
