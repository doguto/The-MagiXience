using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    public sealed class TransformBody : IMotionBody
    {
        public TransformBody(Transform transform) => Transform = transform;
        public Transform Transform { get; }

        public Vector3 Position
        {
            get => Transform.position;
            set => Transform.position = value;
        }

        public Quaternion Rotation
        {
            get => Transform.rotation;
            set => Transform.rotation = value;
        }
    }
}
