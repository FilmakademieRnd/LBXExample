using EZCameraShake;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using tracer;
using UnityEngine;
using UnityEngine.InputSystem;
using static MinoGameManager;

// Keybindings for switching between cameras
// 1-9: Switch to the corresponding camera
// 0: Randomly switch to cameras (automatic) - if only one player is nearest it will select pov
// Tab: Switch to the next camera
// Shift + Tab: Switch to the previous camera
// f free camera
// k Show Keybindings
public class MinoSpectator : MonoBehaviour, CharacterOverlay
{
    // Singleton-Instanz
    public static MinoSpectator instance;

    // Spectator General
    private bool isSpectator;
    private MinoSpectator m_spectator;
    private GameObject[] minoSpectatorCameras;
    private List<GameObject> combinedCameraList = new List<GameObject>(); 
    private short currentIndex = -1;
    private bool freeCamera = false;
    private bool automaticCameraSwitch = false;
    public bool automaticCameraSwitchCurrentlyInSceneTransition = false;
    [SerializeField] private GameObject keybindingsHUD;
    [SerializeField] private float automaticCameraSwitchTime = 5f;
    private bool automaticInPOV = false;

    // Flying
    [SerializeField] Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 3.0f;
    [SerializeField] private float flyingSpeed = 10.0f;
    [SerializeField] private float acceleration = 20.0f;
    [SerializeField] float movementSpeedMultiplier = 1.0f;

    private Vector2 look;
    private CharacterController controller;
    private Vector3 velocity;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction flyUpDownAction;

    // Player Overlay
    [Header("Damage Overlay | Settings")]
    public Transform headOverlay;
    public Transform headTarget;
    public Transform headEye;

    private int currentNetworkPlayerParentId = -1;

    private bool lookAtSomethingOnce = false;

    #region Utility
    private void Start()
    {
        isSpectator = MinoGameManager.Instance.IsSpectator();
        m_spectator = MinoGameManager.Instance.m_spectator;
        minoSpectatorCameras = GameObject.FindGameObjectsWithTag("SpectatorCamera");

        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        flyUpDownAction = playerInput.actions["FlyUpDown"];

        // Create camera list for spectator mode (order = keymap)
        CreateCameraList();
        SwitchSpecatorCamera(0);
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    private void Update(){
        if (!isSpectator)
            return;

        // if any key is pressed, switch to the corresponding camera
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchSpecatorCamera(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchSpecatorCamera(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SwitchSpecatorCamera(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SwitchSpecatorCamera(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            SwitchSpecatorCamera(5);
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            SwitchSpecatorCamera(6);
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            SwitchSpecatorCamera(7);
        else if (Input.GetKeyDown(KeyCode.Alpha8))
            SwitchSpecatorCamera(8);
        else if (Input.GetKeyDown(KeyCode.Alpha9))
            SwitchSpecatorCamera(9);
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            automaticCameraSwitch = !automaticCameraSwitch;

            if (automaticCameraSwitch)
                StartCoroutine(AutomaticCameraSwitch());
        }
        else if (Input.GetKeyDown(KeyCode.F))
            TrigggerFly();
        else if (Input.GetKeyDown(KeyCode.K))
            keybindingsHUD.gameObject.SetActive(!keybindingsHUD.gameObject.activeSelf);
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Tab))
            SwitchSpecatorCamera(currentIndex - 1);
        else if (Input.GetKeyDown(KeyCode.Tab))
            SwitchSpecatorCamera(currentIndex + 1);

        if (freeCamera)
        {
            UpdateLook();
            UpdateMovementFlying();
        }

        // Update keybindingsHUD
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.F))
            keybindingsHUD.gameObject.GetComponent<TextMeshProUGUI>().text = "1-9 - Switch Cameras | 0 - Automatic = " + automaticCameraSwitch + " | Tab - Next camera | Shift + Tab - Previous camera | f - Free camera = " + freeCamera + " | k - Show / hide keybindings";
    }

    public void CheckIfCameraIsInUnloadedScene(string sceneName)
    {
        Debug.Log("<<<<< Check if spectator camera is unLoaded Scene " + m_spectator.transform.parent.gameObject.scene.name + " " + sceneName);

        if (m_spectator.transform.parent != null && m_spectator.transform.parent.gameObject.scene.name == sceneName)
        {
            Debug.Log("Inside");

            automaticCameraSwitchCurrentlyInSceneTransition = true;
            SwitchSpecatorCamera(1);
            StartCoroutine(FinishedSceneTransitionCoroutine(sceneName));
        }
    }

    private IEnumerator FinishedSceneTransitionCoroutine(string sceneName)
    {
        while (!MinoGameManager.Instance.IsSceneUnloaded(sceneName))
        {
            yield return null; // Wait for the next frame before checking again
        }
        Debug.Log("Scene is unloaded");
        yield return new WaitForSeconds(7f);

        UpdateCameraList();
        automaticCameraSwitchCurrentlyInSceneTransition = false;
        StartCoroutine(AutomaticCameraSwitch());
    }

    #endregion
    #region Spectator
    // Create a list of all cameras in the scene
    private void CreateCameraList()
    {
        combinedCameraList.Clear();

        // Add each player to the camera list
        foreach (MinoGameManager.NetworkClientDataClass player in MinoGameManager.Instance.GetNetworkClientList()){
            if(!player.isClientSpectator)
                combinedCameraList.Add(player.GetHead().gameObject);
        }

        // Add cameras to the list
        foreach (var camera in minoSpectatorCameras){
            combinedCameraList.Add(camera.gameObject);
        }

    }

    // Updates the camera list
    public void UpdateCameraList()
    {
        minoSpectatorCameras = GameObject.FindGameObjectsWithTag("SpectatorCamera");
        CreateCameraList();
    }

    public void InitialLookAtPlayer(){
        //look at a player on start, if we are not already looked at one (at least once)
        if(!lookAtSomethingOnce){
            for(int x = 1; x<=4; x++){
                SwitchSpecatorCamera(x);
                if(lookAtSomethingOnce)
                    return;
            }
        }
    }

    public void RemoveFromPlayerIfWeDeleteHim(MinoNetworkCharacter character){
        MinoNetworkCharacter minoChar = GetComponentInParent<MinoNetworkCharacter>();
        if(minoChar == character){
            SwitchSpecatorCamera( (currentIndex + 1) % combinedCameraList.Count );
        }
    }

    // Switch to the camera with the given index
    private void SwitchSpecatorCamera(int index)
    {
        UpdateCameraList();
        freeCamera = false;

        if (index > 0 && index <= combinedCameraList.Count){
            index -= 1; // removing one cause we are starting from 0
            Debug.Log("Switch to Camera " + index);
            currentIndex = (short)index;
            m_spectator.transform.parent = combinedCameraList[index].transform;
            m_spectator.transform.localPosition = Vector3.zero;
            m_spectator.transform.localRotation = Quaternion.identity;

            cameraTransform.localRotation = Quaternion.identity;

            if(m_spectator.transform.GetComponentInParent<MinoNetworkCharacter>()){
                currentNetworkPlayerParentId = m_spectator.transform.GetComponentInParent<MinoNetworkCharacter>().id;
            }else{
                currentNetworkPlayerParentId = -1;
            }
            MinoGameManager.Instance.DisableNetworkHead(currentNetworkPlayerParentId);  //for pov spectator

            lookAtSomethingOnce = true;
        }
    }

    private IEnumerator AutomaticCameraSwitch()
    {
        while (automaticCameraSwitch && !automaticCameraSwitchCurrentlyInSceneTransition)
        {
            // Find nearest camera to the players if Camera is further then 10 units away switch to a player camera
            int closestCameraIndex = FindCameraIndexWithHighestPlayerCount();
            SwitchSpecatorCamera(closestCameraIndex);

            // Wait for the specified interval
            yield return new WaitForSeconds(automaticInPOV ? 15f : automaticCameraSwitchTime);
            automaticInPOV = false;
        }
    }

    private int FindCameraIndexWithHighestPlayerCount()
    {
        Dictionary<int, int> nearestPlayerCount = new Dictionary<int, int>(); // Key = cam index, Value = player count

        foreach (MinoGameManager.NetworkClientDataClass player in MinoGameManager.Instance.GetNetworkClientList()){
            if(player.isClientSpectator)
                continue;

            Dictionary<int, float> latestClosestCamera = new Dictionary<int, float>(); // Key = index, Value = distance 

            int i = 0; // Count the current camera index
            foreach (var camera in minoSpectatorCameras){
                if (player.GetHead() == null || camera == null)
                    return -1;

                float distanceToCamera = Vector3.Distance(player.GetHead().position, camera.transform.position);

                // Check if latestClosestCamera is empty or if this camera is closer than the previously stored one
                if (latestClosestCamera.Count == 0 || distanceToCamera < latestClosestCamera.First().Value)
                {
                    latestClosestCamera.Clear();
                    latestClosestCamera.Add(i, distanceToCamera);
                }

                i++;
            }

            // After finding the closest camera for the current player, update nearestPlayerCount
            if (latestClosestCamera.Count > 0)
            {
                int closestCameraIndex = latestClosestCamera.First().Key;

                // Update nearestPlayerCount for the closestCameraIndex
                if (nearestPlayerCount.ContainsKey(closestCameraIndex))
                {
                    nearestPlayerCount[closestCameraIndex]++;
                }
                else
                {
                    nearestPlayerCount.Add(closestCameraIndex, 1);
                }
            }
        }

        // Find the camera index with the highest player count
        int cameraIndexWithMaxPlayers = -1;
        int maxPlayerCount = -1;

        foreach (var kvp in nearestPlayerCount)
        {
            if (kvp.Value > maxPlayerCount)
            {
                maxPlayerCount = kvp.Value;
                cameraIndexWithMaxPlayers = kvp.Key;
            }
        }

        int closestCamera = MinoGameManager.Instance.GetNetworkClientList().Count + cameraIndexWithMaxPlayers; // Get camera index in combinedCameraList

        if(maxPlayerCount == 1) // If only one player is near camera return random camera from player cameras
        {
            automaticInPOV = true;
            return Random.Range(0, MinoGameManager.Instance.GetNetworkClientList().Count); // Return random player camera index
        } 

        return closestCamera;

    }

    #endregion
    #region Fly
    public void TrigggerFly()
    {
        freeCamera = !freeCamera;

        if (freeCamera)
        {
            currentIndex = -1;
            m_spectator.transform.parent = null; // unparent m_spectator from current camera
            currentNetworkPlayerParentId = -1;
            MinoGameManager.Instance.DisableNetworkHead(currentNetworkPlayerParentId);

        }
    }
    private Vector3 GetMovementInput()
    {
        var moveInput = moveAction.ReadValue<Vector2>();
        var flyingUpDownInput = flyUpDownAction.ReadValue<float>();
        var input = new Vector3();

        var referenceTransform = cameraTransform;
        input += referenceTransform.forward * moveInput.y;
        input += referenceTransform.right * moveInput.x;
        input += referenceTransform.up * flyingUpDownInput;
        input = Vector3.ClampMagnitude(input, 1);
        input *= flyingSpeed * movementSpeedMultiplier;

        return input;
    }

    private void UpdateMovementFlying()
    {
        var input = GetMovementInput();

        var factor = acceleration * Time.deltaTime;
        velocity = Vector3.Lerp(velocity, input, factor);

        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateLook()
    {
        var lookInput = lookAction.ReadValue<Vector2>();

        look.x += lookInput.x * mouseSensitivity;
        look.y += lookInput.y * mouseSensitivity;

        look.y = Mathf.Clamp(look.y, -89, 89);

        cameraTransform.localRotation = Quaternion.Euler(-look.y, 0, 0);
        transform.localRotation = Quaternion.Euler(0, look.x, 0);
    }
    #endregion

    #region Health
    public void TakeDamage(int playerID){
        Debug.Log("TakeDamage (Spect): currentNetworkPlayerParentId: "+currentNetworkPlayerParentId+", received player id: "+playerID);
        if(currentNetworkPlayerParentId < 0)
            return;
        
        bool isAtCurrentPlayer = currentNetworkPlayerParentId == playerID;
        if(isAtCurrentPlayer)
            ScreenOverlays();
    }
    #endregion
    #region Screen Overlays
    public void ActivateNormalFade()
    {
        headEye.GetComponent<Animator>().SetTrigger("Fade");
    }

    public void FadeHeadToBlack(bool isBlack)
    {
        headEye.GetComponent<Renderer>().material.SetInt("_Activate_Texture", 0);
        headEye.GetComponent<Animator>().SetBool("isBlack", isBlack);
    }

    public void FadeHeadChangeColor(Color color)
    {
        Debug.Log("Change Color" + color);
        headEye.GetComponent<Renderer>().material.SetColor("_VignetteColor", color);
    }

    /* Screen Overlay Effects
     * 
     * Blink
     * Camera shake -> Shakes the camera on hit
     * 
     */
    public void ScreenOverlays()
    {
        CameraShaker.Instance.ShakeOnce(3f, 7f, 0.1f, 3f);
        Invoke("InitFadeOut", 0.4f);
    }

    #endregion
    #region Blink Overlay
    public void InitFadeOut()
    {
        FadeHeadBlinking();
        headOverlay.GetComponent<Animator>().SetTrigger("BloodVignetteMid");
    }
    public void FadeHeadBlinkClose()
    {
        headEye.GetComponent<Animator>().SetTrigger("BlinkClose");
        headEye.GetComponent<Animator>().SetBool("isBlack", true);
    }
    public void FadeHeadBlinkOpen()
    {
        headEye.GetComponent<Animator>().SetBool("isBlack", false);
    }
    public void FadeHeadBlinking()
    {
        headEye.GetComponent<Animator>().SetTrigger("Blinking");
    }
    #endregion

}
