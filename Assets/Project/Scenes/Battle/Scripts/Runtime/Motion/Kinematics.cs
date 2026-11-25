using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    public static class Kinematics
    {
        public static Vector2 Position(Vector2 start, Vector2 velocity, Vector2 acceleration, float time)
            => start + velocity * time + acceleration * (0.5f * time * time);
    }
}
