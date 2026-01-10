# Battleシーン インゲーム シーケンス図

## 概要

`Assets/Project/Scenes/Battle/Scripts` 配下のコードを分析し、バトルシーン全体の処理フローをシーケンス図としてまとめました。

---

## 全体フロー シーケンス図

```mermaid
sequenceDiagram
    autonumber
    participant Unity
    participant Presenter as BattleScenePresenter
    participant Repo as BattlePhaseSequenceRepository
    participant StateMachine as BattlePhaseStateMachine
    participant Sequence as BattlePhaseSequenceModel
    participant Phase as BattlePhaseModelBase<br/>(TimeLimitBattlePhaseModel)
    participant BindingMap as BattleTimelineBindingMap
    participant Director as PlayableDirector
    participant External as シナリオシーン

    Note over Unity,External: ゲーム開始
    Unity->>Presenter: Start() / InitializeAndStart()
    
    rect rgb(230, 245, 255)
        Note over Presenter,Repo: ステージ解決 & シーケンスロード
        Presenter->>Presenter: ResolveStageModel()
        Presenter->>Repo: Load(waySequenceAddress)
        Repo-->>Presenter: waySequence (BattlePhaseSequenceModel)
        Presenter->>Repo: Load(bossSequenceAddress)
        Repo-->>Presenter: bossSequence (BattlePhaseSequenceModel)
    end

    rect rgb(255, 245, 230)
        Note over Presenter,Director: 道中シーケンス再生
        Presenter->>StateMachine: PlaySequence(waySequence)
        StateMachine->>Sequence: Reset()
        
        loop 各フェーズを順番に処理
            StateMachine->>Sequence: TryMoveNext()
            Sequence-->>StateMachine: phase (BattlePhaseModelBase)
            StateMachine->>Phase: ResolveTimeline()
            Phase-->>StateMachine: TimelineAsset
            StateMachine->>Director: playableAsset = timeline
            StateMachine->>BindingMap: ApplyBindings(director, timeline)
            BindingMap->>Director: SetGenericBinding()
            StateMachine->>Director: Play()
            StateMachine->>Phase: Enter(director)
            Phase->>Phase: OnEnter() [タイマー開始]
            
            Note over Phase: 制限時間経過
            Phase-->>StateMachine: OnExitPhase (CompletePhase)
            StateMachine->>Phase: Exit()
        end
        
        StateMachine->>Director: Stop()
        StateMachine-->>Presenter: OnSequenceCompleted(Way)
    end

    rect rgb(245, 255, 230)
        Note over Presenter,External: 道中→ボス間シナリオ遷移
        Presenter->>Presenter: RequestScenario(scenarioIdWayToBoss)
        Presenter-->>External: OnScenarioRequested
        
        alt シナリオシーン連携 (未実装)
            External-->>Presenter: CompleteScenarioTransition(WayToBoss)
        else autoCompleteScenarioRequests = true
            Presenter->>Presenter: CompleteScenarioTransition(WayToBoss)
        end
    end

    rect rgb(255, 230, 230)
        Note over Presenter,Director: ボスシーケンス再生
        Presenter->>StateMachine: PlaySequence(bossSequence)
        StateMachine->>Sequence: Reset()
        
        loop 各フェーズを順番に処理
            StateMachine->>Sequence: TryMoveNext()
            Sequence-->>StateMachine: phase
            StateMachine->>Phase: ResolveTimeline()
            StateMachine->>Director: playableAsset = timeline
            StateMachine->>BindingMap: ApplyBindings()
            StateMachine->>Director: Play()
            StateMachine->>Phase: Enter(director)
            
            Note over Phase: 制限時間経過
            Phase-->>StateMachine: OnExitPhase
            StateMachine->>Phase: Exit()
        end
        
        StateMachine->>Director: Stop()
        StateMachine-->>Presenter: OnSequenceCompleted(Boss)
    end

    rect rgb(230, 255, 245)
        Note over Presenter,External: ボス→次ステージ シナリオ遷移
        Presenter->>Presenter: RequestScenario(scenarioIdBossToNext)
        Presenter-->>External: OnScenarioRequested
        
        alt シナリオシーン連携 (未実装)
            External-->>Presenter: CompleteScenarioTransition(BossToNextStage)
        else autoCompleteScenarioRequests = true
            Presenter->>Presenter: CompleteScenarioTransition(BossToNextStage)
        end
    end

    rect rgb(240, 240, 255)
        Note over Presenter: バトル完了処理
        Presenter->>Presenter: CompleteStage()
        Presenter->>Presenter: stageModel.Clear()
        Presenter-->>Unity: OnBattleCompleted
    end
```

---

## フェーズ内 Timeline再生 詳細

```mermaid
sequenceDiagram
    autonumber
    participant StateMachine as BattlePhaseStateMachine
    participant Phase as BattlePhaseModelBase
    participant Definition as BattlePhaseDefinition
    participant Builder as BattleTimelineBuilderAsset
    participant BindingMap as BattleTimelineBindingMap
    participant Director as PlayableDirector

    StateMachine->>Phase: ResolveTimeline()
    
    alt 静的Timeline設定あり
        Phase->>Definition: TryCreateTimeline()
        Definition-->>Phase: TimelineAsset (isRuntime=false)
    else 動的生成Builder設定あり
        Phase->>Definition: TryCreateTimeline()
        Definition->>Builder: BuildTimeline()
        Note over Builder: SignalTrack/AnimationTrack を<br/>メモリ上に動的生成
        Builder-->>Definition: TimelineAsset (isRuntime=true)
        Definition-->>Phase: TimelineAsset
    end
    
    Phase-->>StateMachine: resolvedTimeline
    
    StateMachine->>Director: playableAsset = resolvedTimeline
    StateMachine->>Director: time = 0
    StateMachine->>Director: Evaluate()
    StateMachine->>Director: extrapolationMode = Loop
    
    StateMachine->>BindingMap: ApplyBindings(director, timeline)
    
    loop timeline.outputs を列挙
        BindingMap->>BindingMap: TryGetBinding(trackName)
        BindingMap->>Director: SetGenericBinding(track, target)
    end
    
    StateMachine->>Director: Play()
```

---

## クラス構成図

```mermaid
classDiagram
    direction TB
    
    class BattleScenePresenter {
        -BattlePhaseStateMachine phaseStateMachine
        -BattlePhaseSequenceRepository sequenceRepository
        -StageModel stageModel
        -BattlePhaseSequenceModel waySequence
        -BattlePhaseSequenceModel bossSequence
        +IObservable~ScenarioTransitionRequest~ OnScenarioRequested
        +IObservable~Unit~ OnBattleCompleted
        +InitializeAndStart()
        +CompleteScenarioTransition(timing)
    }
    
    class BattlePhaseStateMachine {
        -PlayableDirector playableDirector
        -BattleTimelineBindingMap bindingMap
        -BattlePhaseSequenceModel activeSequence
        -BattlePhaseModelBase activePhase
        +IObservable~BattlePhaseModelBase~ OnPhaseStarted
        +IObservable~BattleSequenceType~ OnSequenceCompleted
        +PlaySequence(sequence)
        +Stop()
    }
    
    class BattlePhaseSequenceModel {
        -IReadOnlyList~BattlePhaseModelBase~ phases
        -int currentIndex
        +BattleSequenceType SequenceType
        +TryMoveNext() bool
        +Reset()
    }
    
    class BattlePhaseModelBase {
        <<abstract>>
        #BattlePhaseDefinition Definition
        #PlayableDirector Director
        +string PhaseId
        +IObservable~Unit~ OnExitPhase
        +ResolveTimeline() TimelineAsset
        +Enter(director)
        +Exit()
        #OnEnter()*
        #CompletePhase()
    }
    
    class TimeLimitBattlePhaseModel {
        #OnEnter()
    }
    
    class BattleTimelineBindingMap {
        -List~BindingEntry~ bindings
        +ApplyBindings(director, timeline)
    }
    
    class BattlePhaseSequenceAsset {
        <<ScriptableObject>>
        +BattleSequenceType SequenceType
        +IReadOnlyList~BattlePhaseDefinition~ Phases
    }
    
    class BattlePhaseDefinition {
        +string PhaseId
        +float TimeLimitSeconds
        +TimelineAsset TimelineAsset
        +BattleTimelineBuilderAsset TimelineBuilder
        +TryCreateTimeline()
    }
    
    BattleScenePresenter --> BattlePhaseStateMachine : uses
    BattleScenePresenter --> BattlePhaseSequenceModel : manages
    BattlePhaseStateMachine --> BattlePhaseSequenceModel : plays
    BattlePhaseStateMachine --> BattlePhaseModelBase : controls
    BattlePhaseStateMachine --> BattleTimelineBindingMap : uses
    BattlePhaseSequenceModel --> BattlePhaseModelBase : contains
    BattlePhaseModelBase <|-- TimeLimitBattlePhaseModel : extends
    BattlePhaseModelBase --> BattlePhaseDefinition : uses
    BattlePhaseSequenceAsset --> BattlePhaseDefinition : contains
```

---

## 状態遷移図

```mermaid
stateDiagram-v2
    [*] --> Idle: シーン開始
    
    state "道中フェーズ" as WayPhases {
        [*] --> WayPhase1: PlaySequence(waySequence)
        WayPhase1 --> WayPhase2: OnExitPhase (時間切れ)
        WayPhase2 --> WayPhaseN: OnExitPhase
        WayPhaseN --> [*]: TryMoveNext() = false
    }
    
    Idle --> WayPhases: InitializeAndStart()
    WayPhases --> ScenarioWayToBoss: OnSequenceCompleted(Way)
    
    state "シナリオ遷移 (道中→ボス)" as ScenarioWayToBoss {
        [*] --> WaitingScenario1: RequestScenario()
        WaitingScenario1 --> [*]: CompleteScenarioTransition()
    }
    
    state "ボスフェーズ" as BossPhases {
        [*] --> BossPhase1: PlaySequence(bossSequence)
        BossPhase1 --> BossPhase2: OnExitPhase
        BossPhase2 --> BossPhaseN: OnExitPhase
        BossPhaseN --> [*]: TryMoveNext() = false
    }
    
    ScenarioWayToBoss --> BossPhases: StartBossSequence()
    BossPhases --> ScenarioBossToNext: OnSequenceCompleted(Boss)
    
    state "シナリオ遷移 (ボス→次)" as ScenarioBossToNext {
        [*] --> WaitingScenario2: RequestScenario()
        WaitingScenario2 --> [*]: CompleteScenarioTransition()
    }
    
    ScenarioBossToNext --> Completed: CompleteStage()
    Completed --> [*]: OnBattleCompleted
```
