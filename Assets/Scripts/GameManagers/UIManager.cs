using Pulsar.DataClasses;
using System.Collections.Generic;
using Audune.Utils.Dictionary;
using Pulsar.Player;
using TMPro;
using UnityEngine;

// ReSharper disable AsyncVoidMethod

namespace Pulsar
{
public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    [field:
        SerializableDictionaryOptions(keyHeader = "State", valueHeader = "UI")]
    private SerializableDictionary<GameState, GameObject> uiStates = new();

    [SerializeField]
    private Canvas uiCanvas;

    protected override void Awake()
    {
        base.Awake();
        CameraData.MainCameraChanged += OnMainCameraChanged;
        StateManager.GameStateChanged += SetUIMode;
    }

    private void OnDestroy()
    {
        CameraData.MainCameraChanged -= OnMainCameraChanged;
        StateManager.GameStateChanged -= SetUIMode;
    }

    private void OnMainCameraChanged(Camera cam)
    {
        uiCanvas.worldCamera = cam;
    }

    private void SetUIMode(GameState state)
    {
        foreach (KeyValuePair<GameState, GameObject> uiState in uiStates)
        {
            uiState.Value.gameObject.SetActive(uiState.Key == state);
        }
    }

    public async void OnStartGameClicked()
    {
        await GameManager.Instance.LoadLevel(GameState.Playing);
    }

    public async void OnToMainMenuClicked()
    {
        await GameManager.Instance.LoadLevel(GameState.MainMenu);
    }

    public async void OnSandboxClicked()
    {
        await GameManager.Instance.LoadLevel(GameState.Sandbox);
    }

    public async void OnTutorialClicked()
    {
        await GameManager.Instance.LoadLevel(GameState.Tutorial);
    }
}
}
