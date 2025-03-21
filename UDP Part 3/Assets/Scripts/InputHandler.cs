using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class InputHandler : MonoBehaviour
{
    private static InputHandler _instance;
    public static InputHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("InputHandler");
                _instance = go.AddComponent<InputHandler>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private InputAction moveUpAction;
    private InputAction moveDownAction;
    private InputAction moveLeftAction;
    private InputAction moveRightAction;
    private InputAction selectAction;
    private InputAction cancelAction;
    private InputAction tabLeftAction;
    private InputAction tabRightAction;

    private Vector2 movementInput = Vector2.zero;


    public enum InputMethod { Keyboard, Controller }
    public InputMethod CurrentInputMethod { get; private set; } = InputMethod.Keyboard;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InputActionAsset asset = InputSystemSetup.InputActions;

        if (asset == null)
        {
            Debug.LogError("Could not find the InputActionAsset from InputSystemSetup!");
            return; 
        }

        var actionMap = asset.FindActionMap("UI");

        if (actionMap == null)
        {
            Debug.LogError("Could not find the 'UI' action map in the assigned InputActionAsset!");
            return; 
        }

        moveUpAction = actionMap.FindAction("MoveUp");
        moveDownAction = actionMap.FindAction("MoveDown");
        moveLeftAction = actionMap.FindAction("MoveLeft");
        moveRightAction = actionMap.FindAction("MoveRight");
        selectAction = actionMap.FindAction("Select");
        cancelAction = actionMap.FindAction("Cancel");
        tabLeftAction = actionMap.FindAction("TabLeft");
        tabRightAction = actionMap.FindAction("TabRight");

        if (moveUpAction == null || moveDownAction == null || moveLeftAction == null || moveRightAction == null ||
            selectAction == null || cancelAction == null || tabLeftAction == null || tabRightAction == null)
        {
            Debug.LogError("One or more input actions were not found in the 'UI' action map!");
            return; 
        }

        moveUpAction.performed += ctx => { movementInput.y = 1; UpdateInputMethod(ctx.control.device); };
        moveUpAction.canceled += ctx => { if (movementInput.y > 0) movementInput.y = 0; };

        moveDownAction.performed += ctx => { movementInput.y = -1; UpdateInputMethod(ctx.control.device); };
        moveDownAction.canceled += ctx => { if (movementInput.y < 0) movementInput.y = 0; };

        moveLeftAction.performed += ctx => { movementInput.x = -1; UpdateInputMethod(ctx.control.device); };
        moveLeftAction.canceled += ctx => { if (movementInput.x < 0) movementInput.x = 0; };

        moveRightAction.performed += ctx => { movementInput.x = 1; UpdateInputMethod(ctx.control.device); };
        moveRightAction.canceled += ctx => { if (movementInput.x > 0) movementInput.x = 0; };

        selectAction.performed += ctx => UpdateInputMethod(ctx.control.device);
        cancelAction.performed += ctx => UpdateInputMethod(ctx.control.device);
    }

    private void OnEnable()
    {
        moveUpAction?.Enable();
        moveDownAction?.Enable();
        moveLeftAction?.Enable();
        moveRightAction?.Enable();
        selectAction?.Enable();
        cancelAction?.Enable();
        tabLeftAction?.Enable();
        tabRightAction?.Enable();
    }

    private void OnDisable()
    {
        moveUpAction?.Disable();
        moveDownAction?.Disable();
        moveLeftAction?.Disable();
        moveRightAction?.Disable();
        selectAction?.Disable();
        cancelAction?.Disable();
        tabLeftAction?.Disable();
        tabRightAction?.Disable();
    }

    private void UpdateInputMethod(InputDevice device)
    {
        if (device is Keyboard)
        {
            CurrentInputMethod = InputMethod.Keyboard;
        }
        else
        {
            CurrentInputMethod = InputMethod.Controller;
        }
    }

    public float GetHorizontalInput()
    {
        return movementInput.x;
    }

    public float GetVerticalInput()
    {
        return movementInput.y;
    }

    public bool InteractPressed()
    {
        return selectAction.WasPressedThisFrame();
    }

    public bool CancelPressed()
    {
        return cancelAction.WasPressedThisFrame();
    }

    public bool TabLeftPressed()
    {
        return tabLeftAction.WasPressedThisFrame();
    }

    public bool TabRightPressed()
    {
        return tabRightAction.WasPressedThisFrame();
    }

    public bool LeftPressed()
    {
        return moveLeftAction.WasPressedThisFrame();
    }

    public bool RightPressed()
    {
        return moveRightAction.WasPressedThisFrame();
    }

    public void DebugControllerInputs()
    {
        Debug.Log($"Movement Input: {movementInput}");

        var gamepads = Gamepad.all;
        if (gamepads.Count > 0)
        {
            var gamepad = gamepads[0];
            Debug.Log($"D-pad: Up={gamepad.dpad.up.isPressed}, Down={gamepad.dpad.down.isPressed}, " +
                      $"Left={gamepad.dpad.left.isPressed}, Right={gamepad.dpad.right.isPressed}");
        }
    }
}
