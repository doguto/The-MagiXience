# Magic-science-world（魔科セカ）

Unity 製 2D 弾幕シューティング。

- Unity `6000.0.59f2` / URP / Addressables / Input System
- ライブラリ: UniRx, UniTask, DOTween, ZString, TextMesh Pro

## Tools

### Skills

| Name | 用途 |
|------|------|
| `msw-battle-stage-builder` | Battle シーンのステージ・道中・ボス・弾幕・フェーズ追加。Battle 関連のアセット作業では最初に読む |
| `unity-mcp-skill` | Unity Editor を MCP で操作する際のベストプラクティス |

### MCP

| Name | Description |
|------|-------------|
| Serena MCP | C# のシンボル検索・参照検索・リファクタリング |
| Unity MCP | アセット/Prefab/シーン/ScriptableObject の作成・編集、コンパイルエラー確認（`read_console`）、Play 制御 |

## アーキテクチャ

MVP + Repository。詳細は `Document/Architecture.md`, `Document/Directory-Structure.md`。

- **Model**: データとロジック。MonoBehaviour 非継承のピュア C#
- **View**: 画面更新・入力。MonoBehaviour 継承、SerializeField でコンポーネントを持つ
- **Presenter**: Model と View の仲介。MonoBehaviour 継承
- **Repository**: `AssetRepository`（Addressables ロード）/ `ModelRepository`（ScriptableObject 等から Model を取得）

```
Assets/
  Project/
    Scripts/          # グローバル層。レイヤごとに asmdef (Model/View/Presenter/Repository/Infra/Extensions)
    Scenes/<Scene>/   # Battle, Title, StageList, Scenario, Global, SoundRoom, DemoClear, Entry
      Scripts/{Model,View,Presenter,Repository}/  # 各フォルダの .asmref でグローバル asmdef に所属
    Commons/          # シーン横断の共通 Prefab/Script/Shader
    DataStore/        # ScriptableObject データ（StageData, BgmData, SeData, Stage1〜4 の攻撃・移動プリセット等）
    Textures/         # Addressable 管理の画像
  Scripts/ZString/
Document/             # 設計ドキュメント
```

- `Global` シーンに SoundManager / SceneNavigator など常駐オブジェクトを置き、各シーンは加算ロードされる

## コードスタイル

- 名前空間: シーン固有は `Project.Scenes.<Scene>.Scripts.<Model|View|Presenter>`、グローバルは `Project.Scripts.<機能名>`
- ファイル名: `<機能名>Model.cs` / `<機能名>View.cs` / `<機能名>Presenter.cs`
- `private` 修飾子は省略する
- 型が明示されている場合のコンストラクタは `new()` を使う
- `[SerializeField]` フィールドに `readonly` を付けない
- 文字列結合は ZString、非同期は UniTask、イベントは UniRx を使う
- フォーマットは `.editorconfig` に従う

## 注意事項

- **`.asset` / `.prefab` / `.unity` / `.meta` を Edit/Write で直接編集・生成しない。** Unity MCP 経由で Editor を操作して変更する。テキスト編集してよいのは `.cs` のみ
- `[SerializeReference]` や配列サイズの変更は MCP の patch では作れないため `execute_code` で組み立てる。`execute_code` 内では `using` が使えないので完全修飾名で書く
- シリアライズ対象の型のフィールドを変更するときは、`.asset` だけでなく `.prefab` / `.unity` に埋め込まれた値も壊れるので全て検索して移行する
- Inspector でユーザーが手調整した値を想定値で上書きしない
- `.cs` を変更したら Unity MCP の `read_console` でコンパイルエラーがないことを確認する
- 自動テストは無い。動作確認は Unity Editor で Play して行う

## Git

- 作業ブランチから `dev` へ PR を出す
- コミットメッセージは `<Prefix>: <日本語の簡潔な説明>`。Prefix は `Feat` / `Fix` / `Docs` / `Style` / `Refactor` / `Perf` / `Test` / `Chore`
