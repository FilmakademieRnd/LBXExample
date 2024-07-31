using System.Collections;
using UnityEngine;

public class IngameFPS : MonoBehaviour{

    /**********************
     *
     *  Shows an Ingame FPS Viz for performance testing
     *
     **********************/

    public TextMesh fpsText;

    private WaitForSeconds countWait;   //caching this, so its a bit more performant
    private float fpsCounter = 0f;
    
    void Start(){
        //Abort if not set up or if we are not within a development build
        if(fpsText == null || !IsDevelopmentBuild()){
            Destroy(this);
            return;
        }
        StartCoroutine(CountFPS());
    }

    private IEnumerator CountFPS(){
        yield return null;
        //fpsText.gameObject.layer = LayerMask.NameToLayer("Default");
        
        //Position it at the top right corner and just behind the near clip plane
        fpsText.transform.position = GetComponent<Camera>().ViewportToWorldPoint(new Vector3(0.95f, 0.95f, GetComponent<Camera>().nearClipPlane+1f));
        
        //cache it for more performance
        countWait = new WaitForSeconds(0.1f);
        while (true){
            fpsCounter = 1f / Time.unscaledDeltaTime;
            yield return countWait;
        }
    }

    private bool IsDevelopmentBuild(){
        #if UNITY_EDITOR
        return true;
        #endif
        return Debug.isDebugBuild || MinoGameManager.Ingame_Debug;
    }
    
    void Update(){
        fpsText.text = "FPS: " + fpsCounter.ToString("F2");
    }
}
