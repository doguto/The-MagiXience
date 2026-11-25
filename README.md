# Magic-science-world

魔法と科学が融合した世界を舞台にしたUnityゲームプロジェクトです。

## プロジェクト概要

このプロジェクトは、MVPアーキテクチャパターンを採用したUnity 2Dゲームです。モジュラー設計により、保守性と拡張性を重視した開発を行っています。

## 技術スタック

### 開発環境

- **Unity バージョン**: 6000.0.59f2
- **レンダリングパイプライン**: Universal Render Pipeline (URP)
- **アセット管理**: Addressable Asset System

### 使用ライブラリ

- **UniRx** - リアクティブプログラミング
- **UniTask** - 非同期処理の最適化
- **DOTween** - トゥイーンアニメーション
- **ZString** - 高速文字列処理（Assets/Scripts/ZString/に配置）
- **TextMesh Pro** - 高品質テキストレンダリング

## アーキテクチャ

プロジェクトは **MVP (Model-View-Presenter)** パターンに基づいて設計されています。

- **Model**: データとビジネスロジック（MonoBehaviour非継承）
- **View**: UI表示とユーザー入力の管理（MonoBehaviour継承）
- **Presenter**: ModelとViewの仲介、ゲームロジックの制御（MonoBehaviour継承）

詳細は [Architecture.md](Document/Architecture.md) をご覧ください。

## プロジェクト構造

```
Magic-science-world/
├── Assets/
│   ├── Project/           # メインプロジェクトアセット
│   │   ├── Commons/       # 共通アセット
│   │   ├── DataStore/     # ScriptableObject データ（ステージ・BGM・SE・BehaviorTree など）
│   │   ├── Scenes/        # シーン（Title, Battle, Scenario など）
│   │   ├── Scripts/       # グローバルスクリプト
│   │   └── Textures/      # キャラクター・背景画像 Addressable管理のもの
│   ├── Plugins/           # サードパーティライブラリ
│   └── Settings/          # プロジェクト設定
├── Document/              # プロジェクトドキュメント
└── Packages/              # Unity Package Manager設定
```

## シーン構成

- **Entry** - 起動シーン
- **Global** - 常駐シーン（SoundManager、SceneNavigator、共通モーダルなど）
- **Title** - タイトル画面
- **StageList** - ステージ選択画面
- **Battle** - 戦闘シーン（道中・ボス戦）
- **Scenario** - 会話シーン
- **SoundRoom** - サウンドルーム
- **DemoClear** - デモクリア画面

## ドキュメント

- [Architecture.md](Document/Architecture.md) - MVPアーキテクチャの詳細説明
- [Directory-Structure.md](Document/Directory-Structure.md) - ディレクトリ構造とファイル配置の詳細
- [Document.md](Document/Document.md) - シーン・主要クラス・データアセットのリファレンス
- [Battle/BehaviorTree.md](Document/Battle/BehaviorTree.md) - バトルの BehaviorTree の構造
- [Battle/BehaviorTree-Nodes.md](Document/Battle/BehaviorTree-Nodes.md) - BehaviorTree ノードリファレンス
- [Battle/DataStore.md](Document/Battle/DataStore.md) - バトルのデータ配置

## コーディング規約

### ネームスペース規則

```csharp
// シーン固有
Project.Scenes.[シーン名].Scripts.[Model|View|Presenter]

// シーン横断の共通機能
Project.Commons.[機能名].Scripts.[Model|View|Presenter]

// グローバル機能
Project.Scripts.[機能名]
```

### ファイル命名規則

- Model: `[機能名]Model.cs`
- View: `[機能名]View.cs`
- Presenter: `[機能名]Presenter.cs`
