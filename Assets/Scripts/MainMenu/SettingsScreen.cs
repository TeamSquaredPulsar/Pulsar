using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pulsar.UI
{
public class SettingsScreen : MonoBehaviour
{
    public static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900)
    };

    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private Button backButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button resolutionButton;
    [SerializeField] private TMP_Text resolutionLabel;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider vfxVolumeSlider;

    private void OnEnable()
    {
        GameSettings settings = GameSettingsStore.Current;
        volumeSlider.SetValueWithoutNotify(settings.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
        vfxVolumeSlider.SetValueWithoutNotify(settings.vfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);
        RefreshResolutionLabel();

        backButton.onClick.AddListener(Back);
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        resolutionButton.onClick.AddListener(CycleResolution);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        vfxVolumeSlider.onValueChanged.AddListener(SetVfxVolume);
        volumeSlider.Select();
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveListener(Back);
        volumeSlider.onValueChanged.RemoveListener(SetVolume);
        fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        resolutionButton.onClick.RemoveListener(CycleResolution);
        musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        vfxVolumeSlider.onValueChanged.RemoveListener(SetVfxVolume);
        GameSettingsStore.Save();
    }

    public void Back()
    {
        mainMenu.CloseSettings();
    }

    public void SetVolume(float value)
    {
        GameSettingsStore.SetMasterVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        GameSettingsStore.SetMusicVolume(value);
    }

    public void SetVfxVolume(float value)
    {
        GameSettingsStore.SetVfxVolume(value);
    }

    public void SetFullscreen(bool isOn)
    {
        GameSettingsStore.SetFullscreen(isOn);
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= Resolutions.Length)
        {
            return;
        }

        GameSettingsStore.SetResolution(Resolutions[index]);
        RefreshResolutionLabel();
    }

    private void CycleResolution()
    {
        int index = Array.IndexOf(Resolutions, GameSettingsStore.Current.resolution);
        SetResolution((index + 1) % Resolutions.Length);
    }

    private void RefreshResolutionLabel()
    {
        GameSettings settings = GameSettingsStore.Current;
        resolutionLabel.text = $"{settings.resolution.x} x {settings.resolution.y}";
    }
}
}
