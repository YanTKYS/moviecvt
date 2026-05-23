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
