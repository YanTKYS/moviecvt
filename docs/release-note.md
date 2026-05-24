## v0.1.6
### Title
動画簡易変換ツール v0.1.6 — 試作版配布前整備（ドキュメント・チェックリスト）

### Note
### 変更内容

#### ドキュメント整備（コード変更なし）

- **`README.md`**: 推奨フォルダ構成を詳細化（DLL配置の強調）。よくあるトラブル一覧を追加（ffmpeg未配置・DLL不足・WebView2不足・プレビュー失敗・変換遅延・高速カットずれ・出力先など）
- **`manuals/admin_manual.md`**: バージョンを v0.1.6 に更新。「2-3. 初回確認チェックリスト」を追加（10項目）。「8-4. 変換に時間がかかる」「8-5. 高速カットで開始位置がずれる」の障害対応を追加
- **`manuals/user_manual.md`**: バージョンを v0.1.6 に更新。先頭に「目的別ガイド」（やりたいこと→推奨設定の表）を追加。高速カット・圧縮変換・音量スライダーの注意事項を簡潔にまとめた説明を追加。MP4以外は対象外であることを明記
- **`development_report.md`**: v0.1.6 対応内容と v0.2.0 以降の機能候補一覧を追加

---

## v0.1.5
### Title
動画簡易変換ツール v0.1.5 — 変換中の進捗表示追加・ログ簡略化

### Note
### 変更内容

#### 変換中の進捗表示

- **`FfmpegRunner.cs`**: ffmpegのstderr出力から `time=HH:MM:SS.ss` を正規表現でパース。進捗率（0.0〜1.0）をコールバックで通知。進捗行はログ出力をスキップし代わりにコールバックに渡す
- **`MainForm.cs`**: `ProgressBar` を変換パネル上部に追加。進捗率が取得できるまでマーキー表示、取得開始後は0〜100%の確定表示に切り替わる
- **`MainForm.cs`**: ステータス欄に「状態: 変換中... 経過時間 00:03:25  45%」を1秒ごとに更新（WinForms Timerを使用）
- **`MainForm.cs`**: 完了ログに所要時間を追加（`所要時間: 00:05:12`）

#### ログ簡略化

- **`MainForm.cs`**: 変換中の画面ログを一般利用者向けに簡略化。「変換中です。しばらくお待ちください。」と注意事項のみ表示
- **`MainForm.cs`**: ffmpegの生出力（ストリーム情報・進捗行）は内部バッファに蓄積し画面には表示しない。変換失敗時のみ詳細ログをダンプして確認できるようにする

#### バージョン更新

- `MovieConverter.csproj`: バージョン 0.1.4.0 → 0.1.5.0 / v0.1.4 → v0.1.5
- ウィンドウタイトル: v0.1.4 → v0.1.5

---

## v0.1.4
### Title
動画簡易変換ツール v0.1.4 — 全体変換自動判定・D&D修正・プレビュー音量追加

### Note
### 変更内容

#### 全体変換の自動判定

- **`ConversionSettings.cs`**: `ConversionMode`（RangeOnly/FullVideo）を追加
- **`FfmpegRunner.cs`**: `FullVideo` 時は `-ss`/`-to` を付加しない
- **`MainForm.cs`**: 動画ロード時に開始=0・終了=動画時間を自動設定。変換実行時に開始≈0かつ終了≈動画時間の場合は自動的にFullVideoモード（-ss/-to なし）を選択。ユーザーがUIで範囲を変更すれば自動的にRangeOnlyモードになる

#### D&D時の挙動修正

- **`MainForm.cs`**: `CoreWebView2.Settings.AllowExternalDrop = false` でWebView2ブラウザ側のドロップ処理を無効化し、WinForms OLE DragDropハンドラに委譲。`webView2`・`pnlPreview` にもWinFormsのDragEnter/DragDropを登録。`NavigationStarting` によるMP4ナビゲーションキャンセルは安全網として継続

#### プレビュー音量スライダー追加

- **`MainForm.cs`**: 音量スライダー（TrackBar）と消音ボタンをプレビューコントロールバーに追加（出力動画の音量は変更しない）
- **`player.html`**: `volume`/`mute` コマンドを追加

#### 変換時間に関するドキュメント整理

- 高速カット（-c copy）と圧縮変換（libx264）の速度差の理由を docs に記載
- `-preset fast`/`veryfast`、プログレスバーをv0.1.5以降の候補として記録

---

## v0.1.3
### Title
動画簡易変換ツール v0.1.3 — 高速カット追加・ffmpeg DLL配置手順追記

### Note
### 変更内容

#### 高速カット機能の追加（主要変更）

実機確認で、長時間動画（数時間の会議録画等）の変換に非常に長い時間がかかることを確認。
既存の実装では「解像度: 元のまま」を選んでも常に再エンコードしていたため、高速カット方式を追加した。

- **`ConversionSettings.cs`**: `QualityPreset` 列挙型に `FastCut` を追加（値0）
- **`FfmpegRunner.cs`**: `FastCut` 選択時は `-c copy` を使用、解像度フィルタ・再エンコード処理をスキップ
- **`MainForm.cs`**: 圧縮設定コンボに「しない（高速カット）」を追加し初期値に設定。高速カット時は解像度コンボを無効化。ヒントラベルで方式の特徴を表示。変換ログに「出力方式: 高速カット（再エンコードなし）」または「圧縮変換（再エンコードあり）」を記録
- `v0.1.3` タイトル・バージョン更新

#### 高速カットの仕様

| 項目 | 内容 |
|------|------|
| ffmpegオプション | `-c copy` |
| 処理速度 | 高速（再エンコードなし） |
| カット精度 | 直前のキーフレームに丸められる（数秒のずれあり） |
| 解像度変更 | 不可（元のまま固定） |
| 用途 | 長時間動画・速さ優先 |

#### ffmpeg DLL 配置手順の明確化（ドキュメントのみ）

実機確認で、`ffmpeg.exe` 単体だけを配置した場合に終了コード `-1073741515`（DLL不足）が返る事象を確認。
配布元ZIPのDLL一式を `bin\ffmpeg\` フォルダに配置する必要があることを各ドキュメントに追記した。

#### 対応しなかった内容（v0.2.0以降の検討事項）

- 高速カット後の自動faststart化
- `-c copy` 時の音声ずれ対応
- カット精度モード切替

---

## v0.1.2
### Title
動画簡易変換ツール v0.1.2 — MP4/AVCプレビュー失敗時の案内改善・切り分け手順整備

### Note
### 変更内容

#### WebView2 プレビュー読み込み方式の改善（主要変更）

実機確認で、Microsoft Edgeでは再生できるMP4/AVC動画が本ツールのプレビューで再生できない問題が確認された。原因はコーデック非対応ではなく、旧方式（仮想ホスト経由）の読み込み方式との相性と判断。

- **`MainForm.cs`**: プレビュー読み込みを `https://video.local/` 仮想ホスト方式から `file://` URI 方式（主）+ 仮想ホスト方式（フォールバック）に変更
- **`MainForm.cs`**: file-uri方式が失敗した場合、player.htmlを仮想ホストコンテキストに再ナビゲートしてvirtual-host方式で再試行するフォールバック機能を実装
- **`MainForm.cs`**: 読み込み成功・失敗時にどの方式が使われたかをログに記録
- **`player.html`**: 読み込み方式（method）をC#に送信。エラーメッセージを「Edgeで再生できる動画でも、この画面では再生できない場合があります」に改善

#### ドキュメント整備

- `docs/test_scenarios.md`: 読み込み方式確認とEdge再生確認の切り分け手順をセクション8に追加
- `docs/tool_design.md`: セクション4.5をfile-uri主方式/virtual-hostフォールバックの設計判断で改訂
- `README.md`: プレビュー方式と制約（プレビュー不可時は変換不可）を明記
- `manuals/user_manual.md` / `admin_manual.md`: 制約とログ確認方法を更新
- `docs/release_checklist.md`: 読み込み方式確認チェック項目を追加

### 対応しなかった内容（v0.2.0以降の検討事項）

- 自動faststart化
- プレビュー用一時ファイル自動生成
- プレビュー不可MP4のカット位置指定・変換
- MP4以外の形式対応

---

## v0.1.1
### Title
動画簡易変換ツール v0.1.1 — バグ修正・配布準備

### Note
### バグ修正

| # | 修正箇所 | 内容 |
|---|----------|------|
| 1 | `FfmpegRunner.cs` | 単一ファイル発行時の ffmpeg.exe パス解決を `Environment.ProcessPath` ベースに修正 |
| 2 | `MainForm.cs` | WebView2 初期化完了前にファイル選択した場合、初期化完了後に自動再ロードするよう修正 |
| 3 | `MainForm.cs` | WebView2 の appDir 解決を `Environment.ProcessPath` ベースに統一（単一ファイル発行対応） |
| 4 | `MainForm.cs` | 変換キャンセル・例外発生時に `_cancelSource` を確実に Dispose するよう修正 |
| 5 | `MainForm.cs` | ファイル読み込み時に `_isPlaying` をリセットし再生ボタン表示を更新するよう修正 |
| 6 | `player.html` | 動画読み込みエラー時にエラーメッセージを表示し C# 側へ通知するよう修正 |

### 配布物

`MovieConverter_v0.1.1_win-x64.zip`

```
MovieConverter/
  MovieConverter.exe        # 単体実行可能（.NET Desktop Runtime 不要）
  Assets/
    player.html
  bin/ffmpeg/
    README.txt              # ffmpeg.exe 配置案内
  docs/
    ffmpeg_setup.md
    license_notice.md
  manuals/
    user_manual.md
```

### 動作要件

- Windows 10 / 11 (x64)
- Microsoft Edge WebView2 Runtime（Windows 11 は標準搭載）
- ffmpeg.exe（別途入手し `bin\ffmpeg\` に配置）
