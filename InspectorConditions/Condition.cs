using System;
using UnityEngine;

namespace hbgr.InspectorConditions
{
    [Serializable]
    public class Condition
    {
        [SerializeField] private ConditionGroup _conditions;

        public bool Evaluate()
        {
            return _conditions.Evaluate();
        }
    }
}