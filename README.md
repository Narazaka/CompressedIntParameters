# Compressed Int Parameters

同期Intパラメーターを最大値に合わせて圧縮するMA Parameters

## Install

### VCC用インストーラーunitypackageによる方法（おすすめ）

https://github.com/Narazaka/CompressedIntParameters/releases/latest から `net.narazaka.vrchat.compressed-int-parameters-installer.zip` をダウンロードして解凍し、対象のプロジェクトにインポートする。

### VCCによる方法

0. https://modular-avatar.nadena.dev/ja から「ダウンロード（VCC経由）」ボタンを押してリポジトリをVCCにインストールします。
1. https://vpm.narazaka.net/ から「Add to VCC」ボタンを押してリポジトリをVCCにインストールします。
2. VCCでSettings→Packages→Installed Repositoriesの一覧中で「Narazaka VPM Listing」にチェックが付いていることを確認します。
3. アバタープロジェクトの「Manage Project」から「Compressed Int Parameters」をインストールします。

## Usage

`Compressed Int Parameters` をAdd Componentし、`MA Parameters` と同じように使用します。

## Changelog

- 1.0.0
  - `CompressedParameterConfig.From(ParameterConfig parameter, int maxValue)` APIを追加
- 0.1.5
  - Changelogを追加
- 0.1.4
  - Avatar Menu Creator for MA と同じオブジェクトに付けた時に正しく動作しない問題を修正
- 0.1.3
  - IsLocalパラメーターが元から無い場合にエラーになる問題を修正
- 0.1.2
  - Avatar Optimizer対応
  - リモートで不要な動作をしないように
- 0.1.1
  - Plugin名を指定
- 0.1.0
  - リリース

## License

[Zlib License](LICENSE.txt)
