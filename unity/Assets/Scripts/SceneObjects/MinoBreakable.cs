using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;
using UnityEngine.Events;

public class MinoBreakable : MinoGrabbable
{
    [SerializeField] private ConfigurableJoint joint;
    public bool isGrabbed;
    private bool isUsed = false;
    public bool isBreakable = false;
    public UnityEvent jointBreak;
    void Update()
    {
        if (!isBreakable) return;
        if (isUsed) return;
        if (!isGrabbed) return;

        if (joint == null)
        {
            Debug.Log("Break");
            isUsed = true;
            jointBreak.Invoke();
        }
    }

    public void SetGrabbed(bool grabbed)
    {
        isGrabbed = grabbed;
    }
}
