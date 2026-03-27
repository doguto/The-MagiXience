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
│   │   ├── Scenes/        # シーン（Title, BattleWay, BattleBoss, など）
│   │   ├── Scripts/       # グローバルスクリプト
│   │   └── Textures/      # キャラクター・背景画像 Addressable管理のもの
│   ├── Plugins/           # サードパーティライブラリ
│   └── Settings/          # プロジェクト設定
├── Document/              # プロジェクトドキュメント
└── Packages/              # Unity Package Manager設定
```

## シーン構成

- **Title** - タイトル画面
- **StageList** - ステージ選択画面
- **Battle** - 戦闘シーン
- **Scenario** - シナリオ・ノベルパートシーン
- **Global** - グローバル管理シーン
  - ユーティリティGameObject等を配置する
    - SoundManager
    - SceneNavigator
    - etc

## ドキュメント

- [Architecture.md](Document/Architecture.md) - MVPアーキテクチャの詳細説明
- [Directory-Structure.md](Document/Directory-Structure.md) - ディレクトリ構造とファイル配置の詳細

## コーディング規約

### ネームスペース規則

```csharp
// シーン固有
Project.Scenes.[シーン名].Scripts.[Model|View|Presenter]

// 共通機能
Project.Commons.Scripts.[機能名]

// グローバル機能
Project.Scripts.[機能名]
```

### ファイル命名規則

- Model: `[機能名]Model.cs`
- View: `[機能名]View.cs`
- Presenter: `[機能名]Presenter.cs`
