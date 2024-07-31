using UnityEngine;
using Autohand;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using tracer;
using static RootMotion.Demos.CharacterThirdPerson;

public class MinoVibrationManager : SceneObjectMino //<- this updates the pos!!
{
    public static MinoVibrationManager singleton;

    // Network
    public Parameter<Vector4> m_vibrationState;
    private Vector4 vibrationState = new Vector4(-1, -1, -1, -1);

    // Vibration
    public Hand leftController;
    public Hand rightController;


    #region Utility
    public override void Awake()
    {
        base.Awake();
        m_vibrationState = new Parameter<Vector4>(new Vector4(), "vibrationState", this);
        m_vibrationState.hasChanged += UpdateVibrationState;

        position.hasChanged -= updatePosition;
        rotation.hasChanged -= updateRotation;
        scale.hasChanged -= updateScale;

    }

    void Start()
    {
        if (singleton && singleton != this)
            Destroy(this);
        else
            singleton = this;
    }

    #endregion
    #region NetworkCommunication
    // Currently not used
    public void UpdateVibrationState(object sender, Vector4 e)
    {
        Debug.Log("Change VibrationState to: " + e);
        vibrationState = e;

        emitHasChanged((AbstractParameter)sender);


        foreach (var localPlayerID in new[] { vibrationState.x, vibrationState.y, vibrationState.z, vibrationState.w})
        {
            // if minocharacter id is the given... vibrate
            if(localPlayerID == MinoGameManager.Instance.m_playerCharacter.id)
            {
                TriggerVibration(0.5f, true, true, 1f);
            }
        }
    }
    #endregion
    #region Vibration
    /// <summary>
    /// Triggers a vibration with specified intensity and duration on the controllers.
    /// </summary>
    /// <param name="intensity">The intensity of the vibration.</param>
    /// <param name="leftControllerBool">Whether vibration should be triggered on the left controller.</param>
    /// <param name="rightControllerBool">Whether vibration should be triggered on the right controller.</param>
    /// <param name="duration">The duration of the vibration.</param>
    public void TriggerVibration(float intensity, bool leftControllerBool, bool rightControllerBool, float duration)
    {
        if (leftControllerBool && leftController)
            leftController.PlayHapticVibration(duration, intensity);

        if (rightControllerBool && rightController)
            rightController.PlayHapticVibration(duration, intensity);
    }

    /// <summary>
    /// Triggers a vibration based on the provided AnimationCurve.
    /// </summary>
    /// <param name="intensityCurve">The AnimationCurve defining the intensity over time.</param>
    /// <param name="leftControllerBool">Whether vibration should be triggered on the left controller.</param>
    /// <param name="rightControllerBool">Whether vibration should be triggered on the right controller.</param>
    /// <param name="duration">Optional duration for the vibration. If set, the vibration will loop within this duration.</param>
    public void TriggerVibrationFromCurve(AnimationCurve intensityCurve, bool leftControllerBool, bool rightControllerBool, float duration = 0f)
    {
        if (intensityCurve == null)
        {
            Debug.LogWarning("IntensityCurve is null.");
            return;
        }

        // Start a coroutine to update vibration intensity
        StartCoroutine(UpdateVibration(intensityCurve, leftControllerBool, rightControllerBool, duration));
    }



    /// <summary>
    /// Updates vibration intensity over time based on a given AnimationCurve.
    /// </summary>
    /// <param name="intensityCurve">The AnimationCurve defining the intensity over time.</param>
    /// <param name="leftControllerBool">Whether vibration should be triggered on the left controller.</param>
    /// <param name="rightControllerBool">Whether vibration should be triggered on the right controller.</param>
    /// <param name="duration">Optional duration for the vibration. If set, the vibration will loop within this duration.</param>
    /// <returns>An IEnumerator for coroutine control.</returns>
    private IEnumerator UpdateVibration(AnimationCurve intensityCurve, bool leftControllerBool, bool rightControllerBool, float duration = 0f)
    {
        // Get the duration of the curve
        float curveDuration = intensityCurve.keys[intensityCurve.length - 1].time;

        // Time
        float initStartTime = Time.time;
        float currentStartTime = Time.time;
        float overallElapsedTime;
        float currentElapsedTime;

        do
        {
            currentElapsedTime = Time.time - currentStartTime;
            overallElapsedTime = Time.time - initStartTime;

            // Sample the intensity curve at current time
            float intensity = Mathf.Max(0, intensityCurve.Evaluate(currentElapsedTime));

            // Trigger vibration on left controller if enabled
            if (leftControllerBool && leftController)
                leftController.PlayHapticVibration(0.1f, intensity);

            // Trigger vibration on right controller if enabled
            if (rightControllerBool && rightController)
                rightController.PlayHapticVibration(0.1f, intensity);


            // Reset the parameters to start the curve from the beginning again
            // This will only be called when the duration parameter is set
            if (currentElapsedTime > curveDuration && duration > 0f)
            {
                currentElapsedTime = 0;
                currentStartTime = Time.time;
            }


            if (duration > 0f && overallElapsedTime > duration)
                break;

            // Wait for the next frame
            yield return null;

        } while (currentElapsedTime < curveDuration);
    }
    #endregion
}
