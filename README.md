# PlaylistDownLoader

[![Build and Release](https://github.com/denpadokei/PlaylistDownLoader/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/denpadokei/PlaylistDownLoader/actions/workflows/build-and-release.yml)

プレイリストを読み込み、含まれている曲のうち未所持のものを BeatSaver から自動でダウンロードする MOD です。曲はプレイリストのハッシュをもとに検索され、`Beat Saber_Data\CustomLevels` へ展開されます。

## 機能

- `Playlists` フォルダの `*.json` と `*.bplist` を走査し、ハッシュを持つ曲を列挙する
- 未所持の曲を BeatSaver API (`https://api.beatsaver.com`) で検索し、公開済みバージョンの ZIP をダウンロードして展開する
- 同時ダウンロードは 2 件までに制限し、ゲームプレイ中や曲読み込み中は待機する
- ダウンロード済みのハッシュを履歴に記録し、次回以降の再取得を防ぐ
- ダウンロード後に SongCore の曲リストを再読み込みし、プレイリストを更新する
- 進行状況をフローティングテキストに表示する (表示は 5 秒で消える)
- SyncSaber がインストールされている場合は処理を行わない

## 動作環境

| 項目 | 内容 |
| --- | --- |
| Beat Saber | 1.19.0 |
| 依存 MOD | BSIPA ^4.2.0 / SongCore ^3.5.0 / SiraUtil ^3.0.0 / BeatSaberPlaylistsLib ^1.4.0 / PlaylistManager ^1.2.0 |

## インストール

1. Beat Saber を 1.19.0 に合わせる (BSManager などのバージョン管理ツールを推奨)
2. BSIPA と上記の依存 MOD をインストールする
3. [Releases](https://github.com/denpadokei/PlaylistDownLoader/releases) から `PlaylistDownLoader-2.0.2-bs1.19.0.zip` をダウンロードし、`Plugins` フォルダをゲームフォルダへ上書きコピーする

## 使い方

1. プレイリストを `Playlists` フォルダに置きます (`*.json` / `*.bplist` の両方に対応)。
2. ゲームを起動してメニューを開くと、プレイリスト内の曲のチェックと不足曲のダウンロードが自動で始まります。
3. 状況はメニュー内のフローティングテキストに表示されます。ダウンロードされた曲は `Beat Saber_Data\CustomLevels` に保存されます。
4. ダウンロード済みのハッシュは `UserData\PlaylistDownLoader.txt` に記録されます。このファイルを削除すると履歴がリセットされ、次回起動時に再チェックされます。

SyncSaber を併用している場合、この MOD はダウンロード処理を行いません。

## ビルド

```powershell
git clone https://github.com/denpadokei/PlaylistDownLoader.git
cd PlaylistDownLoader
msbuild .\PlaylistDownLoader\PlaylistDownLoader.csproj /t:Build /p:Configuration=Release
```

プロジェクトは CodeTaskFactory を使う従来形式の targets を読み込むため、`dotnet build` ではなく Visual Studio または Developer PowerShell の msbuild でビルドしてください。`/p:BeatSaberDir="C:\Program Files (x86)\Steam\steamapps\common\Beat Saber"` のように `BeatSaberDir` へゲームのインストール先を指定すると参照アセンブリを解決できます (未指定の場合は `.\PlaylistDownLoader\Refs` が使われます)。

GitHub Actions の `build-and-release.yml` が push ごとにビルドし、`X.X.X` 形式のタグを push するとリリースを作成します。

## ライセンス

MIT
