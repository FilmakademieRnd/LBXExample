using System.Collections.Generic;
using UnityEngine;

namespace tracer{
    
    public class MinoCharacter : SceneObjectMino{

        private bool isReplicate = false;   //it must not change its lock state every and will be connected with the ID from the actual client!

        //GameState
        public byte playerNumber
        {
            get; protected set;
        } = 1;


        public void Setup(byte playerNr = 1, byte sID = 254, short oID = -1){
            base.Setup(sID, oID);
            setPlayerNumber(playerNr);
        }

        public void setPlayerNumber(byte playerNr){
            playerNumber = playerNr;
            UpdatePlayerColorsIngame();
        }
        public void IsReplicate(){
            gameObject.name += "_Replicate";
            isReplicate = true;
            lockObjectLocal(true);
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(GetComponent<Collider>());
            Destroy(GetComponent<Rigidbody>());
        }

        private void UpdatePlayerColorsIngame(){
            Color detailColor = MinoGameManager.Instance.colors[Mathf.Clamp(playerNumber, 0, MinoGameManager.Instance.colors.Length)];
            foreach(MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
                mr.material.SetColor("_BaseColor", detailColor);
        }

        //protected override void Update(){
            // disabled to prevent TRS Update
            //base.Update();
        //}
    }
}
