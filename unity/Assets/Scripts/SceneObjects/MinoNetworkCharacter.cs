using System;
using UnityEngine;
using RootMotion.FinalIK;

namespace tracer
{
    public class MinoNetworkCharacter : MinoCharacter
    {
        [SerializeField]
        private Animator m_animator;
        private bool m_ownerLock = false;

        private Transform rig;
        private Transform leftUpperArm;
        private Transform rightUpperArm;

        private int m_leftHandGrabID = 0;
        private int m_rightHandGrabID = 0;
        private int m_leftHandTriggerID = 0;
        private int m_rightHandTriggerID = 0;

        public VRIK vrik;

        public override void Awake()
        {
            //useThomasJitterFixHere = true;    //always use, but only ever receive, never send
            base.Awake();
            
            rig = FindInChildren(tr, "Avatar");
            leftUpperArm = FindInChildren(tr, "CC_Base_L_Upperarm");
            rightUpperArm = FindInChildren(tr, "CC_Base_R_Upperarm");

            m_leftHandGrabID = Animator.StringToHash("LeftHandGrabValue");
            m_rightHandGrabID = Animator.StringToHash("RightHandGrabValue");
            m_leftHandTriggerID = Animator.StringToHash("LeftHandTriggerValue");
            m_rightHandTriggerID = Animator.StringToHash("RightHandTriggerValue");

            m_playerColorMateriels.Add(tr.FindDeepChild("Avatar_head_01").GetComponent<SkinnedMeshRenderer>().material);
            m_playerColorMateriels.Add(tr.FindDeepChild("Avatar_Shackles_01").GetComponent<SkinnedMeshRenderer>().material);
            m_playerColorMateriels.Add(tr.FindDeepChild("Avatar_Body_01").GetComponent<SkinnedMeshRenderer>().material);

            //JITTER HOTFIX
            InitJitterHotfix();
            position.hasChanged -= updatePosition;
        }

        protected override void Update()
        {
            // disabled to prevent sending back all other updates
            //base.Update();

            //WE (NETWORK CHAR) NEVER SEND, ONLY RECEIVE
            ApplyNetworkPosDataOnLocked();

            UpdateHandGrabValues();
        }

        private void UpdateHandGrabValues()
        {
            m_animator.SetFloat(m_leftHandTriggerID, leftGrabValue.x);
            m_animator.SetFloat(m_leftHandGrabID, leftGrabValue.y);
            m_animator.SetFloat(m_rightHandTriggerID, rightGrabValue.x);
            m_animator.SetFloat(m_rightHandGrabID, rightGrabValue.y);
        }
        
        private void ReceiveCalibration(float scale, bool isBody)
        {
            Debug.Log("Receive Calibration");
            Vector3 newScale = new Vector3(scale, scale, scale);

            if (isBody)
            {
                rig.localScale = newScale;
            }
            else
            {
                leftUpperArm.localScale = newScale;
                rightUpperArm.localScale = newScale;
            }
        }

        protected override void updateRigScale(object sender, float scale)
        {
            ReceiveCalibration(scale, true);
            emitHasChanged((AbstractParameter)sender);
        }
        
        protected override void updateArmScale(object sender, float scale)
        {
            ReceiveCalibration(scale, false);
            emitHasChanged((AbstractParameter)sender);
        }
       
        private static Transform FindInChildren(Transform transform, string name){
            foreach (Transform t in transform.GetComponentsInChildren<Transform>()){
                if (string.Equals(t.name, name))
                    return t;
            }

            return null;
        }

        
    }
}