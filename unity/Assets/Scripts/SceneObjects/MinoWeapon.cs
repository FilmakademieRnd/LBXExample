using Autohand;
using System.Collections.Generic;
using System.Linq;
using tracer;
using UnityEngine;
using static RootMotion.Demos.CharacterThirdPerson;

public class MinoWeapon : MonoBehaviour
{
    [Header("Weapon | Settings")]
    public int weaponStrength = 20;
    private const int layerMinotaur = 24;
    public Grabbable grabbable;
    public AnimationCurve[] vibrationCurve = new AnimationCurve[1];
    //private MinoVibrationManager vibrationManager;

    // Weapon Material
    [SerializeField] private MeshRenderer weaponRenderer;
    [SerializeField] private Texture[] weaponTextureHurt = new Texture[0];
    private List<Material> weaponMaterials = new List<Material>();
    private int weaponTextureHurtState = -1;

    public void Awake()
    {
        weaponRenderer.GetMaterials(weaponMaterials);
        weaponMaterials[0] = Instantiate(weaponMaterials[0]);
        weaponRenderer.SetMaterials(new List<Material> { weaponMaterials[0], weaponMaterials[0] });
    }

    public bool AreWeHoldingThisWeapon(){
        return grabbable.GetHeldBy().Contains( MinoGameManager.Instance.m_playerCharacter.GetLeftHand()) ||
            grabbable.GetHeldBy().Contains( MinoGameManager.Instance.m_playerCharacter.GetRightHand());
    }
    public void ThisWeaponHitMino(){
        //ChangeWeaponTexture(GameObject.Find("Opponent").GetComponent<MinoEnemyRefactor>().health, GameObject.Find("Opponent").GetComponent<MinoEnemyRefactor>().maxHealth);
        MinoVibrationManager vm = MinoGameManager.Instance.IsSpectator() ? null : MinoGameManager.Instance.m_playerCharacter.GetComponent<MinoVibrationManager>();
        List<Hand> heldBy = grabbable.GetHeldBy();
        if (heldBy.Count != 0 && vm){ 
            bool rightHand = heldBy.Any(hand => hand.name == "VRHANDRIGHT");
            bool leftHand = heldBy.Any(hand => hand.name == "VRHANDLEFT");
            vm.TriggerVibrationFromCurve(vibrationCurve[0], leftHand, rightHand);
        }else if(vm){
            vm.TriggerVibrationFromCurve(vibrationCurve[0], false, true);
        }
        MinoGameManager.Instance.m_playerCharacter.SetBloodyHands();
    }


    private void ChangeWeaponTexture(int minotaurHealth, int minotaurMaxHealth)
    {
        float normalizedHealth = (float)minotaurHealth / (float)minotaurMaxHealth;

        if (weaponTextureHurt.Length == 0)
            return;

        int textureIndex;
        if (normalizedHealth <= 0.1f) // last 10% of health
        {
            textureIndex = weaponTextureHurt.Length - 1;
        }
        else
        {
            // Calculate the texture index based on the normalized health for the remaining range
            textureIndex = (int)((1 - normalizedHealth) * (weaponTextureHurt.Length - 1));
        }

        textureIndex = Mathf.Clamp(textureIndex, 0, weaponTextureHurt.Length - 1);

        if (weaponTextureHurtState != textureIndex)
        {
            weaponMaterials[0].SetTexture("_BaseMap", weaponTextureHurt[textureIndex]);
            weaponTextureHurtState = textureIndex;
        }
    }


}

