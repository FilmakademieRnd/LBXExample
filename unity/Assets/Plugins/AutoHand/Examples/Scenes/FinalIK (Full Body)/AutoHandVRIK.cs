using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

namespace Autohand {
    [DefaultExecutionOrder(-5), RequireComponent(typeof(VRIK))]
    public class AutoHandVRIK : MonoBehaviour {
        public Hand rightHand;
        public Hand leftHand;
        [Tooltip("The transform (or a child transform) of the Tracked VR controller")]
        public Transform rightTrackedController;
        [Tooltip("The transform (or a child transform) of the Tracked VR controller")]
        public Transform leftTrackedController;

        [HideInInspector, Tooltip("Should be a transform under the Auto Hand, can be used to adjust the IK offset so the hands connect with the arms properly (This is the point where the wrists follow the hands)")]
        public Transform rightIKTarget = null;
        [HideInInspector, Tooltip("Should be a transform under the Auto Hand, can be used to adjust the IK offset so the hands connect with the arms properly (This is the point where the wrists follow the hands)")]
        public Transform leftIKTarget = null;
        [HideInInspector, Tooltip("Should be a transform under the IK Character hierarchy, can be used to adjust the IK offset so the hands connect with the arms properly")]
        public Transform rightHandFollowTarget = null;
        [HideInInspector, Tooltip("Should be a transform under the IK Character hierarchy, can be used to adjust the IK offset so the hands connect with the arms properly")]
        public Transform leftHandFollowTarget = null;

        VRIK visibleIK;
        VRIK invisibleIK;
        bool isCopy = false;

        public void DesignateCopy() {
            isCopy = true;
        }

        void Start() {
            visibleIK = GetComponent<VRIK>();

            if (!isCopy)
                SetupIKCopy();

            if(AutoHandPlayer.Instance != null)
                visibleIK.transform.position -= Vector3.up * AutoHandPlayer.Instance.heightOffset;

        }

        void SetupIKCopy() {
            if(rightIKTarget == null)
            {
                rightIKTarget = new GameObject().transform;
                rightIKTarget.name = "rightIKTarget";
                rightIKTarget.transform.parent = rightHand.transform;

                rightIKTarget.transform.localPosition = Vector3.zero;
                rightIKTarget.transform.localRotation = Quaternion.identity;
            }
            if (leftIKTarget == null)
            {
                leftIKTarget = new GameObject().transform;
                leftIKTarget.name = "leftIKTarget";
                leftIKTarget.transform.parent = leftHand.transform;

                leftIKTarget.transform.localPosition = Vector3.zero;
                leftIKTarget.transform.localRotation = Quaternion.identity;
            }
            if (rightHandFollowTarget == null)
            {
                rightHandFollowTarget = new GameObject().transform;
                rightHandFollowTarget.name = "rightHandTarget";
                rightHandFollowTarget.transform.parent = rightHand.transform.parent;

                rightHandFollowTarget.transform.localPosition = rightHand.transform.localPosition;
                rightHandFollowTarget.transform.localRotation = rightHand.transform.localRotation;
            }
            if (leftHandFollowTarget == null)
            {
                leftHandFollowTarget = new GameObject().transform;
                leftHandFollowTarget.name = "leftHandTarget";
                leftHandFollowTarget.transform.parent = leftHand.transform.parent;
                leftHandFollowTarget.transform.localPosition = leftHand.transform.localPosition;
                leftHandFollowTarget.transform.localRotation = leftHand.transform.localRotation;
            }

            rightHand.transform.parent = visibleIK.transform.parent;
            leftHand.transform.parent = visibleIK.transform.parent;

            visibleIK.references.rightHand = rightHandFollowTarget;
            visibleIK.references.leftHand = leftHandFollowTarget;

            invisibleIK = Instantiate(visibleIK.gameObject, visibleIK.transform.parent).GetComponent<VRIK>();
            invisibleIK.name = "Hidden IK Copy (Auto Hand + VRIK requirement)";
            DeactivateEverything(invisibleIK.transform);
            invisibleIK.enabled = true;

            if (invisibleIK.CanGetComponent<AutoHandVRIK>(out var autoIK)) {
                autoIK.DesignateCopy();
                autoIK.enabled = true;
                rightHand.follow = autoIK.rightHandFollowTarget;
                leftHand.follow = autoIK.leftHandFollowTarget;
            }

            visibleIK.solver.rightArm.target = rightIKTarget;
            visibleIK.solver.leftArm.target = leftIKTarget;
            invisibleIK.solver.rightArm.target = rightTrackedController;
            invisibleIK.solver.leftArm.target = leftTrackedController;


        }

        void DeactivateEverything(Transform deactivate) {
            var behaviours = deactivate.GetComponents<Component>();
            var childBehaviours = deactivate.GetComponentsInChildren<Component>();
            for(int j = behaviours.Length - 1; j >= 0; j--)
                if(!(behaviours[j] is Animator) && !(behaviours[j] is VRIK) && !(behaviours[j] is AutoHandVRIK) && !(behaviours[j] is Transform))
                    Destroy(behaviours[j]);
            for(int j = childBehaviours.Length - 1; j >= 0; j--)
                if(!(childBehaviours[j] is Animator) && !(childBehaviours[j] is VRIK) && !(childBehaviours[j] is AutoHandVRIK) && !(childBehaviours[j] is Transform))
                    Destroy(childBehaviours[j]);
        }
    }

}