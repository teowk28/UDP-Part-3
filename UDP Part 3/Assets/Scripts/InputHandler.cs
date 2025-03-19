using UnityEngine;


public class InputHandler : MonoBehaviour
{
    // Singleton pattern for easy global access
    private static InputHandler _instance;
    public static InputHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                // Create a new GameObject with this component if it doesn't exist
                GameObject go = new GameObject("InputHandler");
                _instance = go.AddComponent<InputHandler>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Constant names to be used for controller button mappings in Input Manager
    public const string ACTION_INTERACT = "Interact";       // L key → A button
    public const string ACTION_CANCEL = "Cancel";           // K key → B button
    public const string ACTION_TAB_LEFT = "TabLeft";        // Q key → L button
    public const string ACTION_TAB_RIGHT = "TabRight";      // P key → R button
    public const string ACTION_NAVIGATE_LEFT = "Left";      // A key → D-pad/stick left
    public const string ACTION_NAVIGATE_RIGHT = "Right";    // D key → D-pad/stick right

    // Used to detect current input method for UI adjustments (optional)
    public enum InputMethod { Keyboard, Controller }
    public InputMethod CurrentInputMethod { get; private set; } = InputMethod.Keyboard;

    private void Awake()
    {
        // Singleton setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        Debug.Log($"Connected joysticks: {string.Join(", ", Input.GetJoystickNames())}");
        // Detect if controller or keyboard is being used
        DetectInputMethod();
    }


    private void DetectInputMethod()
    {
        // Check for controller button presses or stick movement
        if (Input.GetJoystickNames().Length > 0)
        {
            // Check all controller inputs that are mapped
            if (Input.GetButtonDown(ACTION_INTERACT) ||
                Input.GetButtonDown(ACTION_CANCEL) ||
                Input.GetButtonDown(ACTION_TAB_LEFT) ||
                Input.GetButtonDown(ACTION_TAB_RIGHT) ||
                Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.5f ||
                Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.5f)
            {
                CurrentInputMethod = InputMethod.Controller;
                return;
            }
        }

        // Check for any keyboard input
        if (Input.GetKeyDown(KeyCode.L) ||
            Input.GetKeyDown(KeyCode.K) ||
            Input.GetKeyDown(KeyCode.Q) ||
            Input.GetKeyDown(KeyCode.P) ||
            Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.S))
        {
            CurrentInputMethod = InputMethod.Keyboard;
        }
    }


    public bool GetButtonDown(KeyCode keyCode, string buttonName)
    {
        return Input.GetKeyDown(keyCode) || Input.GetButtonDown(buttonName);
    }

    public float GetDirectionalInput(KeyCode negativeKey, KeyCode positiveKey, string axisName)
    {
        // First check keyboard
        float keyboardValue = 0f;
        if (Input.GetKey(negativeKey)) keyboardValue -= 1f;
        if (Input.GetKey(positiveKey)) keyboardValue += 1f;

        // If keyboard is being used, return its value
        if (keyboardValue != 0f) return keyboardValue;

        // Otherwise return controller axis value
        return Input.GetAxisRaw(axisName);
    }

    // Convenience methods for common inputs used in your code

    public bool InteractPressed() => GetButtonDown(KeyCode.L, ACTION_INTERACT);

    public bool CancelPressed() => GetButtonDown(KeyCode.K, ACTION_CANCEL);

    public bool TabLeftPressed() => GetButtonDown(KeyCode.Q, ACTION_TAB_LEFT);

    public bool TabRightPressed() => GetButtonDown(KeyCode.P, ACTION_TAB_RIGHT);

    public bool LeftPressed() => GetButtonDown(KeyCode.A, ACTION_NAVIGATE_LEFT);

    public bool RightPressed() => GetButtonDown(KeyCode.D, ACTION_NAVIGATE_RIGHT);

    public float GetHorizontalInput() => GetDirectionalInput(KeyCode.A, KeyCode.D, "Horizontal");

    public float GetVerticalInput() => GetDirectionalInput(KeyCode.S, KeyCode.W, "Vertical");
}