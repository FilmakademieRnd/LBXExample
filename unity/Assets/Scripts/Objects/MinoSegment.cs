using System;
using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoSegment : MonoBehaviour
{
    [SerializeField] private MeshRenderer indicator;

    [SerializeField] private MinoPlayerCharacter minoPlayerCharacter;
    public bool inTransition = false;
    public bool isExit = false;
    public List<MinoSegment> linkedSegments = new List<MinoSegment>();
    void Start()
    {
        if (MinoGameManager.Instance.IsSpectator())
        {
            GetComponent<BoxCollider>().enabled = false;
        }
        else
        {
            indicator = GetComponentInChildren<MeshRenderer>();
            
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {


        if (other.gameObject.CompareTag("Head") && other.transform.parent.parent.parent.GetComponentInParent<MinoPlayerCharacter>())
        {
            if(Debug.isDebugBuild || MinoGameManager.Ingame_Debug){
                Debug.Log("<color=grey>Segments are switched off due to DebugSystem.debugMode</color>");
                return;
            }

            if (minoPlayerCharacter == null)
            {
                GetPlayerCharacter();
                //Debug.Log("Player Character: " + minoPlayerCharacter + " entered " + this.name);
            }

            if (indicator.enabled && this == minoPlayerCharacter.lastSegment)
            {
                // Reentering while faded out
                SetIndicatorVisibility(false);
                minoPlayerCharacter.FadeHeadToBlack(false);
                minoPlayerCharacter.ActivateNormalFade(); // Reset to normal fade
                minoPlayerCharacter.segments.Add(this);
                minoPlayerCharacter.ghostModeOn = false;
                minoPlayerCharacter.SwitchColliderEnabled(true);
            }
            
            // If already faded out return
            if (minoPlayerCharacter.headEye.GetComponent<Animator>().GetBool("isBlack")) return;
            
            if (minoPlayerCharacter.lastSegment == null)
            {
                if (!minoPlayerCharacter.segments.Contains(this))
                {
                    minoPlayerCharacter.segments.Add(this);
                    minoPlayerCharacter.lastSegment = this;
                    //Debug.Log("Add Segment to list and set as last segment");
                }
            }
            else
            {
                if (minoPlayerCharacter.lastSegment.linkedSegments.Contains(this) &&
                    !minoPlayerCharacter.segments.Contains(this))
                {
                    minoPlayerCharacter.segments.Add(this);
                    minoPlayerCharacter.lastSegment = this;
                    //Debug.Log("Add Segment to list and replace last segment");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {

        if (other.gameObject.CompareTag("Head") &&
            other.transform.parent.parent.parent.GetComponentInParent<MinoPlayerCharacter>())
        {
            if(Debug.isDebugBuild || MinoGameManager.Ingame_Debug)
                return;
                
            if (!inTransition)
            {
                if (minoPlayerCharacter.segments.Contains(this))
                    minoPlayerCharacter.segments.Remove(this);
            }

            if (minoPlayerCharacter.segments.Count < 1)
            {
                if (minoPlayerCharacter.lastSegment.isExit) return;
                if (!minoPlayerCharacter.headEye.GetComponent<Animator>().GetBool("isBlack"))
                {
                    minoPlayerCharacter.headEye.GetComponent<Animator>().SetTrigger("NoFade");
                    minoPlayerCharacter.FadeHeadToBlack(true);
                    if (minoPlayerCharacter.lastSegment != null)
                        minoPlayerCharacter.lastSegment.SetIndicatorVisibility(true);
                    minoPlayerCharacter.ghostModeOn = true;
                    minoPlayerCharacter.SwitchColliderEnabled(false);
                }
            }
            else
            {
                // In Case someone switches back and forth too fast without properly entering/exiting
                if (!minoPlayerCharacter.segments.Contains(minoPlayerCharacter.lastSegment))
                    minoPlayerCharacter.lastSegment = minoPlayerCharacter.segments[0];
                
            }
        }
    }

    private void GetPlayerCharacter()
    {
        minoPlayerCharacter = MinoGameManager.Instance.m_playerCharacter;
    }
    
    public void SetIndicatorVisibility(bool visible)
    {
        indicator.enabled = visible;
    }

    public void SetInTransition(bool val)
    {
        inTransition = val;
    }
}
