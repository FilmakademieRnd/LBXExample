using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using tracer;

public class MinoPlayerCharDebugMovement : MonoBehaviour{
    
    private Transform playerCamToMove;
    private MinoPlayerCharacter minoPc;

    private List<XRController> allControllers;

    private TextMesh debugTm;
    private string debugFirstLine;

    void Start(){
        allControllers = new List<XRController>();
        allControllers.AddRange(GetComponentsInChildren<XRController>());

        minoPc = GetComponent<MinoPlayerCharacter>();
        Transform localPlayerCamTr = GetComponentInChildren<Camera>().transform;
        playerCamToMove = minoPc.head;

        //CREATE DEBUG TEXT
        GameObject debugTMGo = new GameObject("DebugTM");
        debugTMGo.GetComponent<Transform>().parent = playerCamToMove;
        debugTMGo.GetComponent<Transform>().localPosition = localPlayerCamTr.forward * 3f + localPlayerCamTr.up * 2f;
        debugTMGo.GetComponent<Transform>().localScale *= 0.075f;
        debugTm = debugTMGo.AddComponent<TextMesh>();
        debugTm.fontSize = 20;
        debugTm.alignment = TextAlignment.Center;
        debugTm.anchor = TextAnchor.MiddleCenter;
        debugTm.gameObject.SetActive(false);        //DEACTIVATED
        debugFirstLine = "Found "+allControllers.Count+" local controllers in model.\n";
    }

    void Update(){
        debugTm.text = debugFirstLine;
        Vector3 moveVecWithoutY;

        Vector2 currentMoveInput = ReadHandDeviceInputXZ(1);    //0 most likely left controller, 1 right controller
        if(currentMoveInput != Vector2.zero){
            moveVecWithoutY = (playerCamToMove.right * currentMoveInput.x + playerCamToMove.forward * currentMoveInput.y) * Time.deltaTime * 1f;
            moveVecWithoutY.y = 0f;
            //playerCamToMove.position += moveVecWithoutY;
            minoPc.DebugMoveRoot(moveVecWithoutY);
            debugTm.text += "Moved "+playerCamToMove.name+" of Player Horizontal\n";
        }
        Vector2 currentPoseInput = ReadHandDeviceInputXZ(0);
        if(currentPoseInput != Vector2.zero){
            //playerCamToMove.position += Vector3.up * currentPoseInput.y * Time.deltaTime * 50f;
            minoPc.DebugMoveRoot(Vector3.up * currentPoseInput.y * Time.deltaTime * 1f);
            debugTm.text += "Moved "+playerCamToMove.name+" of Player Vertical\n";
        }

        //MOVE VIA KEYBOARD
        moveVecWithoutY = (playerCamToMove.right * Input.GetAxis("Horizontal") + playerCamToMove.forward * Input.GetAxis("Vertical")) * Time.deltaTime * 2f;
        moveVecWithoutY.y = 0f;
        //playerCamToMove.position += moveVecWithoutY;
        minoPc.DebugMoveRoot(moveVecWithoutY);
        //playerCamToMove.position += (Vector3.up * (Input.GetKey(KeyCode.Q) ? -1f : 0f) + Vector3.up * (Input.GetKey(KeyCode.E) ? 1f : 0f)) * Time.deltaTime * 1f;
        minoPc.DebugMoveRoot((Vector3.up * (Input.GetKey(KeyCode.Q) ? -1f : 0f) + Vector3.up * (Input.GetKey(KeyCode.E) ? 1f : 0f)) * Time.deltaTime * 1f);
    
        //RotateCam();
    }

    public void RotateCam() {
		//Cursor.lockState = CursorLockMode.Locked;
        float x = playerCamToMove.eulerAngles.x;
        float y = playerCamToMove.eulerAngles.y;

        x += Input.GetAxis("Mouse X") * 3f;
        y = ClampAngle(y - Input.GetAxis("Mouse Y") * 3f, -89f, 89f);

        // Rotation
        playerCamToMove.rotation = Quaternion.AngleAxis(x, Vector3.up) * Quaternion.AngleAxis(y, Vector3.right);

    }

    private float ClampAngle (float angle, float min, float max) {
			if (angle < -360) angle += 360;
			if (angle > 360) angle -= 360;
			return Mathf.Clamp (angle, min, max);
		}



    //**************** FUNCTIONS AND DOCS FROM DeviceBasedContinuousMoveProvider

    /// <inheritdoc />
    private Vector2 ReadHandDeviceInputXZ(int controllerIndex){
        if (allControllers.Count == 0 || controllerIndex >= allControllers.Count)
            return Vector2.zero;

        debugTm.text += "Checking for controller input at "+controllerIndex+"\n";
        // Accumulate all the controller inputs
        Vector2 input = Vector2.zero;
        var feature = CommonUsages.primary2DAxis; //k_Vec2UsageList[(int)m_InputBinding];
        var controller = allControllers[controllerIndex] as XRController;
        if (controller != null && controller.inputDevice.TryGetFeatureValue(feature, out var controllerInput)){
            debugTm.text += "Got Value 'CommonUsages.primary2DAxis': "+controllerInput+"\n";
            input += controllerInput; //GetDeadzoneAdjustedValue(controllerInput);
        }

        return input;
    }

    /// <summary>
    /// Gets value adjusted based on deadzone thresholds defined in <see cref="deadzoneMin"/> and <see cref="deadzoneMax"/>.
    /// </summary>
    /// <param name="value">The value to be adjusted.</param>
    /// <returns>Returns adjusted 2D vector.</returns>
    private Vector2 GetDeadzoneAdjustedValue(Vector2 value){
        var magnitude = value.magnitude;
        var newMagnitude = GetDeadzoneAdjustedValue(magnitude);
        if (Mathf.Approximately(newMagnitude, 0f))
            value = Vector2.zero;
        else
            value *= newMagnitude / magnitude;
        return value;
    }

    public float m_DeadzoneMin = 0.125f;
    public float m_DeadzoneMax = 0.925f;

    /// <summary>
    /// Gets value adjusted based on deadzone thresholds defined in <see cref="deadzoneMin"/> and <see cref="deadzoneMax"/>.
    /// </summary>
    /// <param name="value">The value to be adjusted.</param>
    /// <returns>Returns adjusted value.</returns>
    private float GetDeadzoneAdjustedValue(float value){
        var min = m_DeadzoneMin;
        var max = m_DeadzoneMax;

        var absValue = Mathf.Abs(value);
        if (absValue < min)
            return 0f;
        if (absValue > max)
            return Mathf.Sign(value);

        return Mathf.Sign(value) * ((absValue - min) / (max - min));
    }
}
