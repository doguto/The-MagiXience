# Warp.md

## プロジェクト概要

Magic-science-world は2DシューティングゲームのUnityプロジェクトです。

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

### アーキテクチャ

本プロジェクトではMVPアーキテクチャを採用しています。
詳細はArchitecture.mdを参照してください。

### コーディングルール

以下のコーディングルールに従って実装してください。

#### その他記法

- C#はデフォルトで`private`となるため、`private`修飾子は省略してください。
- コンストラクタは、型が明示な場合`new()`と省略した形で使用してください。
- 変数の宣言時には`var`を使用しても構いません。明示的に型を宣言しても構いません。

## 備考

Unityはmetaファイルが多く、レビューが困難です。編集は.csファイルのみに留め、以下の拡張子は編集しないでください。

- .meta
- .unity
- .asset
- .prefab
