## v0.4.2
### Title
動画簡易変換ツール v0.4.2 — 動作確認機能追加・起動時ログ強化

### 変更内容

#### 新機能: 動作確認（環境診断）

ログ欄の上部に「動作確認」ボタンを追加した。
ボタンを押すと以下の項目を自動確認し、OK / 注意 / NG で結果を表示する。

| 確認項目 | 内容 |
|----------|------|
| アプリバージョン | インストールされているバージョンを表示 |
| WebView2 Runtime | インストール済み / 未インストール |
| ffmpeg.exe | 配置済み / 未配置 |
| ffmpeg 起動確認 | 正常 / DLL不足 / 起動失敗 |
| ffprobe.exe | 配置済み / 未配置 |
| ffprobe 起動確認 | 正常 / DLL不足 / 起動失敗 |
| ログフォルダ書き込み | 書き込み可 / 書き込み不可 |
| 選択中の動画ファイル | 存在する / 見つかりません（ファイル選択時のみ） |
| 出力先フォルダ書き込み | 書き込み可 / 書き込み不可（ファイル選択時のみ） |

問題がある場合は次のアクションを表示する。

**診断結果の保存:**
ダイアログ内「診断結果を保存」ボタンで `logs/diagnostic_{yyyyMMdd_HHmmss}.txt` に保存できる。管理者への問い合わせ時に添付できる。

#### 機能強化: 起動時ログ

起動時に以下の情報を `logs/startup_latest.log` に自動記録する（画面には表示しない）。

| 記録項目 |
|----------|
| 起動日時 |
| アプリバージョン |
| 実行フォルダ |
| OS情報 |
| WebView2 Runtime インストール状況 |
| ffmpeg.exe 配置状況 |
| ffprobe.exe 配置状況 |

---

## v0.4.1
### Title
動画簡易変換ツール v0.4.1 — WebView2未インストール時のフリーズ修正

### Note
### 変更内容

#### 不具合修正: WebView2未インストール環境でのフリーズ

WebView2ランタイム（Microsoft Edge WebView2 Runtime）が未インストールの環境で動画ファイルを読み込んだ際、アプリが「動画を読み込み中...」のまま応答しなくなる問題を修正した。

**修正前の動作:**
- WebView2の初期化が失敗してもMainFormへの通知が行われず、変換ボタンも有効にならない

**修正後の動作:**
- WebView2が使えないことを検出し、即座に「プレビュー不可モード」へ移行
- ffprobeが動画の長さを取得次第、開始・終了位置を自動設定
- 「変換実行」ボタンが有効になり、変換を実行できる

#### プレビュー不可モードの動作

| 状況 | 動作 |
|------|------|
| WebView2未インストール + ffprobe配置済み | 動画の長さを取得して開始・終了位置を自動設定。変換実行可能 |
| WebView2未インストール + ffprobe未配置 | 開始・終了位置のテキストボックスを有効化。手入力で変換実行可能 |

> **注意:** WebView2がないとプレビュー（動画の確認・シーク）はできません。
> カット位置を確認したい場合は、別途 Microsoft Edge 等で動画を確認してから開始・終了位置を手入力してください。
> WebView2のインストールは管理者に相談してください。

---

## v0.4.0
### Title
動画簡易変換ツール v0.4.0 — 変換結果表示・設定説明・エラー診断・ログ保存の改善

### Note
### 変更内容

#### 変換結果表示の改善

変換完了時のダイアログを `MessageBox` からカスタムダイアログに変更した。

- 出力ファイル名・出力先フォルダを表示
- 変換前・変換後のファイルサイズを並べて表示
- 削減率（%）を色付きで表示（削減: 緑、増加: オレンジ）
- 処理時間を表示
- 「出力フォルダを開く」ボタン: エクスプローラーで出力先フォルダを開く
- 「再生」ボタン: 関連付けられたプレイヤーで出力ファイルを再生

#### 設定説明の改善

圧縮設定の選択ごとに表示されるヒントテキストを全5設定分に整備した。

| 設定 | 表示テキスト |
|------|------------|
| しない（高速カット） | 速い。画質は変わりません。開始位置が少しずれる場合があります。 |
| 速度優先 | 変換時間を短くします。画質は少し下がる場合があります。 |
| 画質優先 | 画質を保ちます。容量はあまり小さくならない場合があります。 |
| 標準 | 迷ったときの設定です。 |
| 容量優先 | 容量を小さくします。画質は下がる場合があります。 |

#### エラー診断の改善

- ffmpeg未配置メッセージを改善（「配置先:」「→ docs/ffmpeg_setup.md を参照」を明示）
- ffprobe未配置時のログ注意メッセージを追加（変換は可能である旨も表示）
- 変換失敗時にエラー原因を自動診断するメソッド（`DiagnoseConversionError`）を追加
  - DLL不足（終了コード 0xC0000135）を終了コードで検出
  - 書き込み権限拒否・ディスク容量不足・ファイル破損をログ文字列から検出

#### ログ保存ボタンの追加

ログ欄上部に「ログを保存」ボタンを追加した。

- 保存先: 入力ファイルと同じフォルダ優先（未選択時は `{exeフォルダ}\logs\`）
- ファイル名: `movieconverter_log_{yyyyMMdd_HHmmss}.txt`
- 保存後にフォルダを開く確認ダイアログを表示

---

## v0.3.3
### Title
動画簡易変換ツール v0.3.3 — 起動安定化確認・ドキュメント整理

### Note
### 変更内容

#### ドキュメント整理（コード変更なし）

v0.3.2 で起動と動画変換を実機確認した。v0.3.3 では新機能追加は行わず、v0.3.1 由来の代替プレビュー記述を整理し、ドキュメントを現行の動作仕様に合わせる。

- **`manuals/user_manual.md`**: v0.3.1 で追加したステップ 2-B（プレビュー方式の切り替え）とエラー節「標準のプレビューで再生できませんでした」を削除。現行の WebView2 単一方式の記述に整理
- **`manuals/admin_manual.md`**: 「v0.3.1以降」の代替プレビュー案内を削除し、事前変換ダイアログへの案内に統一
- **`docs/release_checklist.md`**: セクション 1-A「起動安定性確認」を追加（v0.3.1 クラッシュ再発防止チェック項目）
- **`README.md`**: バージョン v0.3.3 に更新。バージョン履歴に v0.3.3 を追加
- **`development_report.md`**: v0.3.2 実機確認結果・代替プレビューの今後の方針を追記

#### 代替プレビューの今後の方針（development_report.md 参照）

- WebView2 継続 + 事前変換で対応（現行・v0.3.2 実機確認済み）
- WPF MediaElement 再採用は起動リスクがあるため慎重に判断
- FFmpeg 静止画プレビュー方式を将来の検討候補とする
- LibVLCSharp は DLL 同梱・ライセンス管理の観点から慎重に検討

---

## v0.3.2
### Title
動画簡易変換ツール v0.3.2 — 起動安定性修正（UseWpf削除・WPF代替プレビュー無効化）

### Note
### 変更内容

#### 起動クラッシュの修正（主要変更）

v0.3.1 のリリース版が起動直後にクラッシュする問題（`System.DllNotFoundException` in `WindowsBase`）を修正した。

- **`MovieConverter.csproj`**: `<UseWpf>true</UseWpf>` を削除。`WpfMediaElementVideoPlayer.cs` をコンパイル対象から除外（`<Compile Remove=...>`）。バージョン 0.3.1.0 → 0.3.2.0 / v0.3.2
- **`MainForm.cs`**: デュアルプレイヤー切り替えインフラを削除。`_player = _webView2Player` に一本化。プレビュー方式コンボボックスを削除。代替プレビューへの切り替えダイアログを削除。タイトルを v0.3.2 に更新
- **`Program.cs`**: 未処理例外ハンドラを追加。`logs/startup_error_{timestamp}.log` に例外情報を記録

#### 起動クラッシュの原因

| 要因 | 内容 |
|------|------|
| `<UseWpf>true</UseWpf>` の追加 | WPF ランタイム（WindowsBase.dll 等）の読み込みが必要になった |
| セルフコンテインド単一ファイル発行 | WPF 依存 DLL が正しく展開されない環境で `DllNotFoundException` が発生 |
| v0.3.0 では起動成功 | `UseWpf=true` がなく WPF 依存がなかったため起動していた |

#### 無効化した機能

- WPF MediaElement による代替プレビュー方式（v0.3.1 で追加）
- プレビュー方式切り替えコンボボックス
- 標準プレビュー失敗時の代替プレビュー案内ダイアログ

> WPF 代替プレビューのコード（`WpfMediaElementVideoPlayer.cs`）はリポジトリに保存されています。
> 将来のバージョンで単一ファイル発行との相性を確認したうえで再実装予定です。

#### ドキュメント更新

- `README.md`: バージョン v0.3.1 → v0.3.2。プレビュー注意事項から代替プレビュー記述を削除
- `manuals/admin_manual.md`: 起動時例外ログの確認方法を追加。v0.3.1 代替プレビューの経緯を注記
- `docs/release_checklist.md`: v0.3.2 起動確認・例外ログ確認チェック項目を追加
- `docs/test_scenarios.md`: v0.3.2 起動・例外ログ確認テストに更新
- `development_report.md`: v0.3.2 確認記録（原因分析・対応方針）を追加

---

## v0.3.1
### Title
動画簡易変換ツール v0.3.1 — 代替プレビュー方式追加（WPF MediaElement）

### Note
### 変更内容

#### 代替プレビュー方式の追加（主要変更）

標準プレビュー（WebView2）が再生できない場合のフォールバックとして、WPF MediaElement を使った代替プレビュー方式を追加した。

- **`WpfMediaElementVideoPlayer.cs`**: 新規追加。`IVideoPlayer` の WPF MediaElement 実装。ElementHost で WinForms に埋め込む。`_loadingOnly` パターンで読み込みトリガーの Play/Pause を UI に伝播しない。WinForms Timer 250ms で再生位置をポーリング
- **`MovieConverter.csproj`**: `<UseWpf>true</UseWpf>` を追加。バージョン 0.3.0.0 → 0.3.1.0 / v0.3.0 → v0.3.1
- **`MainForm.cs`**: 代替プレビュー対応の複数変更
  - `_webView2Player`・`_wpfPlayer`・`_previewContainer`・`cmbPlayerMode`・`_switchingPlayer`・`_playerVersion` フィールドを追加
  - `SubscribePlayerEvents()` にバージョンガード（`_playerVersion` 比較）を実装
  - `SwitchToPlayer()` で `_previewContainer` のコントロールを差し替え
  - `OnVideoPlayerError` で標準プレビュー失敗時に YesNo ダイアログを表示
  - 画面右上に「プレビュー方式:」コンボボックスを追加（「標準」「代替」）
  - `SetConvertingState` で `cmbPlayerMode.Enabled` を制御

#### 代替プレビューの仕様

| 項目 | 内容 |
|------|------|
| 標準プレビュー | WebView2 + HTML5 video（継続） |
| 代替プレビュー | WPF MediaElement + ElementHost |
| 切り替えトリガー | 標準プレビュー失敗時の YesNo ダイアログ（自動切り替えなし） |
| 手動切り替え | 画面右上「プレビュー方式:」コンボボックスで手動切り替え可 |
| 変換への影響 | なし（変換は ffmpeg.exe を直接呼び出す） |

#### フォールバックフロー

```
標準プレビュー失敗
  → 「代替プレビューで開き直しますか？」（YesNo）
    → Yes: 代替プレビューで再読み込み
    → No（または代替プレビューも失敗）: 事前変換ダイアログ（既存）
```

#### ドキュメント更新

- `README.md`: 代替プレビュー方式の説明追加・バージョン v0.3.0 → v0.3.1
- `manuals/user_manual.md`: ステップ 2-B「プレビュー方式の切り替え」を追加
- `manuals/admin_manual.md`: 代替プレビューの技術詳細・ログ確認方法を追加
- `docs/tool_design.md`: セクション 4.12（代替プレビュー設計方針）を追加。4.2 の比較表を更新
- `docs/test_scenarios.md`: セクション 17（代替プレビューテスト項目）を追加
- `docs/release_checklist.md`: 代替プレビュー関連チェック項目を追加
- `development_report.md`: v0.3.1 確認・修正記録を追加

---

## v0.3.0
### Title
動画簡易変換ツール v0.3.0 — 速度優先プリセット追加

### Note
### 変更内容

#### 速度優先プリセットの追加（主要変更）

v0.2.2 で準備していた `SpeedPreset` を活用し、圧縮設定に「速度優先」オプションを追加した。

- **`ConversionSettings.cs`**: `QualityPreset` に `SpeedPriority = 1` を追加。`HighQuality`・`Standard`・`SmallSize` の値を 1 つずつシフト
- **`FfmpegRunner.cs`**: `SpeedPriority` ケースを追加（`-c:v libx264 -crf 28 -preset veryfast -c:a aac -b:a 128k`）
- **`MainForm.cs`**: 圧縮設定コンボに「速度優先」をインデックス 1 に追加。ヒント文を switch 式に変更し速度優先の説明を追加。変換ログに「出力方式: 圧縮変換（速度優先）」を記録。タイトルを v0.3.0 に更新
- **`MovieConverter.csproj`**: バージョン 0.2.3.0 → 0.3.0.0 / v0.2.3 → v0.3.0

#### 速度優先プリセットの仕様

| 項目 | 内容 |
|------|------|
| 表示名 | 速度優先 |
| ffmpeg コマンド | `-c:v libx264 -crf 28 -preset veryfast -c:a aac -b:a 128k` |
| 解像度変更 | 可（元のまま・720p・480p から選択可） |
| ログ | `出力方式: 圧縮変換（速度優先）` |
| 画面ヒント | 「速度優先は、変換時間を短くするための設定です。画質は標準より少し低下する場合があります。」 |

#### ドキュメント更新

- `README.md`: 速度優先プリセット追加・バージョン v0.2.3 → v0.3.0
- `manuals/user_manual.md`: 目的別ガイド・圧縮設定表・説明ブロックを更新
- `manuals/admin_manual.md`: バージョン v0.2.3 → v0.3.0
- `docs/tool_design.md`: セクション 4.11（速度優先プリセット設計方針）・5.1・5.2 を更新
- `docs/test_scenarios.md`: セクション 14（速度優先テスト項目）を追加
- `docs/release_checklist.md`: 速度優先関連チェック項目を追加

---

## v0.2.3
### Title
動画簡易変換ツール v0.2.3 — コードベース確認・軽微修正

### Note
### 変更内容

#### コードレビューによる軽微修正

v0.2.2 リファクタリング後のコードベース全体を確認し、安全性・保守性・ドキュメント整合性を確認した。

- **`MainForm.cs`**: `AppendLogSafe` メソッドを削除（`AppendLog` と完全に同一の実装で冗長だった。`ConversionLogCallback` の呼び出しを `AppendLog` に変更）
- **`docs/test_scenarios.md`**: セクション 10-0「全体変換モード」のテスト手順を現行実装に更新（v0.1.4修正コミットでラジオボタン廃止・自動判定方式に変更済みだったが、テスト文書が旧UI前提のまま残っていた）

#### ドキュメント整合

- **`README.md`**: バージョン v0.2.1 → v0.2.3。バージョン履歴に v0.2.1 / v0.2.2 / v0.2.3 を追加
- **`manuals/user_manual.md`・`manuals/admin_manual.md`**: バージョン v0.2.1 → v0.2.3
- **`docs/test_scenarios.md`・`docs/release_checklist.md`**: バージョン表記を v0.2.3 に更新
- **`development_report.md`**: v0.2.2・v0.2.3 の確認・修正記録を追加

#### コードレビュー確認結果（問題なし）

| 確認項目 | 結果 |
|----------|------|
| 例外発生時の UI 状態回復 | ✓ 全終了経路で StopConversionTimer → SetConvertingState(false) |
| キャンセル時の不完全ファイル削除 | ✓ exitCode == null 判定で File.Delete |
| ffmpeg / ffprobe 未配置時の扱い | ✓ 起動時警告・変換ボタン無効化・動画情報欄の silent skip |
| 高速カット・事前変換の進捗計算 | ✓ time= 未出力時はマーキー継続。破綻なし |
| 日本語・空白パス対応 | ✓ ffmpeg 引数ダブルクォート・URI エンコード・SanitizeFileName |
| ログの利用者向け表現 | ✓ 技術用語の露出なし |

#### バージョン更新

- `MovieConverter.csproj`: バージョン 0.2.2.0 → 0.2.3.0 / v0.2.2 → v0.2.3
- ウィンドウタイトル: v0.2.2 → v0.2.3

---

## v0.2.2
### Title
動画簡易変換ツール v0.2.2 — IVideoPlayer抽象化・SpeedPreset追加（大規模リファクタリング）

### Note
### 変更内容

#### IVideoPlayer インターフェース新設（主要変更）

WebView2 固有コードを `MainForm` から完全に分離し、動画プレビュー方式を差し替え可能にするインターフェースを導入した。

- **`IVideoPlayer.cs`**: 新規追加。`PreviewControl`・`IsReady`・`InitializeAsync`・`LoadVideo`・再生コントロール（Play/Pause/Seek/SetVolume/SetMute）・イベント（VideoLoaded/TimeUpdated/PlaybackStarted/PlaybackPaused/PlaybackEnded/PlaybackBlocked/VideoError/FileDropped/LogMessage）を定義
- **`WebView2VideoPlayer.cs`**: 新規追加。`IVideoPlayer` の WebView2 実装。file-uri 主方式 + virtual-host フォールバック・D&Dインターセプト・WebView2 初期化を内包。`MainForm` から WebView2 固有コードをすべて移管
- **`MainForm.cs`**: `IVideoPlayer _player` フィールドに統一。`WebView2`・`CoreWebView2` への直接依存をすべて排除。`SubscribePlayerEvents()` でプレイヤーイベントをバインド。`using Microsoft.Web.WebView2.*` 参照を削除

#### SpeedPreset 追加（将来の速度優先プリセット UI への準備）

- **`ConversionSettings.cs`**: `SpeedPreset` enum を追加（`Default` = medium / `Fast` = fast / `VeryFast` = veryfast）。`ConversionSettings` に `Speed` プロパティを追加（デフォルト: `Default`）
- **`FfmpegRunner.cs`**: `GetPresetName(SpeedPreset)` ヘルパーを追加。`BuildArguments` で ffmpeg `-preset` オプションを `SpeedPreset` から動的に決定（従来は `medium` 固定）

#### 動作仕様

| 項目 | 内容 |
|------|------|
| 外部動作変化 | なし（リファクタリングのみ） |
| 速度プリセット UI | 未実装（v0.3.x で追加予定） |
| 代替プレビュー方式 | 未実装（IVideoPlayer 実装を追加するだけで差し替え可能） |
| ffmpeg preset | 現状 SpeedPreset.Default（medium）固定 |

#### バージョン更新

- `MovieConverter.csproj`: バージョン 0.2.1.0 → 0.2.2.0 / v0.2.1 → v0.2.2
- ウィンドウタイトル: v0.2.1 → v0.2.2

---

## v0.2.1
### Title
動画簡易変換ツール v0.2.1 — ffprobe 動画情報表示・変換残り時間（ETA）表示

### Note
### 変更内容

#### ffprobe による動画情報表示

ファイルを読み込んだ際に ffprobe を使って動画情報を取得し、ファイル選択欄に一行で表示する機能を追加した。

- **`FfprobeRunner.cs`**: 新規追加。`VideoInfo` レコード（長さ・解像度・映像コーデック・音声コーデック・フレームレート・サイズ・ビットレート）。`GetVideoInfoAsync` でffprobeをサブプロセス実行しJSON出力をパース。ffprobe不在時は `IsAvailable = false` となりアプリは正常動作を継続する
- **`MainForm.cs`**: `lblVideoInfo` を Row 0（ファイル選択欄）に追加。ファイル読み込み時に `LoadVideoInfoAsync` を非同期実行。ffprobe不在時は動画情報欄を空白表示（エラー表示なし）

#### 変換残り時間（ETA）表示

変換・事前変換中の経過時間表示に残り時間の推定値を追加した。

- **`MainForm.cs`**: `_lastProgressRatio` フィールドで最新進捗率を保持。`ElapsedTimer_Tick` を更新し、進捗率が 5% 超の場合は `残り約 HH:MM:SS` を表示、5% 以下の場合は `残り時間を計算中...` を表示。ETA計算式: `残り時間 = 経過時間 × (1 - 進捗率) / 進捗率`

#### 動作仕様

| 項目 | 内容 |
|------|------|
| ffprobe | bin/ffmpeg/ffprobe.exe に配置（任意） |
| ffprobe不在時 | 動画情報欄は空白のまま、変換・プレビューは正常動作 |
| 動画情報表示 | 長さ / 解像度 / 映像コーデック / 音声コーデック / フレームレート / サイズ / ビットレート |
| ETA表示 | 進捗率 5% 超で残り時間を表示 |
| ETA精度 | 線形推定（高速カット・事前変換はベストエフォート） |

#### バージョン更新

- `MovieConverter.csproj`: バージョン 0.2.0.0 → 0.2.1.0 / v0.2.0 → v0.2.1
- ウィンドウタイトル: v0.2.0 → v0.2.1

---

## v0.2.0
### Title
動画簡易変換ツール v0.2.0 — 事前変換機能追加（プレビュー失敗時ダイアログ案内）

### Note
### 変更内容

#### 事前変換機能の追加（主要変更）

プレビューできないMP4を読み込んだ際に、利用者へ分かりやすく説明し同意を得てから事前変換を実行する機能を追加した。

- **`FfmpegRunner.cs`**: `BuildPreconvertedOutputPath` を追加（`{baseName}_preconverted_{timestamp}.mp4`）。`RunFaststartAsync` を追加（内部コマンド: `-c copy -movflags +faststart`）。プロセス実行コアを `RunProcessAsync` として共通化
- **`MainForm.cs`**: プレビュー両方式失敗時に `ShowPreconvertConsentDialog()` を呼び出し同意確認。`RunPreconvertAsync`・`OnPreconvertCompleted` を実装。事前変換完了後に変換後ファイルをプレビューに自動読み込み。`_showPreconvertDialog` フラグで事前変換後ファイルへのダイアログ再表示を抑制。`_activeOperationLabel` フィールドで経過時間タイマーラベルを切り替え。タイトルを v0.2.0 に更新
- **`MovieConverter.csproj`**: バージョン 0.1.6.0 → 0.2.0.0 / v0.1.6 → v0.2.0

#### 動作仕様

| 項目 | 内容 |
|------|------|
| トリガー | file-uri / virtual-host 両方式でプレビュー失敗 |
| ダイアログ | 「この動画はそのままではこの画面で再生できません。事前変換が必要です。」案内→「実行する」/「キャンセル」 |
| 元ファイル | 変更・削除しない |
| 出力ファイル | `{元ファイル名}_preconverted_{yyyyMMdd_HHmmss}.mp4` |
| 再エンコード | なし（`-c copy`）/ 画質劣化なし |
| 完了後 | 変換後ファイルをプレビューに自動読み込み |
| UI表現 | 技術用語（faststart・moov atom等）を利用者向け画面に露出しない |

#### ドキュメント更新

- `README.md`・`manuals/user_manual.md`・`manuals/admin_manual.md`: v0.2.0 対応（機能説明・チェックリスト・エラー対応更新）
- `docs/tool_design.md`: セクション 4.10（事前変換機能の設計方針）を追加・更新
- `docs/test_scenarios.md`: セクション 12（v0.2.0 テスト項目）を追加・更新
- `docs/release_checklist.md`: 事前変換関連チェック項目を追加
- `development_report.md`: v0.2.0 対応記録を更新

---

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
