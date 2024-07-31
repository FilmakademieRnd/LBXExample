using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace tracer
{

    public class MinoInteractable : SceneObjectMino
    {
        protected RPCParameter<int> m_onTriggerEnter;
        protected RPCParameter<int> m_onTriggerExit;
        
        [SerializeField]
        protected Collider m_triggerCollider;   //NOT NECESSARY
        protected List<SceneObject> m_collidedSceneObjects;

        protected RPCParameter<bool> m_isTriggered;
        protected bool isTriggered;

        protected int m_collidedPlayerAmount = 0;

        //!
        //! Initialisation
        //!
        public override void Awake()
        {
            base.Awake();

            m_onTriggerEnter = new RPCParameter<int>(0,"triggerEnter", this);
            m_onTriggerEnter.hasChanged += UpdateTriggerEnter;
            m_onTriggerEnter.setCall(triggerEnter);
            
            m_onTriggerExit = new RPCParameter<int>(0,"triggerExit", this);
            m_onTriggerExit.hasChanged += UpdateTriggerExit;
            m_onTriggerExit.setCall(triggerExit);

            m_isTriggered = new RPCParameter<bool>(false, "isTriggered", this);
            m_isTriggered.setCall(IsTriggered);
            m_isTriggered.hasChanged += UpdateTrigger;   
        }
        
        protected override void emitHasChanged (AbstractParameter parameter)
        {
            if (!_lock)
                base.emitHasChanged(parameter);
        }

        protected virtual void OnTriggerEnter(Collider other){
            if (other.CompareTag("Head")){
                m_collidedPlayerAmount = Mathf.Clamp(m_collidedPlayerAmount+1, 0, MinoGameManager.Instance.numberPlayers);
                m_onTriggerEnter.Call(0);
                Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_onTriggerEnter", "Colliding Players: "+m_collidedPlayerAmount));
            }
        }

        protected virtual void OnTriggerExit(Collider other){
            if (other.CompareTag("Head")){
                m_collidedPlayerAmount = Mathf.Clamp(m_collidedPlayerAmount-1, 0, MinoGameManager.Instance.numberPlayers);
                m_onTriggerExit.Call(0);
                Debug.Log(LogNetworkCalls.LogCallFromSender(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "m_onTriggerEnter", "Colliding Players: "+m_collidedPlayerAmount));
            }
        }

        protected virtual void triggerEnter(int input){
            m_collidedPlayerAmount = Mathf.Clamp(m_collidedPlayerAmount+1, 0, MinoGameManager.Instance.numberPlayers);
            Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Colliding Players: "+m_collidedPlayerAmount));
        }

        private void triggerExit(int input){
            m_collidedPlayerAmount = Mathf.Clamp(m_collidedPlayerAmount-1, 0, MinoGameManager.Instance.numberPlayers);
            Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name, "Colliding Players: "+m_collidedPlayerAmount));
        }

        private void UpdateTrigger(object sender, bool triggered)
        {
            emitHasChanged((AbstractParameter)sender);
        }

        public virtual void IsTriggered(bool triggered)
        {
            //Debug.Log(LogNetworkCalls.LogCallAtReceiver(System.Reflection.MethodBase.GetCurrentMethod(), gameObject.name));
        }

        private void UpdateTriggerEnter(object sender, int input)
        {
            emitHasChanged((AbstractParameter)sender);
        }
        
        private void UpdateTriggerExit(object sender, int input)
        {
            emitHasChanged((AbstractParameter)sender);
        }

        protected override void Update()
        {
            base.Update();
        }

    }
}
