using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReadyObject : MonoBehaviour
{
    [SerializeField] private CalibrationVolume _calibrationVolume;
    public MinoDial startGameDial;
    
    private void OnTriggerEnter(Collider other)
    {
        if (MinoGameManager.Instance.IsSpectator())
            return;

        if (other.CompareTag("Head") && _calibrationVolume.player != null)  //player could not be set/calibrated if walked right into it!
        {
            _calibrationVolume.player.SetPlayerReady(true);
            if (MinoGameManager.Instance.CheckPlayerReadiness())
            {
                Debug.Log("Make Start Game Dial usable");
                startGameDial.usable = true;
                startGameDial.hingeAngleOffset = startGameDial.hinge.angle;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Head") && _calibrationVolume.player != null)
        {
            _calibrationVolume.player.SetPlayerReady(false);
            if (MinoGameManager.Instance.CheckPlayerReadiness())
            {
                Debug.Log("Make Start Game Dial unusable");
                startGameDial.usable = false;
            }
        }
    }
}
