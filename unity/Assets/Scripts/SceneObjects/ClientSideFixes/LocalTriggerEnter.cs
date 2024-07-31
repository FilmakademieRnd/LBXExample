using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;

public class LocalTriggerEnter : MonoBehaviour{
    
    public enum WhatToTriggerEnum{

        nothing = 0,
        startPlatformParentCheck = 10,
        stopPlatformParentCheck = 12
    }

    public bool onlyOnce = true;

    public WhatToTriggerEnum triggerThis = WhatToTriggerEnum.nothing;


    private bool used = false;
    
    void OnTriggerEnter(Collider other){
        if(other.CompareTag("Head")){
            MinoPlayerCharacter minoPlayer = MinoGameManager.Instance.m_playerCharacter;
            if(!minoPlayer || MinoGameManager.Instance.IsSpectator())
                return;

            switch(triggerThis){
                case WhatToTriggerEnum.startPlatformParentCheck:
                    (minoPlayer as SceneObjectMino).StartPlatformParentCheck();
                    break;
                case WhatToTriggerEnum.stopPlatformParentCheck:
                    (minoPlayer as SceneObjectMino).StopPlatformParentCheck();
                    break;

            }
            used = true;
        }
    }
}
