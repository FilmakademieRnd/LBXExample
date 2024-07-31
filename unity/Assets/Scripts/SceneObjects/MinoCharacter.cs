using System.Collections.Generic;
using tracer;
using Unity.VisualScripting;
using UnityEngine;

namespace tracer
{
    public class MinoCharacter : SceneObjectMino
    {


        //!
        //! The head position of the MinoCharacter
        //!
        protected Parameter<Vector3> m_headPosition;
        //!
        //! The head rotation of the MinoCharacter
        //!
        protected Parameter<Quaternion> m_headRotation;
        //!
        //! The left hand position of the MinoCharacter
        //!
        protected Parameter<Vector3> m_leftHandPosition;
        //!
        //! The left hand rotation of the MinoCharacter
        //!
        protected Parameter<Quaternion> m_leftHandRotation;
        //!
        //! The right hand position of the MinoCharacter
        //!
        protected Parameter<Vector3> m_rightHandPosition;
        //!
        //! The right hand rotation of the MinoCharacter
        //!
        protected Parameter<Quaternion> m_rightHandRotation;
        //!
        //! The left hand grab value of the MinoCharacter
        //!
        protected Parameter<Vector2> m_leftHandGrabValue;
        //!
        //! The right hand grab value of the MinoCharacter
        //!
        protected Parameter<Vector2> m_rightHandGrabValue;
        //!
        //! The tracking offset of the MinoCharacter
        //!
        protected Parameter<Vector3> m_trackingOffset;

        //!
        //! The game readiness of the MinoCharacter
        //!
        protected Parameter<bool> m_gameReady;
        //!
        //! The rig scale of the MinoCharacter
        //!
        protected Parameter<float> m_rigScale;
        //!
        //! Thearm scale of the MinoCharacter
        //!
        protected Parameter<float> m_armScale;

        //!
        //! The material containing the player color
        //!
        protected List<Material> m_playerColorMateriels = new List<Material>();

        //GameState
        public byte playerNumber
        {
            get; protected set;
        } = 1;

        public bool setupDone = false;

        [SerializeField]
        protected Transform headTransform;

        public List<Transform> m_collidingObj;
        
        public Transform head
        {
            get => headTransform;
        }

        [SerializeField]
        protected Transform leftHandTransform;

        [SerializeField]
        protected Transform rightHandTransform;

        [SerializeField]
        protected Transform trackingOffset;

        [SerializeField] protected Vector2 leftGrabValue;
        [SerializeField] protected Vector2 rightGrabValue;

        private Color detailColor = Color.white;  //removed network send of such data, since its set via our playerNr

        public bool isReady = false;

        public enum animationStateType { NONE, GRABBING }
        protected Parameter<int> m_leftHandAnimationState;
        protected Parameter<int> m_righttHandAnimationState;

        [Header("Player | Settings")]
        public int maxHealth = 100;
        public int currentPlayerHealth;
        //[HideInInspector] public Collision collision;
        public override void Awake()
        {
            base.Awake();
            
            m_collidingObj = new List<Transform>();

            //HOTFIX
            //position.hasChanged -= updatePosition;
            //scale.hasChanged -= updateScale;

            m_headPosition = new Parameter<Vector3>(tr.localPosition, "m_headPosition", this);
            m_headPosition.hasChanged += updateHeadPosition;
            m_headRotation = new Parameter<Quaternion>(tr.localRotation, "m_headRotation", this);
            m_headRotation.hasChanged += updateHeadRotation;

            m_leftHandPosition = new Parameter<Vector3>(tr.localPosition, "m_leftHandPosition", this);
            m_leftHandPosition.hasChanged += updateLeftHandPosition;
            m_leftHandRotation = new Parameter<Quaternion>(tr.localRotation, "m_leftHandRotation", this);
            m_leftHandRotation.hasChanged += updateLeftHandRotation;
            m_leftHandAnimationState = new Parameter<int>(0, "m_leftHandAnimationState", this);

            m_rightHandPosition = new Parameter<Vector3>(tr.localPosition, "m_rightHandPosition", this);
            m_rightHandPosition.hasChanged += updateRightHandPosition;
            m_rightHandRotation = new Parameter<Quaternion>(tr.localRotation, "m_rightHandRotation", this);
            m_rightHandRotation.hasChanged += updateRightHandRotation;
            m_righttHandAnimationState = new Parameter<int>(0, "rightHandAnimationState", this);

            m_leftHandGrabValue = new Parameter<Vector2>(Vector2.zero, "leftHandGrabValue", this);
            m_leftHandGrabValue.hasChanged += updateLeftHandGrabValue;
            m_rightHandGrabValue = new Parameter<Vector2>(Vector2.zero, "rightHandGrabValue", this);
            m_rightHandGrabValue.hasChanged += updateRightHandGrabValue;

            m_rigScale = new Parameter<float>(1f, "rigScale", this);
            m_rigScale.hasChanged += updateRigScale;
            m_armScale = new Parameter<float>(1f, "armScale", this);
            m_armScale.hasChanged += updateArmScale;

            m_gameReady = new Parameter<bool>(false, "gameReady", this);
            m_gameReady.hasChanged += updateGameReady;

            //JITTER HOTFIX
            //InitJitterHotfix();
            //position.hasChanged -= updatePosition;
        }

        public void Setup(byte playerNbr = 1, byte sID = 254, short oID = -1)
        {
            base.Setup(sID, oID);
            
            playerNumber = playerNbr;
            
            UpdatePlayerColorsIngame();
            setupDone = true;
        }

        public void setPlayerNumber(byte playerNbr)
        {
            playerNumber = playerNbr;
            // Set Color
            //sets the material color here, we do not need to transfer the color via network (not until we can choose our own color)
            UpdatePlayerColorsIngame();
        }

        private void UpdatePlayerColorsIngame(){
            detailColor = MinoGameManager.Instance.colors[Mathf.Clamp(playerNumber, 0, MinoGameManager.Instance.colors.Length)];
            foreach(Material mat in m_playerColorMateriels)
                mat.SetColor("_PlayerColor", detailColor);
            #if UNITY_EDITOR
            //only update if WE are the playercharacter
            if(MinoGameManager.Instance.showPlayerColorsInPlaymodeTint && GetComponent<MinoPlayerCharacter>())
                SettingsHelper.PlaymodeTint = Color.Lerp(detailColor, Color.white, 0.25f);   //lerp so the tint is not too annoying
            #endif
        }

        public void SetPlayerGameReady(object sender, bool isGameReady)
        {
            Debug.Log("Readiness Set");
            emitHasChanged((AbstractParameter)sender);
        }

        public virtual void UpdateRigScales()
        {
            emitHasChanged(m_rigScale);
            //emitHasChanged(m_armScale);

            Debug.Log(LogNetworkCalls.LogFunctionFlowCall(
                System.Reflection.MethodBase.GetCurrentMethod(), 
                gameObject.name, 
                "Sent: " + m_armScale.value + ", " + m_rigScale.value
            ));
            //Debug.Log("Sent: " + m_armScale.value + ", " + m_rigScale.value);
        }

        //!
        //! Function called, when Unity emit it's OnDestroy event.
        //!
        public override void OnDestroy()
        {
            base.OnDestroy();

            m_headPosition.hasChanged -= updateHeadPosition;
            m_headRotation.hasChanged -= updateHeadRotation;

            m_leftHandPosition.hasChanged -= updateLeftHandPosition;
            m_leftHandRotation.hasChanged -= updateLeftHandRotation;

            m_rightHandPosition.hasChanged -= updateRightHandPosition;
            m_rightHandRotation.hasChanged -= updateRightHandRotation;

            m_leftHandGrabValue.hasChanged -= updateLeftHandGrabValue;
            m_rightHandGrabValue.hasChanged -= updateRightHandGrabValue;

            m_rigScale.hasChanged -= updateRigScale;
            m_armScale.hasChanged -= updateArmScale;
            
            m_gameReady.hasChanged -= updateGameReady;   
        }

        public void AddCollisionObject(Transform obj)
        {
            if(!m_collidingObj.Contains(obj))
                m_collidingObj.Add(obj);
        }
        
        public void RemoveCollisionObject(Transform obj)
        {
                m_collidingObj.Remove(obj);
        }
        
        private void updateHeadPosition(object sender, Vector3 pos){
            headTransform.localPosition = pos;
            emitHasChanged((AbstractParameter)sender);
        }

        private void updateHeadRotation(object sender, Quaternion rot)
        {
            headTransform.localRotation = rot;
            emitHasChanged((AbstractParameter)sender);
        }

        private void updateLeftHandPosition(object sender, Vector3 pos)
        {
            leftHandTransform.localPosition = pos;
            emitHasChanged((AbstractParameter)sender);
        }

        private void updateLeftHandRotation(object sender, Quaternion rot)
        {
            leftHandTransform.localRotation = rot;
            emitHasChanged((AbstractParameter)sender);
        }

        private void updateRightHandPosition(object sender, Vector3 pos)
        {
            rightHandTransform.localPosition = pos;
            emitHasChanged((AbstractParameter)sender);
        }

        private void updateRightHandRotation(object sender, Quaternion rot)
        {
            rightHandTransform.localRotation = rot;
            emitHasChanged((AbstractParameter)sender);
        }

        private void updateLeftHandGrabValue(object sender, Vector2 grab)
        {
            leftGrabValue = grab;
            emitHasChanged((AbstractParameter)sender);
        }

        private void updateRightHandGrabValue(object sender, Vector2 grab)
        {
            rightGrabValue = grab;
            emitHasChanged((AbstractParameter)sender);
        }

        protected virtual void updateRigScale(object sender, float scale)
        {
            // TODO: add code here!
            emitHasChanged((AbstractParameter)sender);
        }

        protected virtual void updateArmScale(object sender, float scale)
        {
            // TODO: add code here!
            emitHasChanged((AbstractParameter)sender);
        }

        protected virtual void updateGameReady(object sender, bool ready)
        {
            isReady = true;
            emitHasChanged((AbstractParameter)sender);
        }

        protected override void Update(){
            // disabled to prevent TRS Update
            //base.Update();
        }
    }
}
