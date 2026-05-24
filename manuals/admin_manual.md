# 管理者向け手順書 — 動画簡易変換ツール v0.1.0

| 項目 | 内容 |
|------|------|
| ツール名 | 動画簡易変換ツール |
| バージョン | v0.1.3（試作版） |
| 対象者 | DX推進担当・IT担当（配置・更新・障害対応担当） |
| 作成日 | 2026-05-23 |

---

## 1. ツール概要

### 動作環境

| 項目 | 要件 |
|------|------|
| OS | Windows 10 (20H2以降推奨) / Windows 11 |
| .NET ランタイム | .NET 8 Desktop Runtime（セルフコンテインド配布の場合は不要） |
| WebView2 Runtime | Microsoft Edge WebView2 Runtime（Windows 10 20H2以降・Windows 11は標準インストール済み） |
| ffmpeg.exe | 別途入手して配置が必要（詳細: `docs/ffmpeg_setup.md`） |
| ネットワーク | 不要（完全オフライン動作） |
| 管理者権限 | 不要（実行は一般ユーザー権限で可） |

---

## 2. 配置手順

### 2-1. 配布パッケージの構成

```
MovieConverter/
├── MovieConverter.exe         ← アプリ本体
├── bin/
│   └── ffmpeg/
│       ├── ffmpeg.exe         ← 別途入手して配置（必須）
│       ├── avcodec-*.dll      ┐
│       ├── avformat-*.dll     │ 配布元ZIPのDLL一式を
│       ├── avutil-*.dll       │ ffmpeg.exe と同じフォルダに配置
│       ├── swresample-*.dll   │ (ffmpeg.exe 単体では動作しない
│       ├── swscale-*.dll      │  ビルドがある)
│       └── その他のDLL        ┘
├── Assets/
│   └── player.html            ← プレイヤー用HTML（必須）
├── docs/
│   ├── ffmpeg_setup.md
│   └── license_notice.md
└── manuals/
    └── user_manual.md
```

### 2-2. 配置手順

1. 配布パッケージを任意のフォルダに展開する
   - 例: `C:\Tools\MovieConverter\`
   - 全利用者が使う場合は共有フォルダへの配置を検討する（ただし出力先は各自の端末内を推奨）

2. `docs/ffmpeg_setup.md` を参照し、`ffmpeg.exe` および同梱DLL一式を `bin\ffmpeg\` フォルダに配置する
   - `ffmpeg.exe` 単体だけでは動作しないビルドがある
   - 配布元ZIPのフォルダ構成を崩さずにコピーすること

3. コマンドプロンプトで動作確認を行う（**配布前に必ず実施**）

   ```bat
   bin\ffmpeg\ffmpeg.exe -version
   ```

   バージョン情報が表示されれば配置は正常。表示されない場合はDLL不足の可能性がある。

4. 動作確認を行う（`docs/test_scenarios.md` の正常系テスト）

4. 利用者に `manuals/user_manual.md` を配布する

---

## 3. ビルド手順（ソースからのビルド）

開発環境が必要な場合の手順です。

**必要なもの:**
- .NET 8 SDK
- NuGetパッケージ（インターネット接続のある環境でのリストア）: `Microsoft.Web.WebView2`

**ビルドコマンド:**

```bash
cd src/MovieConverter
dotnet restore
dotnet build -c Release
```

**セルフコンテインド単一ファイル発行（推奨）:**

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

発行後は `publish/MovieConverter.exe` が単一の実行ファイルとして生成されます。
この場合、.NET 8 ランタイムのインストールが不要になります。

---

## 4. アップデート手順

1. 旧バージョンの実行ファイルをバックアップする
2. 新バージョンのファイルを配置する
3. `bin\ffmpeg\ffmpeg.exe` は既存のものを引き継ぐ（更新が不要な場合）
4. 更新後に動作確認を行う
5. 利用者に更新を周知する

---

## 5. アンインストール手順

1. アプリフォルダを削除する
2. WebView2 のユーザーデータフォルダを削除する（必要な場合）
   - `%TEMP%\MovieConverter_WebView2\`

---

## 6. 権限・セキュリティ設定

| 項目 | 設定 |
|------|------|
| アプリの実行権限 | 一般ユーザー権限で可 |
| アプリフォルダの権限 | 読み取り実行（利用者は書き込み不要） |
| 出力先フォルダの権限 | 利用者が書き込み権限を持つフォルダ |
| ネットワーク通信 | 不要・外部通信なし |
| ウイルス対策ソフト | ffmpeg.exe が誤検知される場合は除外設定が必要な場合あり |

---

## 7. ライセンス管理

- ffmpeg.exe のライセンスを確認し、GPL/LGPL の区別を管理台帳に記録すること
- 詳細は `docs/license_notice.md` を参照すること
- ffmpeg.exe のバージョンと入手先URLを記録しておくこと

---

## 8. 障害対応

### 8-1. アプリが起動しない

| 確認事項 | 対応 |
|----------|------|
| .NET 8 Desktop Runtime がインストールされているか | https://dotnet.microsoft.com からインストール（セルフコンテインド配布の場合は不要） |
| `MovieConverter.exe` が「ブロックされている」状態でないか | ファイルのプロパティ→「ブロックの解除」チェックを確認 |
| Windowsイベントログにエラーがあるか | イベントビューアの「Windowsログ→アプリケーション」を確認 |

### 8-2. 動画プレビューが表示されない / 再生できない

| 確認事項 | 対応 |
|----------|------|
| WebView2 Runtime がインストールされているか | コントロールパネル→プログラムで「Microsoft Edge WebView2 Runtime」を確認 |
| `Assets\player.html` が存在するか | 配布パッケージに含まれているか確認 |
| WebView2 のユーザーデータフォルダが破損していないか | `%TEMP%\MovieConverter_WebView2\` を削除して再起動する |

#### MP4/AVC(H.264) なのにプレビューできない場合（v0.1.2改訂）

MP4/AVC/H.264/AACの動画であっても、WebView2のプレビューで再生できない場合があります。
これはAVC非対応ではなく、プレビュー読み込み方式との相性問題です。

**v0.1.2の対応:** file-uri方式（主）→ virtual-host方式（フォールバック）の自動切り替えを実装済み。ログで確認できます。

**ログ確認:**
- `[プレイヤー] file-uri方式で読み込みます` → 主方式で試行中
- `[読み込み成功] 方式: file-uri` → file-uri方式で成功
- `[フォールバック] file-uri方式が失敗しました。virtual-host方式を試みます。` → フォールバック発動
- `[読み込み成功] 方式: virtual-host` → フォールバック方式で成功
- `[プレイヤー エラー] プレビューで再生できませんでした。` → 両方式とも失敗

**両方式とも失敗した場合の切り分け手順:**

**Step 1 — Microsoft Edge での直接再生確認**

対象MP4をEdgeのウィンドウにドラッグ＆ドロップして再生できるか確認する。

- Edge で再生できない → 動画ファイル自体に問題がある可能性が高い
- Edge で再生できる → 下記 Step 2 以降を試す

**Step 2 — 30秒サンプルで確認**

```bash
ffmpeg -ss 00:10:00 -t 00:00:30 -i input.mp4 -c copy sample_30sec.mp4
```

サンプルで再生できる場合 → 長さ・容量が原因の可能性あり

**Step 3 — faststart 化で確認**

```bash
ffmpeg -i input.mp4 -c copy -movflags +faststart output_faststart.mp4
```

faststart化後に再生できる場合 → moov atomの末尾配置が原因だった

**現状の制約:** プレビューできない場合、利用者はカット位置を指定できないため変換が実行できません。自動faststart化はv0.2.0以降の検討事項です。

### 8-3. 変換に失敗する

| 確認事項 | 対応 |
|----------|------|
| `bin\ffmpeg\ffmpeg.exe` が存在し実行可能か | コマンドプロンプトで `bin\ffmpeg\ffmpeg.exe -version` を実行して確認 |
| ffmpeg.exe がウイルス対策ソフトにブロックされていないか | 除外設定を検討する |
| 出力先フォルダへの書き込み権限があるか | フォルダのアクセス権を確認する |
| ディスク容量が十分か | 空き容量を確認する |

#### 終了コード `-1073741515` が表示される場合

`ffmpeg.exe` は見つかっているが、必要なDLLが不足しています。

- `bin\ffmpeg\` フォルダに配布元ZIPのDLL一式（`avcodec-*.dll`、`avformat-*.dll`、`avutil-*.dll`、`swresample-*.dll`、`swscale-*.dll` 等）を配置してください
- `bin\ffmpeg\ffmpeg.exe -version` を実行してバージョン情報が表示されることを確認してください
- 表示されない場合は配布元のZIPから再度DLLを取り出して配置してください
- ライセンス・配布元・同梱ファイルの扱いは管理者が確認してください

---

## 9. 一時ファイルの管理

| ファイル | 場所 | 管理方法 |
|----------|------|---------|
| WebView2 ユーザーデータ | `%TEMP%\MovieConverter_WebView2\` | アプリが自動管理。問題時は手動削除可 |
| 変換途中ファイル | 入力ファイルと同フォルダ | キャンセル・失敗時はアプリが自動削除 |

---

## 10. ロールバック手順

1. バックアップした旧バージョンの実行ファイルを元の場所に戻す
2. 動作確認を行う

---

*作成: 2026-05-23  更新: 2026-05-24  バージョン: v0.1.4*
