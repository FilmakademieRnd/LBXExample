using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class MaterialControlBehaviour : PlayableBehaviour
{
    [SerializeField]
    public Vector2Int dimensions = Vector2Int.one;
    [SerializeField]
    public float inputWeight = 0;


    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        base.ProcessFrame(playable, info, playerData);

        Material material = playerData as Material;

        if (material != null) 
        {
            material.SetVector("_ColorTiling", Vector2.one / dimensions );
            int cells = dimensions.x * dimensions.y;
            float blendOffset = cells * inputWeight;

            float startOffsetV = 1.0f - 1.0f / dimensions.y;
            float u = (Mathf.FloorToInt(blendOffset) % dimensions.x) / (float)dimensions.x;
            float v = startOffsetV - (Mathf.FloorToInt(blendOffset / (float)dimensions.y)) / (float)dimensions.x;
            material.SetVector("_ColorOffset", new Vector2(u, v));
        }
    }
}
