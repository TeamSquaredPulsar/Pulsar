using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static Controls;

public interface IInputReader
{
    Vector2 MoveDirection { get; }
    void EnablePlayerActions();
}

[CreateAssetMenu(fileName = "NewInputReader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, IPlayerActions, IInputReader
{
    private Controls _controls;

    private void OnEnable()
    {
        if (_controls != null)
        {
            return;
        }

        _controls = new Controls();
        _controls.Player.SetCallbacks(this);
    }

    private void OnDisable()
    {
        _controls.Player.Disable();
    }

    private void OnDestroy()
    {
        _controls.Dispose();
    }

    public Vector2 MoveDirection => _controls.Player.Move.ReadValue<Vector2>();

    public void EnablePlayerActions()
    {
        _controls.Player.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        ShootEvent?.Invoke();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        RotateEvent?.Invoke(context.ReadValue<float>());
    }

    public void OnToggleRadialMenu(InputAction.CallbackContext context)
    {
        ToggleRadialMenuEvent?.Invoke(context.started);
    }

    public void OnMouseCursor(InputAction.CallbackContext context)
    {
        MouseCursorEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnControllerRightStickCorsor(InputAction.CallbackContext context)
    {
        ControllerRightStickCorsor?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnSelectFloatingTile(InputAction.CallbackContext context)
    {
        SelectFloatingTileEvent?.Invoke(context.started);
    }

    public void OnBuildTile(InputAction.CallbackContext context)
    {
        BuildTileEvent?.Invoke(context);
    }

    public void OnDetachTile(InputAction.CallbackContext context)
    {
        DetachTileEvent?.Invoke(context.started);
    }
    
    public event Action<Vector2> MoveEvent;
    public event Action ShootEvent;
    public event Action<float> RotateEvent;
    public event Action<bool> DetachTileEvent;
    public event Action<bool> SelectFloatingTileEvent;
    public event Action<Vector2> MouseCursorEvent;
    public event Action<Vector2> ControllerRightStickCorsor;
    public event Action<bool> ToggleRadialMenuEvent;
    public event Action<InputAction.CallbackContext> BuildTileEvent;
}