using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputBindingStore
{
    private const string Key = "Pulsar.InputBindings";
    public static event Action Changed;

    public static void Load(InputActionAsset asset)
    {
        asset.RemoveAllBindingOverrides();
        if (!PlayerPrefs.HasKey(Key)) return;
        try
        {
            asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(Key));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not load saved controls: {exception.Message}");
        }
    }

    public static void Save(InputActionAsset asset)
    {
        PlayerPrefs.SetString(Key, asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
