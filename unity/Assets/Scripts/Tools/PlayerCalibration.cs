using System;
using System.Reflection;
using tracer;
using UnityEngine;
using Autohand;
using RootMotion.FinalIK;

public class PlayerCalibration : MonoBehaviour
{
    private MinoPlayerCharacter _player;
    
    [Header("Calibration")]
    // AutoSetup
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float armScaleMultiplier = 1.1f;
    [SerializeField] public Camera playerCam;
        
    [SerializeField] private float m_measureHeight;
    public Transform m_leftHandTarget;
    private AutoHandVRIK _autoHandVrik;

    [SerializeField] private VRIK _invisibleIK;
    [SerializeField] private VRIK _visibleIK;

    [SerializeField] private Transform _hiddenIKLeft;
    private Transform _hiddenLeftUpperArm;
    private Transform _hiddenRightUpperArm;
    private Transform m_upperArmLeft;
    private Transform m_upperArmRight;
    private Transform m_handLeft;
    private Transform m_handRight;
    public Transform ikLeft;
    
    [Header("Manual Setup")]
    [SerializeField] private Transform headTarget;
    private float height;
    private float _bodyHeight = 1.637309f;
    
    [Header("Calibration Process")]
    [HideInInspector] public bool measuring = false;
    [HideInInspector] public bool calibrateGrow = false;
    [HideInInspector] public bool calibrateShrink = false;
    [HideInInspector] public bool calibrateFeet;
    [HideInInspector] public bool calibrationReady = false;

    private readonly Vector3 _step = new Vector3(0.005f, 0.005f, 0.005f);
    
    [SerializeField] private Transform leftToe;
    [SerializeField] private float footAngle = 14.5f;
    
    void Awake()
    {
        _player = GetComponent<MinoPlayerCharacter>();
    }

    private void Start()
    {
        //Set Playercam
        playerCam = Camera.main;
        //Set Components for Calibration
        _autoHandVrik = GetComponentInChildren<AutoHandVRIK>();
        _invisibleIK = GetInvisibleIK();
        _visibleIK = GetVisibleIK();
        
        FindTarget();
        _hiddenIKLeft = _invisibleIK.GetComponent<AutoHandVRIK>().leftIKTarget;
        FindHiddenArmTransforms();
    }

    void Update()
    {
        if (calibrationReady) return;

        if (calibrateGrow)
        {
            if (height > m_measureHeight &&
                _invisibleIK.transform.localScale.x < maxScale) //bodyheight measuredheight switch
            {
                Debug.Log(_invisibleIK.transform.localScale.x + ", " + maxScale);
                _invisibleIK.transform.localScale += _step;
                _visibleIK.transform.localScale += _step;
            }
            else
            {
                _bodyHeight = m_measureHeight;
                Debug.Log("Stop Grow");
                calibrateGrow = false;
                calibrateFeet = true;
            }
        }

        if (calibrateShrink)
        {
            if (_bodyHeight > height && _invisibleIK.transform.localScale.x > minScale)
            {
                Debug.Log(_invisibleIK.transform.localScale.x + ", " + maxScale);
                _invisibleIK.transform.localScale -= _step;
                _visibleIK.transform.localScale -= _step;
                _bodyHeight -= 0.01f;
            }
            else
            {
                calibrateShrink = false;
                calibrateFeet = true;
            }
        }

        if (calibrateFeet)
        {
            if (leftToe.localRotation.x * Mathf.Rad2Deg > footAngle && _invisibleIK.transform.localScale.x < maxScale)
            {
                _invisibleIK.transform.localScale += _step * 0.5f;
                _visibleIK.transform.localScale += _step * 0.5f;
            }
            else
            {
                calibrateFeet = false;
                calibrationReady = true;
                Debug.Log("Calibration Finished");
                float rigscale = _visibleIK.transform.localScale.x;
                _player.UpdateRigScaleValue(rigscale);

                // scale Arms
                m_upperArmLeft.localScale = new Vector3(rigscale * armScaleMultiplier, rigscale * armScaleMultiplier,
                    rigscale * armScaleMultiplier);
                _hiddenLeftUpperArm.localScale = m_upperArmLeft.localScale;

                m_upperArmRight.localScale = m_upperArmLeft.localScale;
                _hiddenRightUpperArm.localScale = _hiddenLeftUpperArm.localScale;
                _player.UpdateArmScaleValue(m_upperArmLeft.localScale.x);

                MinoGameManager.Instance.calibrationVolume.CalibrationDone();

                _player.playerHeight = height;
            }
        }
    }
    
    public void StartCalibration()
    {
        Debug.Log("Start Calibration");
        _player.SetMeshVisibility(false);
        height = playerCam.transform.position.y;
        if (measuring)
        {
            //if (m_measureHeight >= bodyHeight) // height instead of measured height
            if (height >= _bodyHeight)
            {
                calibrateGrow = true;
            }
            else
            {
                calibrateShrink = true;
            }
        }
    }

    public void ResetCalibration()
    {
        _visibleIK.transform.localScale = new Vector3(1, 1, 1);
        _invisibleIK.transform.localScale = new Vector3(1, 1, 1);
        
        m_upperArmLeft.localScale = new Vector3(1, 1, 1);
        m_upperArmRight.localScale = new Vector3(1, 1, 1);
        _hiddenLeftUpperArm.localScale = new Vector3(1, 1, 1);
        _hiddenRightUpperArm.localScale = new Vector3(1, 1, 1);
        _player.UpdateArmScaleValue(1);
        
        calibrationReady = false;
    }

    public void GetHeightReference()
    {
        Vector3 tmp = playerCam.transform.position;
        m_measureHeight = tmp.y;
    }
    
    //Reflection Methods to get private VRIK Rigs from Autohand
    private VRIK GetVisibleIK()
    {
        // Use reflection to access the private visibleIK field
        Type autoHandVrikType = typeof(AutoHandVRIK);
        FieldInfo visibleIKField =
            autoHandVrikType.GetField("visibleIK", BindingFlags.NonPublic | BindingFlags.Instance);


        VRIK visibleIK = (VRIK)visibleIKField.GetValue(_autoHandVrik);

        return visibleIK;
    }

    private VRIK GetInvisibleIK()
    {
        // Use reflection to access the private invisibleIK field
        Type autoHandVrikType = typeof(AutoHandVRIK);
        FieldInfo invisibleIKField =
            autoHandVrikType.GetField("invisibleIK", BindingFlags.NonPublic | BindingFlags.Instance);

        VRIK invisibleIK = (VRIK)invisibleIKField.GetValue(_autoHandVrik);

        return invisibleIK;
    }
    
    private void FindTarget()
    {
        // [REVIEW]
        ikLeft = FindInChildren(transform,"(L) Follow Offset Hand");
        headTarget = FindInChildren(transform,"HeadTarget");
        m_upperArmLeft = FindInChildren(_visibleIK.transform,"CC_Base_L_Clavicle");
        m_upperArmRight = FindInChildren(_visibleIK.transform,"CC_Base_R_Clavicle");
        m_leftHandTarget = FindInChildren(m_upperArmLeft.transform, "leftHandTarget");
    }
    
    //! 
    //! Find Transforms in Invisible Rig
    //!
    private void FindHiddenArmTransforms()
    {
        _hiddenLeftUpperArm = FindInChildren(_invisibleIK.transform, "CC_Base_L_Clavicle");
        _hiddenRightUpperArm = FindInChildren(_invisibleIK.transform, "CC_Base_R_Clavicle");
    }
    
    //! 
    //! static method to find transforms in all children, not only the first layer 
    //!
    private static Transform FindInChildren(Transform transform, string name)
    {
        foreach (Transform t in transform.GetComponentsInChildren<Transform>())
        {
            if (t.name == name)
                return t;
        }
        return null;
    }
}
