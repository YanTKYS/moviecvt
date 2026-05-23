## v0.1.2
### Title
動画簡易変換ツール v0.1.2 — MP4/AVCプレビュー失敗時の案内改善・切り分け手順整備

### Note
### 変更内容

#### エラーメッセージ・ログ改善

- `player.html`: 再生不可エラー（コード4）のメッセージを、「H.264形式か確認」という誤解を招く表現から「プレビューで再生できませんでした」に変更。長時間・大容量動画での失敗可能性と利用者向け案内（別MP4で試す・Edge確認・管理者相談）を追加
- `MainForm.cs`: プレイヤーエラー時のログに切り分け3ステップ（Edge直接再生・30秒サンプル・faststart化）を出力するよう改善

#### ドキュメント整備

- `docs/test_scenarios.md`: MP4/AVCプレビュー失敗時の切り分け手順をセクション8として追加（ffmpegコマンド例付き）
- `docs/tool_design.md`: WebView2で再生できないMP4が存在することの設計判断を追記（4.5節）
- `README.md`: 「対応形式と注意事項」セクションを新設。長時間・大容量でのプレビュー失敗の可能性を明記
- `manuals/user_manual.md`: 「プレビューで再生できませんでした」エラーの対処手順を追加
- `manuals/admin_manual.md`: MP4/AVCでもプレビューできない場合の切り分け手順（Step1〜3）を追加

### 対応しなかった内容（v0.2.0以降の検討事項）

- 自動faststart化
- プレビュー用一時ファイル自動生成
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
