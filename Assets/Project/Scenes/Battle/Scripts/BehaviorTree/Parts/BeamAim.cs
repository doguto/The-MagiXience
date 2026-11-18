using System.Collections.Generic;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ビームが撃つ位置と向きを決めた結果。WarningLine が先に作って予測線を出し、同じものを子のビームに渡す
    public abstract class BeamAim
    {
        public readonly List<BeamLine> Lines = new();
    }
}
