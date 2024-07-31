using FairyGUI;
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Collections;
using tracer;

public class XRUserInterfaceInteractor : MonoBehaviour
{
    public Transform leftControllerOrigin, rightControllerOrigin;
    public Camera StageCamera; // The seperate camera
    private List<InputDevice> devices = new List<InputDevice>();
    private string activeController = "right";
    private bool querying = false;

    void Start()
    {
        // Initialize controller in Start
        StartCoroutine(GetDevices(1.0f));
    }

    /// <summary>
    /// Initializes the device list after a specified delay and populates it with the connected XR input devices. Delay is needed because of HTC devices need some time to initialize.
    /// It specifically looks for left and right controllers based on their characteristics.
    /// </summary>
    /// <param name="delayTime">The delay in seconds before starting the device search.</param>
    /// <returns>A coroutine that waits for the specified delay before populating the device list.</returns>

    IEnumerator GetDevices(float delayTime)
    {
        if (querying)
            yield break;

        querying = true;
        yield return new WaitForSeconds(delayTime);

        devices = new List<InputDevice>();

        InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDeviceCharacteristics leftControllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;

        // Temporary lists to hold the devices before adding to the main list
        List<InputDevice> rightHandedDevices = new List<InputDevice>();
        List<InputDevice> leftHandedDevices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(rightControllerCharacteristics, rightHandedDevices);
        InputDevices.GetDevicesWithCharacteristics(leftControllerCharacteristics, leftHandedDevices);

        // Add the found devices to the main list
        devices.AddRange(rightHandedDevices);
        devices.AddRange(leftHandedDevices);

        querying = false;
    }

    void Update()
    {
        if (devices.Count == 0 && !querying)
            StartCoroutine(GetDevices(1.0f));


        foreach (var controller in devices)
        {
            controller.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);

            // Position updated every frame
            if (triggerValue <= 0.25f)
            {
                if (activeController == "right" && controller.characteristics.HasFlag(InputDeviceCharacteristics.Right))
                {
                    CheckPosition(controller);
                }
                else if (activeController == "left" && controller.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                {
                    CheckPosition(controller);
                }
            }

            // Button Press only updated on trigger
            if (triggerValue > 0.25f)
            {
                //TRIGGER
                activeController = controller.characteristics.HasFlag(InputDeviceCharacteristics.Right) ? "right" : "left";
                CheckHit(controller);
            }
        }
    }


    private void CheckHit(InputDevice controller, bool buttonDown = true)
    {
        RaycastHit hit;
        Vector3 pos, dir;

        if (controller.characteristics.HasFlag(InputDeviceCharacteristics.Right))
        {
            pos = rightControllerOrigin.position;
            dir = rightControllerOrigin.forward;
        }
        else
        {
            pos = leftControllerOrigin.position;
            dir = leftControllerOrigin.forward;
        }

        if (Physics.Raycast(pos, dir, out hit, Mathf.Infinity))
        {
            Stage.inst.SetCustomInput(ref hit, true);
        }
    }

    public void CheckPosition(InputDevice controller)
    {
        RaycastHit hit;
        Vector3 pos, dir;

        if (controller.characteristics.HasFlag(InputDeviceCharacteristics.Right))
        {
            pos = rightControllerOrigin.position;
            dir = rightControllerOrigin.forward;
        }
        else
        {
            pos = leftControllerOrigin.position;
            dir = leftControllerOrigin.forward;
        }

        if (Physics.Raycast(pos, dir, out hit, Mathf.Infinity))
        {
            Stage.inst.SetCustomInput(ref hit, false);
        }
    }
}
