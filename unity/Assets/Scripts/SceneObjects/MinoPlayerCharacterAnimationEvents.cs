using System.Collections;
using System.Collections.Generic;
using tracer;
using UnityEngine;

public class MinoPlayerCharacterAnimationEvents : MonoBehaviour
{
    public MinoPlayerCharacter m_playerCharacter;
    public MinoSpectator m_spectator;   

    #region AnimationEvents
    public void EventBloodOverlay()
    {
        if(MinoGameManager.Instance.IsSpectator())
            return;

        float normalizedHealth = (float)m_playerCharacter.playerHealth / (float)m_playerCharacter.playerMaxHealth;
        if (normalizedHealth <= 1 && normalizedHealth > 0.75f)
        { // 100% -> 75% Health
            //Debug.Log("State1");
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetVector("_Offset", new Vector2(2.1f, -0.26f));
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetFloat("_Alpha", 0.2f);
        }
        else if (normalizedHealth <= 0.75f && normalizedHealth > 0.5f)
        { // 75% -> 50% Health
            //Debug.Log("State2");
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetFloat("_Alpha", 0.3f);
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetVector("_Offset", new Vector2(0.14f, 0.72f));
        }
        else if (normalizedHealth <= 0.5f && normalizedHealth > 0.25f)
        { // 50% -> 25% Health
            //Debug.Log("State3");
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetFloat("_Alpha", 0.4f);
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetVector("_Offset", new Vector2(1.58f, -0.26f));
        }
        else if (normalizedHealth <= 0.25f && normalizedHealth > 0.1f)
        { // 25% -> 10% Health
            //Debug.Log("State4");
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetFloat("_Alpha", 0.5f);

            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetVector("_Offset", new Vector2(2.1f, -0.55f));
        }
        else if (normalizedHealth <= 0.1f)
        { // 10% -> -x% Health
            //m_playerCharacter.headOverlay.GetComponent<Renderer>().material.SetVector("_Offset", new Vector2(2.58f, -0.55f));
        }

    }
    #endregion
}
