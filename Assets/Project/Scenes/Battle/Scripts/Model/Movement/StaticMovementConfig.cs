using System;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class StaticMovementConfig : IMovementConfig
    {
        public IMovementStrategy CreateStrategy()
        {
            return new StaticMovement();
        }
    }
}
