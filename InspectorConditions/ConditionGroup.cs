using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace hbgr.InspectorConditions
{
    [Serializable]
    internal sealed class ConditionGroup : ConditionItem
    {
        internal enum ConditionGroupOperator
        {
            And,
            Or
        }

        [SerializeReference] private ConditionGroupOperator _operator;

        [SerializeReference] private List<ConditionItem> _conditions;

        public override bool Evaluate()
        {
            return _operator switch
            {
                ConditionGroupOperator.And => _conditions.All(c => c.Evaluate() == true),
                ConditionGroupOperator.Or => _conditions.Any(c => c.Evaluate() == true),
                _ => false
            };
        }

#if UNITY_EDITOR

        internal List<ConditionItem> editorConditions
        {
            get => _conditions;
            set => _conditions = value;
        }

        internal ConditionGroupOperator editorOperator => _operator;

#endif
    }
}