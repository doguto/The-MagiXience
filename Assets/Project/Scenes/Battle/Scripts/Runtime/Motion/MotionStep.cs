using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    [Serializable]
    public abstract class MotionStep
    {
        public abstract MotionRunner CreateRunner();
        public virtual bool GrantsInvincibility => false;
        public virtual string Summary => null;

        public static string Describe(IReadOnlyList<MotionStep> steps) =>
            steps == null ? "" : string.Join(" → ", steps.Where(s => s != null).Select(s => s.Summary ?? s.GetType().Name.Replace("Motion", "")));
    }
}
