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

using tracer;
using UnityEngine;
using System.Collections.Generic;

public class MINO_TEMPLATE : SceneObjectMino{

    private RPCParameter<bool> networkParameter;

    public override void Awake(){

        //INITIALIZE
        _core = GameObject.FindObjectOfType<Core>();
        _sceneID = 254;
        _parameterList = new List<AbstractParameter>();

        //INIT PARAMETER
        networkParameter = new RPCParameter<bool>(false, "parameterChanged", this);
        networkParameter.hasChanged += ParameterHasChanged;   
        networkParameter.setCall(ParameterChangeCalled);
    }

    public override void OnDestroy(){
        _core.removeParameterObject(this);
        _core.getManager<NetworkManager>().RemoveSceneObject(this);
    }


    private void ParameterHasChanged(object sender, bool value){  
        GetComponentInChildren<TextMesh>().text += "\n<color=blue>ParameterHasChanged</color>";
        networkParameter.setValue(value, false);
        GetComponent<MeshRenderer>().material.color = value ? Color.green : Color.red;
        emitHasChanged((AbstractParameter)sender);  //emit the change into the network
    }

    private void ParameterChangeCalled(bool value){   
        GetComponentInChildren<TextMesh>().text += "\n<color=yellow>ParameterChangeCalled</color>";
        GetComponent<MeshRenderer>().material.color = value ? Color.green : Color.red;
    }


    #region SIMULATE
    [Header("DEBUG")]
    public bool debugSimulateChange = false;

    protected override void Update(){
        if(debugSimulateChange){
            networkParameter.Call(false, false);
        }
    }
    void OnTriggerEnter(Collider col){
        networkParameter.Call(!networkParameter.value, false);
    }
    #endregion
}
