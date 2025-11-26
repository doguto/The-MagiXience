# Warp.md

## プロジェクト概要

Magic-science-world は2Dシューティングゲームのプロジェクトです。

## 技術資料

### 使用技術

* Unity
* C#
* UniRx
* UniTask
* DOTween
* ZString
* Addressable

その他READMEのUsedTechの欄に詳細は記載されています。

### コーディングルール

以下のコーディングルールに従って実装してください。

#### 命名規則

private変数: lowerCamelCase

#### その他記法

- C#はデフォルトでprivateとなるため、private修飾子は省略してください。
- コンストラクタは、型が明示な場合`new()`と省略した形で使用してください。
- 変数の宣言時には`var`を使用しても構いません。明示的に型を宣言しても構いません。
