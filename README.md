# Poyo's Asset Cleaner

VRChat のアバター改変用 Unity プロジェクトで、どのアバターからも参照されていないアセットを探して片付ける Unity エディタ拡張です。

使い方: <https://ghp.maaaaa.net/Asset-Cleaner/>

## 動作環境

| 項目 | 要件 |
|---|---|
| Unity | 2022.3 |
| VRChat SDK | Avatars 3.7.0 以上（アバターの自動検出に使います。無くてもフォルダを手で登録すれば使えます） |
| OS | Windows（キャッシュ掃除は Windows のみ） |

## インストール

1. VCC / ALCOM に `https://vpm.maaaaa.net/index.json` を追加します。
2. プロジェクトの Manage Packages で「Poyo's Asset Cleaner」を追加します。

VCC を使わない場合は、[リリースページ](https://github.com/Poyotoron/Asset-Cleaner/releases)の `.unitypackage` をインポートしてください。

メニューの `Tools > Poyo's Asset Cleaner` でウィンドウが開きます。

## できること

- 登録したアバターやシーンから参照をたどり、使われていないアセットを一覧にする
- 選んだアセットを `.meta` ごとプロジェクト直下の `_UnusedAssets_日時/` へ移す（退避）。退避したものは元に戻せる
- スクリプト・シェーダー・ツールのフォルダなどを、未使用でも候補に出さない（保護）
- 内容が同じファイルを探す
- `Library/` などのキャッシュを削除する（Windows のみ）

## 不具合の報告

[GitHub Issues](https://github.com/Poyotoron/Asset-Cleaner/issues) に送ってください。ウィンドウの「情報 (About)」タブから、報告に使う環境情報をコピーできます。

## ライセンス

MIT License（[LICENSE](LICENSE)）
