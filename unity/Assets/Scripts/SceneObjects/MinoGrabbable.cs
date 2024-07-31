using System;
using Autohand;
using UnityEngine;
using UnityEngine.Events;

namespace tracer
{
    public class MinoGrabbable : MinoInteractable{

        public Grabbable grabbable;
        public UnityEvent forcedRelease;
        public int lastGrabbedBy = -1;

        [Header("Safety Net Reset")]
        public Vector3 originPos;
        public Quaternion originRot;

        //!
        //! Cache the transform component - its a bit faster than .transform
        //!
        private Rigidbody rg;
        private bool exectuteLock = false;
        private RPCParameter<int> m_lastGrabbedBy;

        public override void Awake()
        {
            base.Awake();

            rg = GetComponent<Rigidbody>();

            m_lastGrabbedBy = new RPCParameter<int>(-1, "lastGrabbedBy", this);
            m_lastGrabbedBy.hasChanged += UpdateRPC;
            m_lastGrabbedBy.setCall(UpdateLastGrabbedBy);

            originPos = tr.position;
            originRot = tr.rotation;

            InitJitterHotfix();

            if (GetComponentInChildren<Grabbable>())
                grabbable = GetComponentInChildren<Grabbable>();
            else
                grabbable = GetComponent<Grabbable>();
        }

        public override void OnDestroy()
        {
            m_lastGrabbedBy.hasChanged -= UpdateRPC;
        }

        #region NetworkCommunication
        private void UpdateRPC(object sender, int c){
            emitHasChanged((AbstractParameter)sender);
        }
        private void UpdateLastGrabbedBy(int e){
            lastGrabbedBy = e;
        }
        #endregion

        public override void SetLock(bool e)
        {
            base.SetLock(e);
            if (e)
            {
                lock (this)
                {
                    exectuteLock = true;
                }
            }
            else
            {
                // Somethin to happen on Unlock 
            }
        }

        protected override void Update()
        {
            base.Update();
            if (exectuteLock)
            {
                ExecuteLock();
                exectuteLock = false;
            }
        }

        private void ExecuteLock()
        {
            Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "ForceHandsRelease & invoke forcedRelease & SetVeloToZero"));
            grabbable.ForceHandsRelease();
            forcedRelease.Invoke();
            
            SetVeloToZero();
        }

        //is called via UnityEvent (assigned in Inspector > Grabbable > OnGrab)
        public void Event_SetVeloToZero(){
            Debug.Log(LogNetworkCalls.LogFunctionFlowCall(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "call SetVeloToZero"));
            SetVeloToZero();
        }

        private void SetVeloToZero(){
            if(rg){
                rg.angularVelocity = Vector3.zero;
                rg.velocity = Vector3.zero;
            }
        }

        public void SetLastGrabbedBy(){
            if(!MinoGameManager.Instance.m_playerCharacter)
                return;
                
            int localPlayerId = MinoGameManager.Instance.m_playerCharacter.id;
            m_lastGrabbedBy.Call(localPlayerId,true);
        }
        public void SetGrabbableReleased(){
            m_lastGrabbedBy.Call(-1, true);
        }
        
        public int GetLastGrabbedBy(){ return lastGrabbedBy; }

        public void ResetToOrigin(){
            SetVeloToZero();
            tr.position = originPos;
            tr.rotation = originRot;
            if(rg)
                rg.isKinematic = true;
            
            Debug.Log("RESET");
        }
    }
}
