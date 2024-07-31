using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TestManager : MonoBehaviour{
    
    //GET REFERENCES WE NEED
    //LETS HOCK INTO EVENTS
    //ONLY DO LOCAL STUFF HERE (trigger/receive network-calls though)

    public Event onStart, onJoin, onInit;
    public Event onOtherJoined, onOtherLost;

    public static TestManager Instance { get; private set; }


    void Awake(){
        Init();
    }

    private void Init(){
        if (Instance != null && Instance != this){ 
            Destroy(this); 
        }else{ 
            Instance = this; 
        }
    }

}
