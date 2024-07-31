using UnityEngine;

namespace tracer{
    public class MinoNetworkSpectator : MinoCharacter{
        
        public override void Awake(){
            if (_core == null)
                _core = GameObject.FindObjectOfType<Core>();

            _sceneID = 254;
            
            tr = GetComponent<Transform>();
            //m_uiManager = _core.getManager<UIManager>();

            
            //InitJitterHotfix();
        }

        /*protected override void Update(){
            //WE (NETWORK CHAR) NEVER SEND, ONLY RECEIVE
            ApplyNetworkPosDataOnLocked();
        }
        */
        public override void UpdateRigScales(){

        }
    }

}