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
            Destroy(GetComponent<SimpleCharacterController>());
        }

        private void UpdatePlayerColorsIngame(){
            Color detailColor = MinoGameManager.Instance.colors[Mathf.Clamp(playerNumber, 0, MinoGameManager.Instance.colors.Length)];
            foreach(MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
                mr.material.color = detailColor;
        }

        //protected override void Update(){
            // disabled to prevent TRS Update
            //base.Update();
        //}
    }
}
