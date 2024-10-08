/*
-----------------------------------------------------------------------------------
TRACER Location Based Experience Example

Copyright (c) 2024 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Labs
https://github.com/FilmakademieRnd/LBXExample

TRACER Location Based Experience Example is a development by Filmakademie 
Baden-Wuerttemberg, Animationsinstitut R&D Labs in the scope of the EU funded 
project EMIL (101070533).

This program is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the MIT License for more details.
You should have received a copy of the MIT License along with this program;
if not go to https://opensource.org/licenses/MIT
-----------------------------------------------------------------------------------
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tracer;
using UnityEngine.Events;

public class MinoInteractable : SceneObjectMino{

    public InteractableNetworkBehaviourEnum interactableNetworkBehaviour = InteractableNetworkBehaviourEnum.byMaster;

    public bool neverMoves = true;

    public UnityEvent onTriggeredEvent;
    public UnityEvent offTriggeredEvent;
    public UnityEvent isActiveEvent;

    public bool debugSetTriggered = false;
    public bool debugSetUntriggered = false;

    private RPCParameter<bool> m_isTriggered;

    private bool isActive = false;

    public override void Awake(){

        if(neverMoves){
            _core = GameObject.FindObjectOfType<Core>();
            tr = GetComponent<Transform>();
        
            _parameterList = new List<AbstractParameter>();

            _sceneID = 254;

        }else{
            base.Awake();
        }

        m_isTriggered = new RPCParameter<bool>(false, "isTriggered", this);
        m_isTriggered.hasChanged += HasChanged;   
        m_isTriggered.setCall(IsTriggered);

        coroMoveEvent = new UnityEvent<int>();
    }

    private void HasChanged(object sender, bool triggered){  
        switch(interactableNetworkBehaviour){
            case InteractableNetworkBehaviourEnum.byMaster:
                if(!MinoGameManager.Instance.WeAreTheLowestPlayerNumberPlayer()){   //check _lock?
                    emitHasChanged((AbstractParameter)sender);
                }
                break;
            case InteractableNetworkBehaviourEnum.byEveryone:
                //dont emit, just execute (be aware that only network-events that will emit on their own should be triggered, e.g. pos changes)
                break;
            case InteractableNetworkBehaviourEnum.byEveryone4Everyone:
                emitHasChanged((AbstractParameter)sender);
                break;
        }
    }

    private void IsTriggered(bool triggered){   
        switch(interactableNetworkBehaviour){
            case InteractableNetworkBehaviourEnum.byMaster:
                if(_lock || !MinoGameManager.Instance.WeAreTheLowestPlayerNumberPlayer())
                    break;
                if(triggered)
                    onTriggeredEvent?.Invoke();
                else
                    offTriggeredEvent?.Invoke();
                break;
            case InteractableNetworkBehaviourEnum.byEveryone:
            case InteractableNetworkBehaviourEnum.byEveryone4Everyone:
                if(triggered)
                    onTriggeredEvent?.Invoke();
                else
                    offTriggeredEvent?.Invoke();
                break;
        }
    }

    void OnTriggerEnter(Collider col){
        Event_SetIsTriggered(true);
    }
    void OnTriggerExit(Collider col){
        Event_SetIsTriggered(false);
    }

    private void IsActive(){
        isActiveEvent?.Invoke();
    }

    protected override void Update(){
        if(!neverMoves)
            base.Update();

        if(debugSetTriggered){
            Event_SetIsTriggered(true);
            debugSetTriggered = false;
        }
        if(debugSetUntriggered){
            Event_SetIsTriggered(false);
            debugSetUntriggered = false;
        } 
        
        if(isActive){
            IsActive();
        }
    }

    public override void OnDestroy(){
        if(!neverMoves)  
            base.OnDestroy();
        else{
            _core.removeParameterObject(this);
            _core.getManager<NetworkManager>().RemoveSceneObject(this);
        }
    }

    public void Event_SetIsTriggered(bool b){ 
        m_isTriggered.Call(b, true);
    }

    public void EventLocal_SetActive(bool b){
        //So we will send an IsActive
        isActive = b;
    }


    //TEST FUNCTIONS
    [Header("Test Functions")]
    public Transform trToMove;
    public void MoveTransformVertical(int ver){
        trToMove.position += Vector3.up * ver * Time.fixedDeltaTime;
    }
    public void MoveTransformHorizontal(int hor){
        trToMove.position += Vector3.right * hor * Time.fixedDeltaTime;
    }
    public void MoveTransformForward(int fwd){
        trToMove.position += Vector3.forward * fwd * Time.fixedDeltaTime;
    }

    public void MoveTransform_VerticalCoro(){
        coroMoveEvent.RemoveAllListeners();
        coroMoveEvent.AddListener(MoveTransformVertical);
        StartCoroutine(MoveTransformCoro());
    }
    public void MoveTransform_HorizontalCoro(){
        coroMoveEvent.RemoveAllListeners();
        coroMoveEvent.AddListener(MoveTransformHorizontal);
        StartCoroutine(MoveTransformCoro());
    }
    public void MoveTransform_ForwardCoro(){
        coroMoveEvent.RemoveAllListeners();
        coroMoveEvent.AddListener(MoveTransformForward);
        StartCoroutine(MoveTransformCoro());
    }

    private bool isExecuting = false;
    private UnityEvent<int> coroMoveEvent;
    private IEnumerator MoveTransformCoro(){
        if(isExecuting)
            yield break;
        
        isExecuting = true;
        float t = 0f;
        while(t<1f){
            t += Time.fixedDeltaTime/3f;
            coroMoveEvent.Invoke(1);
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        while(t>0f){
            t -= Time.fixedDeltaTime/3f;
            coroMoveEvent.Invoke(-1);
            yield return null;
        }

        isExecuting = false;
    }

    private int pressedCount = 0;

    public void IncreasePressedCount(){
        pressedCount++;
        GetComponentInChildren<TextMesh>().text = "Pressed ("+pressedCount+")";
    }

    public void TriggerNextDelayed(MinoInteractable nextInteractable){
        StartCoroutine(TriggerNextDelayed_Coro(nextInteractable));
    }
    private IEnumerator TriggerNextDelayed_Coro(MinoInteractable nextInteractable){
        yield return new WaitForSeconds(0.5f);
        nextInteractable.Event_SetIsTriggered(true);
    }

    private int colorIndex = 0;

    public void ChangeColorRandom(){
        Color[] colors = new Color[]{Color.red, Color.green, Color.blue, Color.yellow};
        colorIndex = (colorIndex+1) % colors.Length;
        GetComponentInChildren<MeshFilter>().GetComponent<MeshRenderer>().material.color = colors[colorIndex]; //Random.ColorHSV();
    }


}
