using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace Pulsar.UI
{
public class ControlsScreen : MonoBehaviour
{
    [System.Serializable]
    private class BindingRow
    {
        public string actionName;
        public string bindingId;
        public string displayName;
        public Button button;
        public TMP_Text label;
    }

    [SerializeField]
    private SettingsScreen settingsScreen;

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private GameObject keyboardBindings;

    [SerializeField]
    private GameObject gamepadBindings;

    [SerializeField]
    private TMP_Text heading;

    [SerializeField]
    private Button deviceButton;

    [SerializeField]
    private TMP_Text deviceLabel;

    [SerializeField]
    private Button resetButton;

    [SerializeField]
    private Button backButton;

    [SerializeField]
    private BindingRow[] rows;

    private Controls _controls;
    private readonly List<Button> _rows = new();
    private readonly List<(InputAction action, int index)> _bindings = new();
    private InputActionRebindingExtensions.RebindingOperation _operation;
    private bool _gamepad;
    private bool _wasNavigating;
    private Button _rebindButton;
    private EventSystem _eventSystem;

    private void Awake()
    {
        _controls = new Controls();
        InputBindingStore.Load(_controls.asset);
        foreach (BindingRow row in rows)
        {
            InputAction action
                = _controls.asset.FindAction(row.actionName, true);
            int bindingIndex = -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].id.ToString() == row.bindingId)
                {
                    bindingIndex = i;
                }
            }

            if (bindingIndex < 0)
            {
                throw new System.InvalidOperationException(
                    $"Missing binding {row.bindingId} for {row.actionName}");
            }

            int rowIndex = _rows.Count;
            _rows.Add(row.button);
            _bindings.Add((action, bindingIndex));
            row.button.onClick.AddListener(() => BeginRebind(rowIndex));
        }

        deviceButton.onClick.AddListener(SwitchDevice);
        resetButton.onClick.AddListener(ResetBindings);
        backButton.onClick.AddListener(Close);
        Refresh();
    }

    public void Open()
    {
        settingsScreen.gameObject.SetActive(false);
        panel.SetActive(true);
        Refresh();
        deviceButton.Select();
    }

    private void Close()
    {
        if (_operation != null)
        {
            return;
        }

        panel.SetActive(false);
        settingsScreen.gameObject.SetActive(true);
        settingsScreen.SelectKeybindingButton();
    }

    private void SwitchDevice()
    {
        _gamepad = !_gamepad;
        Refresh();
        deviceButton.Select();
    }

    private void ResetBindings()
    {
        _controls.asset.RemoveAllBindingOverrides();
        InputBindingStore.Save(_controls.asset);
        Refresh();
        resetButton.Select();
    }

    private void Refresh()
    {
        heading.text = "Select a binding to change it";
        deviceLabel.text
            = _gamepad ? "Witch to keyboard / mouse" : "Switch to gamepad";
        keyboardBindings.SetActive(!_gamepad);
        gamepadBindings.SetActive(_gamepad);
        for (int i = 0; i < rows.Length; i++)
        {
            (InputAction action, int index) binding = _bindings[i];
            rows[i].label.text
                = $"{rows[i].displayName}: {binding.action.GetBindingDisplayString(binding.index)}";
        }
    }

    private void BeginRebind(int rowIndex)
    {
        if (_operation != null)
        {
            return;
        }

        (InputAction action, int index) binding = _bindings[rowIndex];
        _rebindButton = _rows[rowIndex];
        _eventSystem = EventSystem.current;
        _wasNavigating
            = _eventSystem != null && _eventSystem.sendNavigationEvents;
        if (_eventSystem != null)
        {
            _eventSystem.sendNavigationEvents = false;
        }

        SetInteractable(false);
        heading.text = _gamepad
            ? "Press a button / Esc to cancel"
            : "Press a key or mouse button / Esc to cancel";
        _operation = binding.action.PerformInteractiveRebinding(binding.index)
            .WithCancelingThrough(_gamepad
                ? "<Gamepad>/buttonEast"
                : "<Keyboard>/escape")
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .OnMatchWaitForAnother(0.1f)
            .OnCancel(_ => FinishRebind(false))
            .OnComplete(_ => FinishRebind(true));

        if (_gamepad)
        {
            _operation.WithControlsHavingToMatchPath("<Gamepad>");
        }
        else
        {
            _operation.WithExpectedControlType<ButtonControl>()
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithControlsHavingToMatchPath("<Mouse>")
                .WithControlsExcluding("<Keyboard>/anyKey");
        }

        _operation.Start();
    }

    private void FinishRebind(bool save)
    {
        _operation.Dispose();
        _operation = null;
        if (_eventSystem != null)
        {
            _eventSystem.sendNavigationEvents = _wasNavigating;
        }

        if (save)
        {
            InputBindingStore.Save(_controls.asset);
        }

        int selected = _rows.IndexOf(_rebindButton);
        SetInteractable(true);
        Refresh();
        if (selected >= 0 && selected < _rows.Count)
        {
            _rows[selected].Select();
        }
    }

    private void SetInteractable(bool value)
    {
        deviceButton.interactable = resetButton.interactable
            = backButton.interactable = value;
        foreach (Button row in _rows)
        {
            row.interactable = value;
        }
    }

    private void Update()
    {
        if (!panel.activeSelf)
        {
            return;
        }

        if (_operation == null)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            _operation.Cancel();
        }
    }

    private void OnDisable()
    {
        _operation?.Cancel();
    }

    private void OnDestroy()
    {
        _operation?.Dispose();
        _controls?.Dispose();
    }
}
}
