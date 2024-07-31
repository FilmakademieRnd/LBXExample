using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinoRestart : MonoBehaviour{

    void Awake(){
        DontDestroyOnLoad(gameObject);
    }
    void Start(){
        Invoke("ClearClients", 3f); // Clients should not see each other in credit scene (chapter 5)    
        Invoke("RestartMino", 20f);
        Invoke("Unload", 21f); 
    }

    private void ClearClients(){
        if (MinoGameManager.Instance.IsSpectator())
            MinoSpectator.instance.TrigggerFly();

        MinoGameManager.Instance.ClientCleanup();
    }
    private void RestartMino(){
        Debug.Log("----- Restarting Mino -----");
        SceneManager.LoadSceneAsync("Chapter_0");
    }

    private void Unload(){
        Scene scene = SceneManager.GetSceneByName("Chapter_5");
        if(scene.IsValid())
            SceneManager.UnloadSceneAsync("Chapter_5");

        Destroy(gameObject, 1f);
    }
}
