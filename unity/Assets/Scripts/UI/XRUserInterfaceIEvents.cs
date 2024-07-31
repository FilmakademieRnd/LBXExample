using FairyGUI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System;
using tracer;
using UnityEngine;

public class XRUserInterfaceEvents : MonoBehaviour
{
    public Transform positionSource;
    public float distance = 0.5f;
    public float verticalOffset = -0.5f;
    private GameObject xrMenuInteractionManager;
    private GComponent _mainView;
    private XRUserInterface xrUserInterface;
    private MinoPlayerCharacter m_playerCharacter;
    private Transform m_playerAvatar;
    private GameObject vrHandRight;
    private GameObject vrHandLeft;

    void Start()
    {
        _mainView = this.GetComponent<UIPainter>().ui;
        ResourceRequest request = Resources.LoadAsync("Build", typeof(BuildScriptableObject));
        request.completed += ChangeBuildVersion;

        // Get XRUserInterface
        xrUserInterface = FindObjectOfType<XRUserInterface>();
        m_playerCharacter = xrUserInterface.minoGameManager.GetComponent<MinoGameManager>().m_playerCharacter;
        m_playerAvatar = m_playerCharacter.transform.Find("PlayerAvatar");
        m_playerAvatar.gameObject.SetActive(false);
        xrMenuInteractionManager = GameObject.Find("XR Menu Interaction Manager");
        vrHandRight = GameObject.Find("VRHANDRIGHT");
        vrHandLeft = GameObject.Find("VRHANDLEFT");
        vrHandRight.SetActive(false);
        vrHandLeft.SetActive(false);


        positionSource = GameObject.FindGameObjectWithTag("MainCamera").transform;

        // Ray Interactor
        EnableRayInteractor();

        // Load the state of the buttons
        LoadTextInputState("ip_address");
        LoadButtonState("debug");
        LoadButtonState("controller_movement");

        // Add event listeners to all buttons and input fields in the Settings group
        int cnt = _mainView.numChildren;
        for (int i = 0; i < cnt; i++)
        {
            GObject obj = _mainView.GetChildAt(i);
            if (obj.group != null && obj.group.name == "Settings" || obj.group !=  null && obj.group.name == "Play the Game")
            {
                if (obj is GButton)
                {
                    ((GButton)obj).onClick.Add(OnButtonStateChange);
                }
                else if (obj is GTextInput)
                {
                    //((GTextInput)obj).onFocusIn.Add(OnTextInputClick);
                    ((GTextInput)obj).onClick.Add(() => OnTextInputClick(obj.name));
                    ((GTextInput)obj).onChanged.Add(() => OnTextInputChanged(obj.name));
                }
            }
        }

    }


    #region UI Events
    /// <summary>
    /// Saves the state of the button to PlayerPrefs.
    /// </summary>
    /// <param name="fieldName">The name of the button to save the state for.</param>
    public void SaveButtonState(string fieldName)
    {
        bool selected = _mainView.GetChild(fieldName).asButton.selected;
        PlayerPrefs.SetInt(fieldName, selected ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads the state of the button from PlayerPrefs.
    /// If a saved state exists, it sets the selected state of the button to the saved state.
    /// </summary>
    /// <param name="fieldName">The name of the button to load the state for.</param>
    public void LoadButtonState(string fieldName)
    {
        if (PlayerPrefs.HasKey(fieldName))
        {
            int savedState = PlayerPrefs.GetInt(fieldName);
            bool selected = savedState == 1;
            _mainView.GetChild(fieldName).asButton.selected = selected;
        }
    }

    /// <summary>
    /// Gets the state of the button from PlayerPrefs.
    /// If a saved state exists, it returns the selected state of the button.
    /// If no saved state exists, it returns false.
    /// </summary>
    /// <param name="fieldName">The name of the button to get the state for.</param>
    /// <returns>True if the button is selected, false otherwise.</returns>
    public bool GetButtonState(string fieldName)
    {
        if(PlayerPrefs.HasKey(fieldName))
        {
            int savedState = PlayerPrefs.GetInt(fieldName);
            bool selected = savedState == 1;
            return selected;
        }

        return false;
    }

    /// <summary>
    /// Saves the state of the text input field to PlayerPrefs.
    /// </summary>
    /// <param name="fieldName">The name of the field to save the state for.</param>
    /// <param name="text">The text to save.</param>
    public void SaveTextInputState(string fieldName, string text)
    {
        Debug.Log("SAVED " + fieldName);
        Debug.Log("SAVED " + text);

        PlayerPrefs.SetString(fieldName, text);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads the state of the text input field from PlayerPrefs.
    /// If a saved state exists, it sets the text of the input field to the saved state.
    /// </summary>
    /// <param name="fieldName">The name of the field to load the state for.</param>
    public void LoadTextInputState(string fieldName)
    {
        if (PlayerPrefs.HasKey(fieldName))
        {
            string savedText = PlayerPrefs.GetString(fieldName);
            _mainView.GetChild(fieldName).text = savedText;
        }
    }

    /// <summary>
    /// Gets the text input from PlayerPrefs.
    /// If a saved state exists, it returns the saved text.
    /// If no saved state exists, it returns an empty string.
    /// </summary>
    /// <param name="fieldName">The name of the field to get the text for.</param>
    /// <returns>The saved text if it exists, an empty string otherwise.</returns>
    public string GetTextInput(string fieldName)
    {
        if(PlayerPrefs.HasKey(fieldName))
        {
            string savedText = PlayerPrefs.GetString(fieldName);
            return savedText;
        }

        return "";
    }

    /// <summary>
    /// Event handler for text input changes. This method is triggered when the text in an input field changes.
    /// It saves the state of the text input field.
    /// </summary>
    /// <param name="fieldName">The name of the field that has changed.</param>
    public void OnTextInputChanged(string fieldName)
    {
        if (fieldName == "build_version") return;

        Debug.Log("CHANGED " + fieldName);
        GTextInput textInput = _mainView.GetChild(fieldName).asTextInput;
        string text = textInput.text;
        SaveTextInputState(fieldName, text);
    }

    /// <summary>
    /// Changes the build version displayed in the UI.
    /// </summary>
    /// <param name="version">The new build version to display.</param>
    public void ChangeBuildVersion(AsyncOperation obj)
    {
        BuildScriptableObject buildScriptableObject = ((ResourceRequest)obj).asset as BuildScriptableObject;

        if(buildScriptableObject == null)
        {
            Debug.LogError("BuildScriptableObject is null.");
            return;
        } else
        {
            GTextInput textInput = _mainView.GetChild("build_version").asTextInput;
            textInput.text = buildScriptableObject.date + " " + buildScriptableObject.buildNumber;
        }
    }


    /// <summary>
    /// Event handler for text input field click. This method is triggered when a text input field is clicked.
    /// It presents the non-native keyboard for text input.
    /// </summary>
    public void OnTextInputClick(string fieldName)
    {
        if(fieldName == "build_version") return;

        GTextInput textInput = _mainView.GetChild(fieldName).asTextInput;
        string text = (string)textInput.text;

        NonNativeKeyboard.Instance.InputField = textInput;
        NonNativeKeyboard.Instance.PresentKeyboard(text, NonNativeKeyboard.LayoutType.Symbol);

        // Move the cursor to the end of the text
        //Debug.Log("Caret position after PresentKeyboard: " + textInput.caretPosition);
        //textInput.caretPosition = 3;
        //textInput.SetSelection(3, 0);
        for (int i = 0; i < text.Length; i++)
        {
            NonNativeKeyboard.Instance.MoveCaretRight();
        }

        Vector3 direction = positionSource.forward;
        direction.y = 0;
        direction.Normalize();

        Vector3 targetPosition = positionSource.position + direction * distance + Vector3.up * verticalOffset;
        NonNativeKeyboard.Instance.RepositionKeyboard(targetPosition);
    }


    /// <summary>
    /// Event handler for button state changes. This method is triggered when a button's state changes.
    /// It saves the state of the button and logs the button click event.
    /// </summary>
    /// <param name="context">The context of the event, which contains information about the event sender.</param>
    public void OnButtonStateChange(EventContext context)
    {
        // Switch case based on the name of the button
        switch (((GObject)context.sender).name)
        {
            case "debug":
                SaveButtonState("debug");
                Debug.Log("Button Debug Mode clicked.");
                break;
            case "controller_movement":
                SaveButtonState("controller_movement");
                Debug.Log("Button Controller Movement clicked.");
                break;
            case "singleplayer":
                // Fade out
                ApplySettingsInScene("singleplayer"); 
                DisableRayInteractor();
                xrUserInterface.tracerCore.GetComponent<Core>().InitTracer(); // Not sure if needed in singleplayer
                m_playerAvatar.gameObject.SetActive(true);
                vrHandRight.SetActive(true);
                vrHandLeft.SetActive(true);
                xrMenuInteractionManager.SetActive(false);
                m_playerCharacter.transform.position = new Vector3(0, m_playerCharacter.transform.position.y, m_playerCharacter.transform.position.z);  // Set the player character to the correct position
                xrUserInterface.minoGameManager.GetComponent<MinoGameManager>().UnloadScene("Menu");
                break;
            case "multiplayer":
                // Make sure all settings are set correctly (Check if IP address is set)
                // Fade out
                ApplySettingsInScene("multiplayer");  
                DisableRayInteractor();
                xrUserInterface.tracerCore.GetComponent<Core>().InitTracer(); // Start connection to server - tracer
                m_playerAvatar.gameObject.SetActive(true);
                vrHandRight.SetActive(true);
                vrHandLeft.SetActive(true);
                xrMenuInteractionManager.SetActive(false);
                m_playerCharacter.transform.position = new Vector3(0, m_playerCharacter.transform.position.y, m_playerCharacter.transform.position.z);  // Set the player character to the correct position
                xrUserInterface.minoGameManager.GetComponent<MinoGameManager>().UnloadScene("Menu");
                break;
            default:
                Debug.Log("Unknown button clicked.");
                break;
        }
    }
    #endregion
    #region Apply Settings
    /// <summary>
    /// Applies the settings in the scene based on the play mode.
    /// </summary>
    /// <param name="playMode">The play mode.</param>
    public void ApplySettingsInScene(string playMode)
    {
        // Get all the buttons and input fields
        int cnt = _mainView.numChildren;
        for (int i = 0; i < cnt; i++)
        {
            GObject obj = _mainView.GetChildAt(i);
            if (obj.group != null && obj.group.name == "Settings")
            {
                if (obj is GButton)
                {
                    HandleButton(obj, playMode);
                }
                else if (obj is GTextInput)
                {
                    HandleTextInput(obj, playMode);
                }
            }
        }
    }

    /// <summary>
    /// Handles the button settings.
    /// </summary>
    /// <param name="obj">The button object.</param>
    /// <param name="playMode">The play mode.</param>
    public void HandleButton(GObject obj, string playMode)
    {
        string settingName = obj.name;
        bool settingValue = GetButtonState(settingName);

        switch (settingName)
        {
            case "debug":
                // Enable Debug Mode
                MinoGameManager.Ingame_Debug = settingValue;
                m_playerCharacter.debugMode = settingValue;
                break;
            case "controller_movement":
                // Enable Controller Movement
                m_playerCharacter.SetControllerMovement(settingValue);
                break;
        }
    }

    /// <summary>
    /// Handles the text input settings.
    /// </summary>
    /// <param name="obj">The text input object.</param>
    /// <param name="playMode">The play mode.</param>
    public void HandleTextInput(GObject obj, string playMode)
    {
        string settingName = obj.name;
        string settingValue = GetTextInput(settingName);

        switch(settingName)
        {
            case "ip_address":
                if (playMode == "multiplayer")
                {
                    if(xrUserInterface.tracerCore != null)
                        xrUserInterface.tracerCore.GetComponent<Core>().serverIp = settingValue;
                } else
                {
                    xrUserInterface.tracerCore.GetComponent<Core>().serverIp = settingValue;
                }
                break;
        }
    }
    #endregion
    #region Helper Methods
    /// <summary>
    /// Disables the Ray Interactor of the player character in the Mino Game Manager.
    /// </summary>
    public void DisableRayInteractor()
    {
        if(xrUserInterface != null)
            xrUserInterface.minoGameManager.GetComponent<MinoGameManager>().m_playerCharacter.SetRayInteractorState(false);
    }

    public void EnableRayInteractor()
    {
        if (xrUserInterface != null)
            xrUserInterface.minoGameManager.GetComponent<MinoGameManager>().m_playerCharacter.SetRayInteractorState(true);
    }
    #endregion

}
