using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemSetup : MonoBehaviour
{
    public static InputActionAsset InputActions { get; private set; } 

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetupInputActions()
    {
        InputActions = Resources.Load<InputActionAsset>("PlayerInputActions");  

        if (InputActions == null)
        {
            Debug.LogError("Failed to load PlayerInputActions from Resources folder!");
            return;
        }

        InputActions.name = "PlayerControls";

        var actionMap = InputActions.FindActionMap("UI");

        if (actionMap == null)
        {
            Debug.LogError("Failed to find 'UI' action map in PlayerInputActions!");
            return;
        }

        CreateMovementAction(actionMap, "MoveUp", "<Keyboard>/w", "<Gamepad>/dpad/up");
        CreateMovementAction(actionMap, "MoveDown", "<Keyboard>/s", "<Gamepad>/dpad/down");
        CreateMovementAction(actionMap, "MoveLeft", "<Keyboard>/a", "<Gamepad>/dpad/left");
        CreateMovementAction(actionMap, "MoveRight", "<Keyboard>/d", "<Gamepad>/dpad/right");

        CreateButtonAction(actionMap, "Select", "<Keyboard>/l", "<Gamepad>/buttonEast");
        CreateButtonAction(actionMap, "Cancel", "<Keyboard>/k", "<Gamepad>/buttonSouth");
        CreateButtonAction(actionMap, "TabLeft", "<Keyboard>/q", "<Gamepad>/leftShoulder");
        CreateButtonAction(actionMap, "TabRight", "<Keyboard>/p", "<Gamepad>/rightShoulder");

        InputActions.Enable();

        Debug.Log("Input System configured programmatically!");
    }

    private static InputAction CreateMovementAction(InputActionMap map, string name, string keyboardBinding, string gamepadBinding)
    {
        var action = map.AddAction(name, InputActionType.Button);
        action.AddBinding(keyboardBinding).WithGroup("Keyboard");
        action.AddBinding(gamepadBinding).WithGroup("Gamepad");
        return action;
    }

    private static InputAction CreateButtonAction(InputActionMap map, string name, string keyboardBinding, string gamepadBinding)
    {
        var action = map.AddAction(name, InputActionType.Button);
        action.AddBinding(keyboardBinding).WithGroup("Keyboard");
        action.AddBinding(gamepadBinding).WithGroup("Gamepad");
        return action;
    }
}
