using System;

namespace hbgr.InspectorConditions
{
    [Serializable]
    public abstract class ConditionItem
    {
        public abstract bool Evaluate();
    }
}