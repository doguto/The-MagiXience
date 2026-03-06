using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IPlayerPositionProvider
    {
        Vector3 PlayerPosition { get; }
    }
}
