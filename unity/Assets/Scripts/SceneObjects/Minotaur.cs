using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Minotaur : MonoBehaviour
{
    //public AnimatorController ac;
    public Animator animator;
    private bool defeated;

    public int hitpoints = 10;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetController()
    {
        //animator.runtimeAnimatorController = ac;

    }

    private void OnCollisionEnter(Collision other)
    {
        if (defeated) return;
        if (other.gameObject.CompareTag("Weapon"))
        {
            Debug.Log("hit");
            hitpoints -= 1;
            animator.SetTrigger("Hit");

            if (hitpoints <= 0)
            {
                Debug.Log("mino dies");
                animator.runtimeAnimatorController = null;
                
            }

        }
    }
}
