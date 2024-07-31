using Autohand;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinoButtonVibration : MonoBehaviour
{
    Hand handCollider = null;


    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Hand hand))
        {
            handCollider = hand;
        }
    }

    public void ResetHandCollider()
    {
        handCollider = null;
    }

    public void PlayVibrationOnHand()
    {
        if(handCollider)
            handCollider.PlayHapticVibration(0.25f, 0.25f);
    }
}
