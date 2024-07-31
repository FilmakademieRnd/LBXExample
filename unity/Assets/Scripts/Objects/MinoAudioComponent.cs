using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MinoAudioComponent : MonoBehaviour
{
    public bool playRandom = false;
    private int soundIndex = 0;
    private AudioSource source;
    private int lastIndexPlayed = 0;

    public AudioClip[] sounds;

    private void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void PlaySound()
    {
        if (playRandom)
        {
            soundIndex = (int)Random.Range(0, sounds.Length);
            if (soundIndex == lastIndexPlayed)
            {
                soundIndex = (soundIndex + 1) % sounds.Length;
                //Debug.Log("Same Index, Incrementing!");
            }

            lastIndexPlayed = soundIndex;
        }
        
        //Debug.Log("Play Sound: " + soundIndex);
        source.clip = sounds[soundIndex];
        source.Play();
        
        if (!playRandom)
        {
            soundIndex++;
            soundIndex = soundIndex % sounds.Length;
        }
    }

}
