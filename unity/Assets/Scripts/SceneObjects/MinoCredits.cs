using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinoCredits : MonoBehaviour
{
    private Transform playerCameraHead;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("Credits", 3f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void Credits()
    {
        playerCameraHead = GameObject.Find("Camera (head)").transform;
        transform.rotation = MinoGameManager.Instance.IsSpectator() ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, playerCameraHead.rotation.eulerAngles.y, 0);
        transform.position = MinoGameManager.Instance.IsSpectator() ? new Vector3(0, playerCameraHead.position.y, transform.position.z + 4) : new Vector3(0, playerCameraHead.position.y, transform.position.z);
        Debug.Log(playerCameraHead.transform.position.y);
    }
}
