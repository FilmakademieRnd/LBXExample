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

using UnityEngine;

public class SimpleCharacterController : MonoBehaviour{
    
    public float walkSensitivity = 1f;
    public float lookSensitivity = 1f;
    public float jumpStrength = 5f;

    private Transform tr;
    private Vector3 lookInput;

    void Start(){
        tr = GetComponent<Transform>();
        lookInput = tr.localEulerAngles;
        
        #if !UNITY_EDITOR
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        #endif
    }

    void Update(){
        
        Look();
        Walk();
        Jump();

        #if !UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.Escape)){
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        #endif

    }

    private void Look(){
        const float ROT_VERTICAL_MIN = -30.0f;
        const float ROT_VERTICAL_MAX = 40.0f;

        lookInput.x -= Input.GetAxis("Mouse Y") * (lookSensitivity * Time.deltaTime);
        lookInput.y += Input.GetAxis("Mouse X") * (lookSensitivity * Time.deltaTime);

        if(lookInput.x < ROT_VERTICAL_MIN)
            lookInput.x = ROT_VERTICAL_MIN;
        else if (lookInput.x > ROT_VERTICAL_MAX)
            lookInput.x = ROT_VERTICAL_MAX;
        
        tr.localRotation = Quaternion.Euler(lookInput);
    }

    private void Walk(){
        Vector3 rightVec = tr.right;
        rightVec.y = 0f;
        Vector3 fwdVec = tr.forward;
        fwdVec.y = 0f;

        tr.position += Input.GetAxis("Horizontal") * rightVec * Time.deltaTime * walkSensitivity;
        tr.position += Input.GetAxis("Vertical") *   fwdVec * Time.deltaTime * walkSensitivity;
    }

    private void Jump(){
        if(Input.GetKeyDown(KeyCode.Space)){
            if(GetComponent<Rigidbody>()){
                GetComponent<Rigidbody>().AddForce(Vector3.up*jumpStrength, ForceMode.Impulse);
            }
        }
    }
}
