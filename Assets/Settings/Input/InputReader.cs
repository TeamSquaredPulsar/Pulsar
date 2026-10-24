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
    public event Action<Vector2> MoveEvent;
    public event Action ShootEvent;
    public event Action<float> RotateEvent;
    public event Action<Vector2> MoveCursorEvent;
    public event Action<bool> SelectTileEvent;

    [SerializeField]
    private CameraData camData;
    private Controls _controls;
    
    public Vector2 MoveDirection => _controls.Player.Move.ReadValue<Vector2>();

    private void Awake()
    {
        if (_controls is not null)
        {
            return;
        }

        CreateNewControls();
    }

    private void OnEnable()
    {
        if (_controls is null)
        {
            CreateNewControls();
        }

        _controls?.Player.Enable();
    }

    private void OnDisable()
    {
        _controls?.Player.Disable();
    }

    private void OnDestroy()
    {
        _controls.Dispose();
        _controls = null;
    }
    
    private void CreateNewControls()
    {
        _controls = new Controls();
        _controls.Player.SetCallbacks(this);
    }

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

    public void OnSelectTile(InputAction.CallbackContext context)
    {
        // Don't trigger on started because it causes
        // the event to fire twice on mouse click   
        if (context.started)
        {
            return;
        }
        //True if the button is down, false if up.
        SelectTileEvent?.Invoke(!context.canceled);
    }

    // [CB] Annoyingly, Unity will overwrite the controller inputs with mouse
    // inputs if we don't use 2 different functions. At least the event is unified :)
    public void OnMoveCursorGamepad(InputAction.CallbackContext context)
    {
        //Input comes in -1 to 1
        Vector2 input = context.ReadValue<Vector2>();

        //divide by 2 because we're measuring from the centre
        // of the screen, so we want -0.5 to 0.5
        input /= 2;

        // Multiply by camera dimensions so we can get full screen size
        input *= camData.ScreenSize;

        
        MoveCursorEvent?.Invoke(input);
    }

    public void OnMoveCursorMouse(InputAction.CallbackContext context)
    {
        MoveCursorEvent?.Invoke(context.ReadValue<Vector2>());
    }
}