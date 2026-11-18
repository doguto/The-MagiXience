namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// AttackTimelineEntry の directionProvider スロットに刺さりつつ、
    /// 方向だけでなくビームの起点・射程まで伝えるプロバイダの目印。
    ///
    /// AttackTimeline がこの目印を見て弾イベントの発射位置をワールド座標に差し替えるので、
    /// SingleBulletSignal / NWaySignal のような既存シグナルは無改造のままビーム化できる。
    /// </summary>
    public interface IBeamLineProvider
    {
        BeamLine Line { get; }
    }
}
