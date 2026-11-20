using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pulsar.UI
{
public class SettingsScreen : MonoBehaviour
{
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private Button backButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider vfxVolumeSlider;
    [SerializeField] private Button keybindingButton;
    [SerializeField] private ControlsScreen controlsScreen;

    private void OnEnable()
    {
        GameSettings settings = GameSettingsStore.Current;
        volumeSlider.SetValueWithoutNotify(settings.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
        vfxVolumeSlider.SetValueWithoutNotify(settings.vfxVolume);
        fullscreenToggle.SetIsOnWithoutNotify(settings.fullscreen);
        RefreshResolutionDropdown();

        keybindingButton.onClick.AddListener(controlsScreen.Open);
        backButton.onClick.AddListener(Back);
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        vfxVolumeSlider.onValueChanged.AddListener(SetVfxVolume);
        volumeSlider.Select();
    }

    private void OnDisable()
    {
        keybindingButton.onClick.RemoveListener(controlsScreen.Open);
        backButton.onClick.RemoveListener(Back);
        volumeSlider.onValueChanged.RemoveListener(SetVolume);
        fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
        musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        vfxVolumeSlider.onValueChanged.RemoveListener(SetVfxVolume);
        GameSettingsStore.Save();
    }

    public void SelectKeybindingButton()
    {
        keybindingButton.Select();
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
        if (index < 0 || index >= GameSettings.Resolutions.Length)
        {
            return;
        }

        GameSettingsStore.SetResolution(GameSettings.Resolutions[index]);
        resolutionDropdown.SetValueWithoutNotify(index);
        resolutionDropdown.RefreshShownValue();
    }

    private void RefreshResolutionDropdown()
    {
        List<string> options = new List<string>(GameSettings.Resolutions.Length);
        options.AddRange(GameSettings.Resolutions.Select(resolution => $"{resolution.x} x {resolution.y}"));

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.interactable = options.Count > 0;
        int index = Array.IndexOf(GameSettings.Resolutions, GameSettingsStore.Current.resolution);
        resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        resolutionDropdown.RefreshShownValue();
    }
}
}
