using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class MaterialControlClip : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    private MaterialControlBehaviour _materialControlBehaviour = new MaterialControlBehaviour();

    public ClipCaps clipCaps
    {
        get
        {
            return ClipCaps.Blending;
        }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<MaterialControlBehaviour>.Create(graph, _materialControlBehaviour);
    }
}
