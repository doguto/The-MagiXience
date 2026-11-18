using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// 起点→終点の向きを返す方向プロバイダ。
    /// IBeamLineProvider も兼ねるため、これを刺したエントリの弾は
    /// 発射口ではなく線分の起点から出て、終点に届いた時点で消える。
    /// </summary>
    [Serializable]
    public class BeamLineDirectionConfig : IDirectionProvider, IBeamLineProvider
    {
        [SerializeField] BeamLine line = new();

        public BeamLineDirectionConfig() { }

        public BeamLineDirectionConfig(BeamLine line)
        {
            this.line = line;
        }

        public BeamLine Line => line;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation) { }

        public Vector2 GetDirection() => line.Direction;

        public IDirectionProvider Clone() => new BeamLineDirectionConfig(line.Clone());
    }
}
