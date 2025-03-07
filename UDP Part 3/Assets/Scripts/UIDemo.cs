/* 
UIDemo
Demonstrates options for handling user input with 
user interfaces designed with Unity UI Toolkit.

Copyright 2023 John M. Quick
*/

using UnityEngine;
using UnityEngine.UIElements;

public class UIDemo : MonoBehaviour {
    
    //UI document
    //assigned in Inspector
    public UIDocument doc;

    //UI doc root
    private VisualElement _root;

    //UI elements
    private Label _label;
    private Button _button;

    //counter for label text
    private int _counter;

    //whether the button is flipped
    private bool _isFlip;

    //awake
    private void Awake() {

        /*
        We want to enforce the screen resolution 
        on startup to prevent problems with the 
        window in the executable build.
        */

        //screen dimensions, in pixels
        //SNES resolution @4x
        int screenW = 1280;
        int screenH = 960;
        bool isFullScreen = false;

        //force default screen resolution
        Screen.SetResolution(screenW, screenH, isFullScreen);
    }

    //start
    void Start() {

        //init demo variables
        //reset counter
        _counter = 0;

        //reset flag
        _isFlip = false;

        /*
        The UI Toolkit is managed in code via a UI Document.
        Every UI Document has a root, which can be accessed 
        in code. From there, a query system allows us to 
        access each individual UI element. Further, we can 
        register callback functions that execute whenever a 
        specific user interaction occurs on a UI element.
        */

        //retrieve UI doc root
        _root = doc.rootVisualElement;

        //retrieve UI elements
        //search root via query by name
        //text label
        _label = _root.Q<Label>("TextBoxSingleLabel");

        //button
        _button = _root.Q<Button>("MerchantButton");

        //set initial label text
        _label.text = "The UI has been updated " + _counter + " time(s).";

        /* 
        This is an example of event-based input handling using
        callbacks. This is currently Unity's preferred method
        for handling user input with UI Toolkit.

        Some UI objects are focusable by default, like buttons,
        while others are not, like any VisualElement that may
        serve a wide variety of purposes.

        Focusing is more obvious in a UI that uses touch or 
        mouse input. If input happens "on" a UI object, such as
        a user touching a button, then we make sure to handle it.

        However, this demo uses distinct key controls. Similar
        to many, but not all, game controller UIs, the exact
        spatial location of the input is not important. What
        matters is that a specific input command is received.

        Therefore, we focus the entire container object and
        then differentiate the inputs as they arrive to ensure
        the correct result is achieved.
        */

        //register callback to handle user input
        _root.RegisterCallback<KeyUpEvent>(OnKeyUp);

        //allow visual element to be focused
        _root.focusable = true;

        //focus visual element
        _root.Focus();

        Debug.Log("[UIDemo] Callback Version: Press L to update text");
        Debug.Log("[UIDemo] Update Version: Press K to update button");
    }

    //update
    private void Update() {

        /*
        This is an older method for handling user input in Unity.
        We put special input functions inside the Update function
        to check for specific inputs.

        This system is not related to the UI Toolkit. It can be
        useful for quickly rigging up your mock keyboard controls,
        especially if you have used this Unity system in the past.
        */

        //if key released
        //K
        if (Input.GetKeyUp(KeyCode.K)) {

            //update button
            UpdateButton();
        }
    }

    //callbacks for user input
    //key released
    private void OnKeyUp(KeyUpEvent theEvent) {

        //check for key press
        //L
        if (theEvent.keyCode == KeyCode.L) {

            //update label
            UpdateLabel();
        }
    }

    //update the text label
    private void UpdateLabel() {

        //update counter
        _counter++;

        //update label
        _label.text = "The UI has been updated " + _counter + " time(s).";
    }

    //update the button presentation
    private void UpdateButton() {

        //store a scale style that defines the presentation of the button
        //default to normal orientation
        StyleScale flip = new StyleScale(new Vector2(1, 1));

        //if not flipped
        if (_isFlip == false) {

            //toggle flag
            _isFlip = true;

            //create a scale style with -1 on the x axis to flip
            flip = new StyleScale(new Vector2(-1, 1));
        }

        //if already flipped
        else if (_isFlip == true) {

            //toggle flag
            _isFlip = false;
        }

        //toggle button state
        _button.style.scale = flip;
    }
}