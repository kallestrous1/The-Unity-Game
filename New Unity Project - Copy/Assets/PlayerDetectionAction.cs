using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Player detection", story: "Check if [detector] found [player]", category: "Action", id: "0097f12a53597050994fb317cd956cb4")]
public partial class PlayerDetectionAction : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<AlertAreaScript> Detector;


    public override bool IsTrue()
    {
        return Detector.Value.playerDetected;
    }

 
}

