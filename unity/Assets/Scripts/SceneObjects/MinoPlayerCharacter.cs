using System.Collections.Generic;
using Autohand;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using EZCameraShake;
using static MinoGameManager;

namespace tracer
{
    public class MinoPlayerCharacter : MinoCharacter, CharacterOverlay
    {
        // Debug
        [SerializeField] private DeviceBasedContinuousMoveProvider _locomotion;
        public bool debugMode = false;
        private bool _showMenu = false;
        public GameObject debugMenu;

        // Controller
        public bool controllerMovement = false;

        // Tracer
        [SerializeField]
        protected Collider m_rigidBodyCollider;
        [SerializeField]
        protected Rigidbody m_rigidBody;

        // Meshes
        [Header("Mesh Renderer")]
        [SerializeField] private SkinnedMeshRenderer m_ArmsMesh;
        [SerializeField] private SkinnedMeshRenderer m_ShacklesMesh;


        // Calibration
        public float playerHeight = 1.5f;
        
        // AutoSetup
        //[SerializeField] public Camera playerCam;
        private AutoHandVRIK _autoHandVrik;

        [Header("Manual Setup")]
        public Transform headTarget;
        public Transform headEye;
        [HideInInspector] public PlayerCalibration playerCalibration;

        // Boss Fight
        public GameObject handPlayer;

        public bool holdingWeapon = false;

        // Player Overlay
        [Header("Damage Overlay | Settings")]
        public int playerHealth = 100;
        [HideInInspector] public int playerMaxHealth;
        private float normalizedHealth;
        public AnimationCurve[] vibrationCurve = new AnimationCurve[4];
        public Transform headOverlay;

        // Segments
        public bool segmentsActive = false;
        public MinoSegment lastSegment;
        public List<MinoSegment> segments = new List<MinoSegment>();
        public bool ghostModeOn = false;

        //HandReferences

        [SerializeField] private Hand leftVRHand;
        [SerializeField] private Hand rightVRHand;
        [SerializeField] public GameObject leftVRHandRayInteractor;
        [SerializeField] public GameObject rightVRHandRayInteractor;
        [SerializeField] public GameObject xrMenuInteractionManager;



        // Collision GhostMode References
        public Collider[] playerColliders;

        // Bloody Hands
        [SerializeField] private SkinnedMeshRenderer playerRenderer;
        [SerializeField] private Texture bloodyHandsTexture;
        [SerializeField] private Material playerMaterial;
        private bool bloodyHands = false;


        public override void Awake()
        {
            base.Awake();
            _locomotion.enabled = controllerMovement;

            m_playerColorMateriels.Add(tr.FindDeepChild("Shackles").GetComponent<SkinnedMeshRenderer>().material);

            // Player Material
            playerMaterial = Instantiate(playerMaterial);
            playerRenderer.SetMaterials(new List<Material> { playerMaterial });

            //JITTER HOTFIX
            InitToUseParentingPosUpdates();
            position.hasChanged -= updatePosition;

        }
        private void Start()
        {
            //Set Playercam
            //playerCam = Camera.main;
            playerCalibration = GetComponent<PlayerCalibration>();

            //Make the Mesh invisible on start
            //if (!debugMode)
            //{
            //    m_ArmsMesh.enabled = false;
            //    m_ShacklesMesh.enabled = false;
            //}

            //Set Components for Calibration
            _autoHandVrik = GetComponentInChildren<AutoHandVRIK>();
            
            // PLAYER SCRIPT
            InitialHealth();
            //DamageOverlayStart();
            //currentPlayerHealth = maxHealth;

            //Add controls via stick and keyboard
            if(Debug.isDebugBuild || MinoGameManager.Ingame_Debug){
                gameObject.AddComponent<MinoPlayerCharDebugMovement>();
            }

            #if UNITY_EDITOR
            transform.GetChild(0).localPosition += Vector3.up * 1.7f; 
            #endif
        }

        protected override void Update(){
            //OUR AVATAR USES LOCAL POSITION TO OUR PLAYER-CONTROLLER (this!)
            CheckAvatarHead(); 
            CheckAvatarHandLeft();
            CheckAvatarHandRight();

            leftGrabValue = new Vector2(_autoHandVrik.leftHand.fingers[0].bendOffset,_autoHandVrik.leftHand.fingers[1].bendOffset);
            if (leftGrabValue != m_leftHandGrabValue.value)
                m_leftHandGrabValue.value = leftGrabValue;

            rightGrabValue = new Vector2(_autoHandVrik.rightHand.fingers[0].bendOffset,_autoHandVrik.rightHand.fingers[1].bendOffset);
            if (rightGrabValue != m_rightHandGrabValue.value)
                m_rightHandGrabValue.value = rightGrabValue;

            
            //NEW MOVEMENT DEBUG SCRIPT
            if(debugRootStickMovement != Vector3.zero){
                tr.position += debugRootStickMovement;
                debugRootStickMovement = Vector3.zero;
            }

            //base.Update();  //-- should always call --> EmitNonLockedPosData();
            EmitNonLockedPosData();

        }

        private void CheckAvatarHead(){
            //our player will never be locked, therefore no need to check this here
            if (headTransform.localPosition != m_headPosition.value){
                m_headPosition.setValue(headTransform.localPosition, false);
                emitHasChanged(m_headPosition);
            }
            if (headTransform.localRotation != m_headRotation.value){
                m_headRotation.setValue(headTransform.localRotation, false);
                emitHasChanged(m_headRotation);
            }
        }
        private void CheckAvatarHandLeft(){
            if (leftHandTransform.localPosition != m_leftHandPosition.value){
                m_leftHandPosition.setValue(leftHandTransform.localPosition, false);
                emitHasChanged(m_leftHandPosition);
            }
            if (leftHandTransform.localRotation != m_leftHandRotation.value){
                m_leftHandRotation.setValue(leftHandTransform.localRotation, false);
                emitHasChanged(m_leftHandRotation);
            }
        }
        private void CheckAvatarHandRight(){
            if (rightHandTransform.localPosition != m_rightHandPosition.value){
                m_rightHandPosition.setValue(rightHandTransform.localPosition, false);
                emitHasChanged(m_rightHandPosition);
            }
            if (rightHandTransform.localRotation != m_rightHandRotation.value){
                m_rightHandRotation.setValue(rightHandTransform.localRotation, false);
                emitHasChanged(m_rightHandRotation);
            }
        }

        private Vector3 debugRootStickMovement = Vector3.zero;
        public void DebugMoveRoot(Vector3 addFromInput){
            debugRootStickMovement += addFromInput;
        }

        //Finish Calibration, put Arms in Chains, show Shackles, set Readiness level
        public void SetPlayerReady(bool value)
        {
            isReady = value;
            m_gameReady.value = value;
            Debug.Log("Set Player Ready: " + value);
        }

        public void ActivateNormalFade(){
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
        public void UpdateRigScaleValue(float value)
        {
            m_rigScale.value = value;
        }

        public void UpdateArmScaleValue(float value)
        {
            m_armScale.value = value;
        }

        public void SetMeshVisibility(bool value)
        {
            m_ArmsMesh.enabled = value;
            m_ShacklesMesh.enabled = value;

        }

        public void SetHoldingWeapon(bool value)
        {
            holdingWeapon = value;
            core.syncSetOnce = false;
        }

        public void ForceRelease()
        {
            leftVRHand.ForceReleaseGrab();
            rightVRHand.ForceReleaseGrab();
        }

        public void SwitchColliderEnabled(bool val)
        {
            //if (playerColliders[0].enabled != val) return;
            
            foreach (Collider col in playerColliders)
            {
                col.enabled = val;
            }
            Debug.Log("Flip Colliders");
            
            
        }

        public void SetRayInteractorState(bool state)
        {
            if(state)
            {
                leftVRHandRayInteractor.SetActive(true);
                rightVRHandRayInteractor.SetActive(true);
                xrMenuInteractionManager.SetActive(true);
            }
            else
            {
                leftVRHandRayInteractor.SetActive(false);
                rightVRHandRayInteractor.SetActive(false);
                xrMenuInteractionManager.SetActive(false);
            }

        }

        public void SetControllerMovement(bool state)
        {
            if (state)
            {
                _locomotion.enabled = true;
            } else
            {
                _locomotion.enabled = false;
            }

        }

        public Hand GetLeftHand(){ return leftVRHand; }
        public Hand GetRightHand(){ return rightVRHand; }

        public void SetBloodyHands()
        {
            if (!bloodyHands)
            {
                playerMaterial.SetTexture("_BaseMap", bloodyHandsTexture);
                playerMaterial.SetTexture("_EmissionMap", bloodyHandsTexture);
                bloodyHands = true;
            }
        }

        #region Health
        public void InitialHealth()
        {
            playerMaxHealth = playerHealth;
        }

        public void TakeDamage(int damage)
        {
            playerHealth -= damage;
            ScreenOverlays();
        }
        #endregion
        #region Screen Overlays
        /* Screen Overlay Effects
         * 
         * Blink
         * Camera shake -> Shakes the camera on hit
         * 
         */
        public void ScreenOverlays()
        {
            normalizedHealth = (float)playerHealth / (float)playerMaxHealth;

            if (normalizedHealth <= 1 && normalizedHealth > 0.75f) { // 100% -> 75% Health
                CameraShaker.Instance.ShakeOnce(3f, 7f, 0.1f, 3f); // Camera shake
                Invoke("InitFadeOut", 0.4f); // Blackout
                MinoVibrationManager.singleton.TriggerVibrationFromCurve(vibrationCurve[0], true, true); // Haptics
            }
            else if (normalizedHealth <= 0.75f && normalizedHealth > 0.5f) { // 75% -> 50% Health
                CameraShaker.Instance.ShakeOnce(3f, 7f, 0.1f, 3f);
                Invoke("InitFadeOut", 0.4f);
                MinoVibrationManager.singleton.TriggerVibrationFromCurve(vibrationCurve[1], true, true);
            }
            else if (normalizedHealth <= 0.5f && normalizedHealth > 0.25f) { // 50% -> 25% Health
                CameraShaker.Instance.ShakeOnce(3f, 7f, 0.1f, 3f);
                Invoke("InitFadeOut", 0.4f);
                MinoVibrationManager.singleton.TriggerVibrationFromCurve(vibrationCurve[2], true, true);
            }
            else if (normalizedHealth <= 0.25f && normalizedHealth > 0.1f) { // 25% -> 10% Health
                CameraShaker.Instance.ShakeOnce(3f, 7f, 0.1f, 3f);
                Invoke("InitFadeOut", 0.4f);
                MinoVibrationManager.singleton.TriggerVibrationFromCurve(vibrationCurve[2], true, true);
            }
            else if (normalizedHealth <= 0.1f) { // 10% -> -x% Health
                CameraShaker.Instance.ShakeOnce(3f, 7f, 0.1f, 3f);
                Invoke("InitFadeOut", 0.4f);
                MinoVibrationManager.singleton.TriggerVibrationFromCurve(vibrationCurve[3], true, true, 5f); // Haptics
            }
        }

        #endregion
        #region Blink Overlay
        public void InitFadeOut()
        {
            FadeHeadBlinking();

            //FadeHeadBlinkClose();
            //Invoke("FadeHeadBlinkOpen", 0.25f);
            //Invoke("FadeHeadBlinking", 1.3f);


            if (normalizedHealth <= 1 && normalizedHealth > 0.75f)
            { // 100% -> 75% Health
                headOverlay.GetComponent<Animator>().SetTrigger("BloodVignetteLow");
            }
            else if (normalizedHealth <= 0.75f && normalizedHealth > 0.5f)
            { // 75% -> 50% Health
                headOverlay.GetComponent<Animator>().SetTrigger("BloodVignetteLow");

            }
            else if (normalizedHealth <= 0.5f && normalizedHealth > 0.25f)
            { // 50% -> 25% Health
                headOverlay.GetComponent<Animator>().SetTrigger("BloodVignetteMid");

            }
            else if (normalizedHealth <= 0.25f && normalizedHealth > 0.1f)
            { // 25% -> 10% Health
                headOverlay.GetComponent<Animator>().SetTrigger("BloodVignetteMid");

            }
            else if (normalizedHealth <= 0.1f)
            { // 10% -> -x% Health
                headOverlay.GetComponent<Animator>().SetTrigger("BloodVignetteStrong");

            }

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
}