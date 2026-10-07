using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieApocalypse.Settings;

namespace ZombieApocalypse.UI
{
    /// <summary>
    /// UI Controller for Settings Panel.
    /// Drives graphics quality presets, resolution/fullscreen, VSync, audio sliders,
    /// mouse sensitivity, and Invert-Y options, linking directly to SettingsManager.
    /// 
    /// ATTACH TO: SettingsPanel GameObject in scene.
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject settingsPanelContainer;

        [Header("Performance Presets")]
        [SerializeField] private Button lowPresetButton;
        [SerializeField] private Button mediumPresetButton;
        [SerializeField] private Button highPresetButton;
        [SerializeField] private Button ultraPresetButton;

        [Header("Graphics Controls")]
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Audio Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Gameplay Controls")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle invertYToggle;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        private void Start()
        {
            BindEvents();
            RefreshUI();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void BindEvents()
        {
            if (lowPresetButton != null) lowPresetButton.onClick.AddListener(() => SetPreset("LOW"));
            if (mediumPresetButton != null) mediumPresetButton.onClick.AddListener(() => SetPreset("MEDIUM"));
            if (highPresetButton != null) highPresetButton.onClick.AddListener(() => SetPreset("HIGH"));
            if (ultraPresetButton != null) ultraPresetButton.onClick.AddListener(() => SetPreset("ULTRA"));

            if (vsyncToggle != null) vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            if (invertYToggle != null) invertYToggle.onValueChanged.AddListener(OnInvertYChanged);

            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        }

        public void RefreshUI()
        {
            SettingsManager mgr = SettingsManager.Instance;
            if (mgr == null) return;

            if (vsyncToggle != null) vsyncToggle.isOn = mgr.VSyncEnabled;
            if (fullscreenToggle != null) fullscreenToggle.isOn = mgr.FullscreenEnabled;

            if (masterVolumeSlider != null) masterVolumeSlider.value = mgr.MasterVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = mgr.MusicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = mgr.SFXVolume;

            if (sensitivitySlider != null) sensitivitySlider.value = mgr.MouseSensitivity;
            if (invertYToggle != null) invertYToggle.isOn = mgr.InvertY;
        }

        public void OpenPanel()
        {
            RefreshUI();
            if (settingsPanelContainer != null)
            {
                settingsPanelContainer.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void ClosePanel()
        {
            if (settingsPanelContainer != null)
            {
                settingsPanelContainer.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void SetPreset(string preset)
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SetPerformancePreset(preset);
                RefreshUI();
            }
        }

        private void OnVSyncChanged(bool value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetVSync(value);
        }

        private void OnFullscreenChanged(bool value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetFullscreen(value);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetSFXVolume(value);
        }

        private void OnSensitivityChanged(float value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetMouseSensitivity(value);
        }

        private void OnInvertYChanged(bool value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetInvertY(value);
        }
    }
}
