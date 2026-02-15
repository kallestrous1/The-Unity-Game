using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Player detected by rangedetector", story: "[Player] detected by [rangedetector]", category: "Conditions", id: "1c5ae8f1041708691ce0cb2ed4f3df47")]
public partial class PlayerDetectedByRangedetectorCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<PlayerInRangeDetector> Rangedetector;

    public override bool IsTrue()
    {
        return Rangedetector.Value.playerInRange;

    }
}
