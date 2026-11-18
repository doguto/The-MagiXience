using System;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scripts.Model;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ステージツリーの進み具合。Section の開始をシーン側 (BGM・背景・リトライ地点) へ知らせる
    public sealed class StageProgress
    {
        public int StartSection { get; set; }
        // StartSection をリトライなどで途中から始めたか。会話を飛ばすかの判断に使う
        public bool ResumeStart { get; set; }

        public int CurrentSection { get; private set; } = -1;
        public BattleSituation CurrentSituation { get; private set; }
        public bool CurrentSectionResumed { get; private set; }

        readonly Subject<(int Index, BattleSituation Situation, bool Resumed)> sectionStarted = new();

        public IObservable<(int Index, BattleSituation Situation, bool Resumed)> OnSectionStarted => sectionStarted;

        public void Enter(int index, BattleSituation situation, bool resumed)
        {
            CurrentSection = index;
            CurrentSituation = situation;
            CurrentSectionResumed = resumed;
            sectionStarted.OnNext((index, situation, resumed));
        }
    }
}
