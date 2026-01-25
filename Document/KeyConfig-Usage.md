# キー設定機能の使用方法

## 概要

このドキュメントは、GlobalSceneに実装されたキー設定機能の使用方法について説明します。

本機能は**Unity Input System**を使用し、バインディングのオーバーライドとリバインディングをサポートします。

## 機能説明

キー設定機能は、Input Systemのバインディングをカスタマイズし、永続化する基盤システムです。設定はJSON形式で保存され、ゲーム起動時に自動的に読み込まれてInput Actionに適用されます。

Unity UIのEventSystemおよびNavigationと連携し、ButtonBaseなどの既存UIコンポーネントと統合されています。

## アーキテクチャ

MVPパターンに従って実装されています：

### Infra層
- **KeyConfigData**: バインディングオーバーライドを保存するデータクラス
  - アクション名
  - バインディングID
  - オーバーライドパス

### Model層
- **KeyConfigModel**: Input Systemのバインディング管理
  - バインディングのオーバーライド
  - オーバーライドの永続化
  - デフォルトへのリセット
  - Input Actionへのアクセス

### Repository層
- **KeyConfigModelRepository**: KeyConfigModelのインスタンスを管理
  - Singletonパターンで実装
  - InputActionAssetの初期化と管理

## 基本的な使用方法

### GlobalScenePresenterからのアクセス

```csharp
// GlobalScenePresenterのインスタンスを取得
var globalScenePresenter = FindObjectOfType<GlobalScenePresenter>();

// KeyConfigModelを取得
var keyConfigModel = globalScenePresenter.KeyConfigModel;

// Input Actionを取得
var moveAction = keyConfigModel.GetAction("Player/Move");
var attackAction = keyConfigModel.GetAction("Player/Attack");

// アクションの使用
moveAction.performed += ctx => {
    Vector2 movement = ctx.ReadValue<Vector2>();
    // 移動処理
};

attackAction.performed += ctx => {
    // 攻撃処理
};

// アクションの有効化
moveAction.Enable();
attackAction.Enable();
```

### Repositoryから直接アクセス

```csharp
// KeyConfigModelRepositoryから取得
var keyConfigModel = KeyConfigModelRepository.Instance.Get();

// 特定のアクションを取得
var moveAction = keyConfigModel.GetAction("Player/Move");
```

## バインディングのカスタマイズ

### 個別バインディングの変更

```csharp
var keyConfigModel = KeyConfigModelRepository.Instance.Get();

// "Player/Move"アクションの特定のバインディングを変更
// 例: 上移動キーをWキーに変更
keyConfigModel.SetBindingOverride("Player/Move", bindingIndex: 2, "<Keyboard>/w");

// 攻撃キーをSpaceキーに変更
keyConfigModel.SetBindingOverride("Player/Attack", bindingIndex: 0, "<Keyboard>/space");
```

**注意**: バインディングの変更は自動的にJSONファイルに保存されます。

### インタラクティブなリバインディング

Input SystemのRebindingAPIを使用した例：

```csharp
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

public async UniTask RebindKey(string actionName, int bindingIndex)
{
    var keyConfigModel = KeyConfigModelRepository.Instance.Get();
    var action = keyConfigModel.GetAction(actionName);
    
    // アクションを一時的に無効化
    action.Disable();
    
    // リバインディング操作を開始
    var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
        .OnComplete(operation => 
        {
            // 新しいバインディングを保存
            var newPath = action.bindings[bindingIndex].effectivePath;
            keyConfigModel.SetBindingOverride(actionName, bindingIndex, newPath);
            
            operation.Dispose();
            action.Enable();
        })
        .Start();
        
    // キャンセル処理
    await UniTask.WaitUntil(() => rebindOperation.completed);
}
```

## バインディングのリセット

### すべてのバインディングをデフォルトに戻す

```csharp
var keyConfigModel = KeyConfigModelRepository.Instance.Get();
keyConfigModel.ResetAllBindings();
```

### 特定アクションのバインディングをデフォルトに戻す

```csharp
var keyConfigModel = KeyConfigModelRepository.Instance.Get();
keyConfigModel.ResetActionBindings("Player/Move");
```

## Unity UIとの統合

本システムはUnity UIのEventSystemおよびNavigationと連携します。

### EventSystemとの連携

Input SystemのUI Input Moduleを使用することで、既存のButtonBaseやNavigationが自動的にカスタムキーバインドに対応します：

```csharp
// EventSystemにInput System UI Input Moduleがアタッチされている場合
// ButtonBaseやNavigationは自動的にカスタムキーバインドを認識
```

### ゲームプレイでの使用例

```csharp
public class PlayerController : MonoBehaviour
{
    InputAction moveAction;
    InputAction attackAction;

    void Start()
    {
        var keyConfigModel = KeyConfigModelRepository.Instance.Get();
        
        // アクションを取得
        moveAction = keyConfigModel.GetAction("Player/Move");
        attackAction = keyConfigModel.GetAction("Player/Attack");
        
        // イベント登録
        moveAction.performed += OnMove;
        attackAction.performed += OnAttack;
        
        // 有効化
        moveAction.Enable();
        attackAction.Enable();
    }

    void OnMove(InputAction.CallbackContext context)
    {
        Vector2 movement = context.ReadValue<Vector2>();
        // 移動処理
    }

    void OnAttack(InputAction.CallbackContext context)
    {
        // 攻撃処理
    }

    void OnDestroy()
    {
        // クリーンアップ
        moveAction.performed -= OnMove;
        attackAction.performed -= OnAttack;
    }
}
```

## データの永続化

バインディングオーバーライドは `UserData.json` ファイルに保存されます：

### 保存場所
- **エディタモード**: `Assets/Project/DataStore/UserData.json`
- **ビルド後**: `Application.persistentDataPath/DataStore/UserData.json`

### JSON形式

```json
{
  "clearedStageNumber": 0,
  "keyConfigData": {
    "bindingOverrides": [
      {
        "actionName": "Player/Move",
        "bindingId": "e2062cb9-1b15-46a2-838c-2f8d72a0bdd9",
        "overridePath": "<Keyboard>/w"
      },
      {
        "actionName": "Player/Attack",
        "bindingId": "6c2ab1b8-8984-453a-af3d-a3c78ae1679a",
        "overridePath": "<Keyboard>/space"
      }
    ]
  }
}
```

## 利用可能なアクション

`InputSystem_Actions.inputactions`で定義されているアクション：

- **Player/Move**: Vector2型の移動入力
- **Player/Attack**: 攻撃ボタン
- **Player/Next**: UI次項目選択
- **Player/Previous**: UI前項目選択
- その他、Jump、Sprint、Interact、Crouchなど

## 今後の拡張予定

- OptionModalからのリバインディングUI実装
- バインディングの競合検出と警告
- キーバインドのプリセット機能
- ゲームパッド対応の強化

## トラブルシューティング

### バインディングが保存されない
- ファイルの書き込み権限を確認してください
- `DataStore` ディレクトリが存在するか確認してください

### バインディングが読み込まれない
- JSONファイルが破損していないか確認してください
- 必要に応じて `ResetAllBindings()` を呼び出してください

### InputActionAssetが見つからない
- GlobalScenePresenterにInputActionAssetが正しくアサインされているか確認してください
- Resources フォルダに `InputSystem_Actions` が配置されているか確認してください

## 参考資料

- [Unity Input System Documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/index.html)
- [Input System Rebinding](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/ActionBindings.html#runtime-rebinding)
