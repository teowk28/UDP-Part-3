#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SNSControllerSetup : EditorWindow
{
    [MenuItem("Tools/Setup SNS Controller")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SNSControllerSetup), false, "SNS Controller Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("SNS Controller Input Mapping", EditorStyles.boldLabel);

        if (GUILayout.Button("Configure Controller Inputs"))
        {
            ConfigureInputs();
        }
    }

    private void ConfigureInputs()
    {
        // Get the serialized input manager
        SerializedObject inputManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);

        SerializedProperty axesProperty = inputManager.FindProperty("m_Axes");

        // Configure each input
        AddOrUpdateAxis(axesProperty, InputHandler.ACTION_INTERACT, "l", "joystick button 0");
        AddOrUpdateAxis(axesProperty, InputHandler.ACTION_CANCEL, "k", "joystick button 1");
        AddOrUpdateAxis(axesProperty, InputHandler.ACTION_TAB_LEFT, "q", "joystick button 4");
        AddOrUpdateAxis(axesProperty, InputHandler.ACTION_TAB_RIGHT, "p", "joystick button 5");
        AddOrUpdateAxis(axesProperty, InputHandler.ACTION_NAVIGATE_LEFT, "a", "joystick button 14");
        AddOrUpdateAxis(axesProperty, InputHandler.ACTION_NAVIGATE_RIGHT, "d", "joystick button 15");

        // Save the changes
        inputManager.ApplyModifiedProperties();

        Debug.Log("SNS Controller mapping complete!");
    }

    private void AddOrUpdateAxis(SerializedProperty axesProperty, string name, string keyboardKey, string joystickButton)
    {
        // Check if input already exists
        bool exists = false;
        for (int i = 0; i < axesProperty.arraySize; i++)
        {
            SerializedProperty axis = axesProperty.GetArrayElementAtIndex(i);
            if (axis.FindPropertyRelative("m_Name").stringValue == name)
            {
                // Input exists, update it
                ConfigureAxisProperty(axis, name, keyboardKey, joystickButton);
                exists = true;
                break;
            }
        }

        // If input doesn't exist, add it
        if (!exists)
        {
            axesProperty.arraySize++;
            SerializedProperty newAxis = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);
            ConfigureAxisProperty(newAxis, name, keyboardKey, joystickButton);
        }
    }

    private void ConfigureAxisProperty(SerializedProperty axisProperty, string name, string keyboardKey, string joystickButton)
    {
        axisProperty.FindPropertyRelative("m_Name").stringValue = name;
        axisProperty.FindPropertyRelative("descriptiveName").stringValue = "";
        axisProperty.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
        axisProperty.FindPropertyRelative("negativeButton").stringValue = "";
        axisProperty.FindPropertyRelative("positiveButton").stringValue = joystickButton;
        axisProperty.FindPropertyRelative("altNegativeButton").stringValue = "";
        axisProperty.FindPropertyRelative("altPositiveButton").stringValue = keyboardKey;
        axisProperty.FindPropertyRelative("gravity").floatValue = 1000f;
        axisProperty.FindPropertyRelative("dead").floatValue = 0.001f;
        axisProperty.FindPropertyRelative("sensitivity").floatValue = 1000f;
        axisProperty.FindPropertyRelative("snap").boolValue = true;
        axisProperty.FindPropertyRelative("invert").boolValue = false;
        axisProperty.FindPropertyRelative("type").intValue = 0; // Key or Mouse Button
        axisProperty.FindPropertyRelative("axis").intValue = 0; // X axis
        axisProperty.FindPropertyRelative("joyNum").intValue = 0; // All Joysticks
    }
}
#endif