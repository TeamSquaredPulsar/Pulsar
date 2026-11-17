using System;
using System.IO;
using UnityEngine;

namespace Pulsar.UI
{
public static class GameSettingsStore
{
    private const string FileName = "gamecof.data";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static event Action<bool, Vector2Int> ScreenChanged;
    public static event Action<float> MasterVolumeChanged;
    public static event Action<float> MusicVolumeChanged;
    public static event Action<float> VfxVolumeChanged;

    private static GameSettings _current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Application.quitting -= Save;
        _current = null;
        ScreenChanged = null;
        MasterVolumeChanged = null;
        MusicVolumeChanged = null;
        VfxVolumeChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Load();
        Application.quitting += Save;
    }

    public static GameSettings Current
    {
        get
        {
            if (_current == null)
            {
                Load();
            }

            return _current;
        }
    }

    public static void Load()
    {
        GameSettings loaded = new GameSettings();
        if (File.Exists(FilePath))
        {
            try
            {
                loaded = Deserialize(File.ReadAllText(FilePath));
            }
            catch (Exception exception) when (exception is IOException
                                              || exception is UnauthorizedAccessException
                                              || exception is ArgumentException)
            {
                Debug.LogWarning($"Could not load settings from '{FilePath}': {exception.Message}. Using defaults.");
                loaded = new GameSettings();
            }
        }

        loaded.Normalize();
        _current = loaded;
        Apply();
        Save();
    }

    private static GameSettings Deserialize(string json)
    {
        GameSettings settings = new GameSettings();
        JsonUtility.FromJsonOverwrite(json, settings);

        return settings;
    }

    public static void Save()
    {
        if (_current == null)
        {
            return;
        }

        _current.Normalize();
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(FilePath, JsonUtility.ToJson(_current, true));
        }
        catch (Exception exception) when (exception is IOException
                                          || exception is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Could not save settings to '{FilePath}': {exception.Message}");
            return;
        }
    }

    public static void Apply()
    {
        if (_current == null)
        {
            Load();
            return;
        }

        ApplyDisplay();
        GameSettings settings = Current;
        MasterVolumeChanged?.Invoke(settings.masterVolume);
        MusicVolumeChanged?.Invoke(settings.musicVolume);
        VfxVolumeChanged?.Invoke(settings.vfxVolume);
    }

    private static void ApplyDisplay()
    {
        GameSettings settings = Current;
        settings.Normalize();
        Screen.SetResolution(settings.resolution.x, settings.resolution.y,
            settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        ScreenChanged?.Invoke(settings.fullscreen, settings.resolution);
    }

    public static void SetFullscreen(bool fullscreen)
    {
        Current.fullscreen = fullscreen;
        ApplyDisplay();
    }

    public static void SetResolution(Vector2Int resolution)
    {
        Current.resolution = resolution;
        ApplyDisplay();
    }

    public static void SetMasterVolume(float volume)
    {
        GameSettings settings = Current;
        settings.masterVolume = volume;
        settings.Normalize();
        MasterVolumeChanged?.Invoke(settings.masterVolume);
    }

    public static void SetMusicVolume(float volume)
    {
        GameSettings settings = Current;
        settings.musicVolume = volume;
        settings.Normalize();
        MusicVolumeChanged?.Invoke(settings.musicVolume);
    }

    public static void SetVfxVolume(float volume)
    {
        GameSettings settings = Current;
        settings.vfxVolume = volume;
        settings.Normalize();
        VfxVolumeChanged?.Invoke(settings.vfxVolume);
    }
}
}
