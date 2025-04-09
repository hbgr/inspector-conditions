using System;
using UnityEngine;

namespace hbgr.InspectorConditions
{
    [Serializable]
    public class ConditionGameObjectEquals : ConditionItem
    {
        private enum ConditionEquals
        {
            Equal,
            NotEqual
        }

        [SerializeField] private GameObject objectA;

        [SerializeField] private ConditionEquals _conditionEquals;

        [SerializeField] private GameObject objectB;

        public override bool Evaluate()
        {
            return _conditionEquals switch
            {
                ConditionEquals.Equal => objectA == objectB,
                ConditionEquals.NotEqual => objectA != objectB,
                _ => false
            };
        }
    }
}