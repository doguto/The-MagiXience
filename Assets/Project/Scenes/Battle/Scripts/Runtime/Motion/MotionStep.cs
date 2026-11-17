using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    [Serializable]
    public abstract class MotionStep
    {
        public abstract MotionRunner CreateRunner();
        public virtual bool GrantsInvincibility => false;
        public virtual string Summary => null;
    }
}
