using System;
using UnityEngine;

public class MinoTorch : MonoBehaviour
{
    private Vector3 previousPosition;
    private float deltaTime;
    private float maxVelocity = 25.0f;
    private float velocity;
    private float normVelocity;

    private bool _isDead = false;
    [SerializeField] private bool _isActive = false;

    public ParticleSystem fireParticleSystem;
    public AudioSource audio;
    public AudioSource additionalAudio;
    
    public Rigidbody rb; // Reference to the Rigidbody component of the object
    public int numSamples = 10; // Number of samples to use for averaging
    private Vector3[] velocitySamples; // Array to store velocity samples
    private int sampleIndex = 0; // Index for the current sample

    void Start()
    {
        velocitySamples = new Vector3[numSamples];
        previousPosition = transform.position;
        fireParticleSystem = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        if (_isDead) return;
        if (!_isActive) return;
        // Store the current velocity sample
        velocitySamples[sampleIndex] = rb.velocity;
        
        // Increment sample index and loop back to 0 if necessary
        sampleIndex = (sampleIndex + 1) % numSamples;

        // Calculate the average velocity
        Vector3 averageVelocity = CalculateAverageVelocity();

        normVelocity = averageVelocity.magnitude;
        
        additionalAudio.volume = normVelocity;

        //TorchVelocity();
    }

    private void TorchVelocity()
    {
        deltaTime = Time.deltaTime;
        velocity = ((transform.position - previousPosition) / deltaTime).magnitude;
        normVelocity = Mathf.Clamp01(velocity / maxVelocity); // Normalized Velocity
        previousPosition = transform.position;
        //audio.pitch = 1 + normVelocity;
        additionalAudio.volume = Mathf.Lerp(normVelocity,0,0.0001f);
    }
    
    Vector3 CalculateAverageVelocity()
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 velocitySample in velocitySamples)
        {
            sum += velocitySample;
        }
        return sum / numSamples;
    }

    public void SetIsActive(bool value)
    {
        if(!_isDead)
            _isActive = value;
    }

    public void BlowOutTorch()
    {
        // Set Particle count to zero
        SetIsActive(false);
        _isDead = true;

        ParticleSystem[] subsystems = fireParticleSystem.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem subsystem in subsystems)
        {
            ParticleSystem.EmissionModule subsystemEmission = subsystem.emission;
            subsystemEmission.rateOverTime = 0;
        }

        audio.Stop();
        GetComponent<Animator>().SetTrigger("fadeOut");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isDead)
            return;

        switch (other.tag)
        {
            case "TorchIntensity":
                GetComponent<Animator>().SetTrigger("increaseIntensity");
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isDead)
            return;

        switch (other.tag)
        {
            case "TorchIntensity":
                GetComponent<Animator>().SetTrigger("decreaseIntensity");
                break;
        }
    }

}
