# キー設定機能の使用方法

## 概要

このドキュメントは、GlobalSceneに実装されたキー設定機能の使用方法について説明します。

## 機能説明

キー設定機能は、移動と攻撃のキー設定を管理する基盤システムです。設定はJSON形式で永続化され、ゲーム起動時に自動的に読み込まれます。

## アーキテクチャ

MVPパターンに従って実装されています：

### Infra層
- **KeyConfigData**: キー設定データを保持するデータクラス
  - 移動キー（上下左右）
  - 攻撃キー
  - デフォルト値の定義

### Model層
- **KeyConfigModel**: キー設定の操作を管理するモデルクラス
  - キー設定の取得
  - キー設定の更新
  - デフォルトへのリセット
  - キーの重複チェック

### Repository層
- **KeyConfigModelRepository**: KeyConfigModelのインスタンスを管理
  - Singletonパターンで実装
  - UserModelRepositoryに依存

## 基本的な使用方法

### GlobalScenePresenterからのアクセス

```csharp
// GlobalScenePresenterのインスタンスを取得
var globalScenePresenter = FindObjectOfType<GlobalScenePresenter>();

// KeyConfigModelを取得
var keyConfigModel = globalScenePresenter.KeyConfigModel;

// 現在のキー設定を取得
KeyCode moveUp = keyConfigModel.MoveUpKey;
KeyCode moveDown = keyConfigModel.MoveDownKey;
KeyCode moveLeft = keyConfigModel.MoveLeftKey;
KeyCode moveRight = keyConfigModel.MoveRightKey;
KeyCode attack = keyConfigModel.AttackKey;
```

### Repositoryから直接アクセス

```csharp
// KeyConfigModelRepositoryから取得
var keyConfigModel = KeyConfigModelRepository.Instance.Get();

// キー設定を取得
KeyCode moveUp = keyConfigModel.MoveUpKey;
```

## キー設定の変更

```csharp
var keyConfigModel = KeyConfigModelRepository.Instance.Get();

// 移動キーの設定
keyConfigModel.SetMoveUpKey(KeyCode.W);
keyConfigModel.SetMoveDownKey(KeyCode.S);
keyConfigModel.SetMoveLeftKey(KeyCode.A);
keyConfigModel.SetMoveRightKey(KeyCode.D);

// 攻撃キーの設定
keyConfigModel.SetAttackKey(KeyCode.Space);
```

**注意**: キー設定を変更すると自動的にJSONファイルに保存されます。

## キー設定のリセット

```csharp
var keyConfigModel = KeyConfigModelRepository.Instance.Get();

// デフォルト設定にリセット
keyConfigModel.ResetToDefault();
```

デフォルト設定：
- 上移動: ↑ (UpArrow)
- 下移動: ↓ (DownArrow)
- 左移動: ← (LeftArrow)
- 右移動: → (RightArrow)
- 攻撃: Z

## キーの重複チェック

```csharp
var keyConfigModel = KeyConfigModelRepository.Instance.Get();

// 特定のキーが既に使用されているかチェック
bool isUsed = keyConfigModel.IsKeyUsed(KeyCode.W);
```

## データの永続化

キー設定は `UserData.json` ファイルに保存されます：

### 保存場所
- **エディタモード**: `Assets/Project/DataStore/UserData.json`
- **ビルド後**: `Application.persistentDataPath/DataStore/UserData.json`

### JSON形式

```json
{
  "clearedStageNumber": 0,
  "keyConfigData": {
    "moveUpKey": 273,
    "moveDownKey": 274,
    "moveLeftKey": 276,
    "moveRightKey": 275,
    "attackKey": 122
  }
}
```

**注意**: KeyCodeは整数値として保存されます。

## 使用例：入力処理での利用

```csharp
public class PlayerController : MonoBehaviour
{
    KeyConfigModel keyConfigModel;

    void Start()
    {
        keyConfigModel = KeyConfigModelRepository.Instance.Get();
    }

    void Update()
    {
        // 設定されたキーで移動判定
        if (Input.GetKeyDown(keyConfigModel.MoveUpKey))
        {
            MoveUp();
        }
        if (Input.GetKeyDown(keyConfigModel.MoveDownKey))
        {
            MoveDown();
        }
        if (Input.GetKeyDown(keyConfigModel.MoveLeftKey))
        {
            MoveLeft();
        }
        if (Input.GetKeyDown(keyConfigModel.MoveRightKey))
        {
            MoveRight();
        }
        if (Input.GetKeyDown(keyConfigModel.AttackKey))
        {
            Attack();
        }
    }
}
```

## 今後の拡張予定

- OptionModalからのキー設定UI実装
- キー設定のバリデーション強化
- ゲームパッド対応
- キーバインドのプリセット機能

## トラブルシューティング

### キー設定が保存されない
- ファイルの書き込み権限を確認してください
- `DataStore` ディレクトリが存在するか確認してください

### キー設定が読み込まれない
- JSONファイルが破損していないか確認してください
- 必要に応じて `ResetToDefault()` を呼び出してください
