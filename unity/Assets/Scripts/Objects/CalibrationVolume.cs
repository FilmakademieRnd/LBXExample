using System.Collections;
using tracer;
using UnityEngine;
using UnityEngine.Events;

public class CalibrationVolume : MonoBehaviour
{
    private bool active = false;
    public GameObject indicator;
    public MinoPlayerCharacter player;
    private static readonly int Alpha = Shader.PropertyToID("_Alpha");
    public float dur = 1;
    public AudioSource calibrationFinished;
    public UnityEvent calibrationDone;

    private void Start()
    {
        /*if (indicator != null)
        {
            indicator.SetActive(false);
        }
        else
        {
            Debug.LogWarning("You forgot to link the indicator");
        }*/
    }

    private void Update()
    {
        if (!active) return;
        if (player == null) return;
        indicator.transform.position = new Vector3( indicator.transform.position.x, player.playerCalibration.playerCam.transform.position.y,  indicator.transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(MinoGameManager.Instance.IsSpectator()) 
            return;

        if (player != null)
            player.playerCalibration.ResetCalibration();
        
        if (other.CompareTag("Head"))
        {
            indicator.SetActive(true);
            player = other.GetComponentInParent<MinoPlayerCharacter>();
            //player.ResetCalibration();
            player.playerCalibration.measuring = true;
            Debug.Log("Player Entered");
            active = true;
            StopAllCoroutines();
            StartCoroutine(CalibrationTimer(dur));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (MinoGameManager.Instance.IsSpectator())
            return;

        if (other.CompareTag("Head"))
        {
            active = false;
            player.playerCalibration.calibrationReady = false;
            
            StopAllCoroutines();
            indicator.SetActive(false);
        }
    }

    private IEnumerator CalibrationTimer(float duration)
    {
        float evaluatedTime = 0;
        while (evaluatedTime < 1)
        {
            evaluatedTime += Time.deltaTime * (1/duration);
            yield return null;
        }
        player.playerCalibration.GetHeightReference();
        player.playerCalibration.StartCalibration();
    }

    public void CalibrationDone()
    {
        active = false;
        player.SetMeshVisibility(true);
        calibrationDone.Invoke();
    }
    
}