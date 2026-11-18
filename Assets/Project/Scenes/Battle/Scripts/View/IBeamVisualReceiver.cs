namespace Project.Scenes.Battle.Scripts.View
{
    /// <summary>
    /// 生成時に「線分の長さ」と「表示時間」を外から渡される必要があるViewが実装する。
    ///
    /// View アセンブリは Model を参照していないため AttackEvent を直接扱えない。
    /// Presenter が Instantiate 直後(=Startが走る前)に値を流し込む窓口として用意している。
    /// </summary>
    public interface IBeamVisualReceiver
    {
        /// <param name="range">線分の長さ(ワールド単位)。0以下なら未指定としてPrefabの値を使う</param>
        /// <param name="duration">表示時間(秒)。0以下なら未指定としてPrefabの値を使う</param>
        /// <param name="width">線の太さ(ワールド単位)。0以下なら未指定としてPrefabの値を使う</param>
        void ConfigureBeam(float range, float duration, float width = 0f);
    }
}
