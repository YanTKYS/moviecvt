# 開発報告書 — 動画簡易変換ツール v0.1.0

| 項目 | 内容 |
|------|------|
| ツール名 | 動画簡易変換ツール |
| バージョン | v0.4.2 |
| 初回作成日 | 2026-05-23 |
| v0.1.1更新日 | 2026-05-23 |
| v0.1.2更新日 | 2026-05-23 |
| v0.1.3更新日 | 2026-05-24 |
| v0.1.4更新日 | 2026-05-24 |
| v0.1.5更新日 | 2026-05-24 |
| v0.1.6更新日 | 2026-05-24 |
| v0.2.0更新日 | 2026-05-24 |
| v0.2.1更新日 | 2026-05-24 |
| v0.2.2更新日 | 2026-05-24 |
| v0.2.3更新日 | 2026-05-24 |
| v0.3.0更新日 | 2026-05-24 |
| v0.3.1更新日 | 2026-05-24 |
| v0.3.2更新日 | 2026-05-24 |
| v0.3.3更新日 | 2026-05-24 |
| v0.4.0更新日 | 2026-05-24 |
| v0.4.1更新日 | 2026-05-25 |
| v0.4.2更新日 | 2026-05-25 |
| 参照ガイド | reference/guide_context.md（同梱方式） |
| GitHub Pages | 403エラーにより参照不可 — guide_context.md で代替 |

---

## v0.4.2 確認・修正記録

### v0.4.2 対応内容

#### 背景・目的

v0.4.1 で実利用環境への対応（WebView2未導入時のフリーズ修正）を行った。ここまでの実機確認を通じ、以下の問題が配布後の問い合わせの原因となっていた。

- ffmpeg.exe のDLL不足（終了コード 0xC0000135）
- ffprobe.exe の配置有無
- WebView2 Runtime 未導入
- 出力先パスへの書き込み権限

v0.4.2 では、配布後に問い合わせがあった際の原因切り分けを容易にするため、環境診断（動作確認）機能と起動時ログを追加した。

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `EnvironmentDiagnostics.cs` | 新規追加。`DiagnosticLevel`（Ok/Warning/Error）・`DiagnosticItem` レコード・`EnvironmentDiagnostics` クラスを実装。WebView2・ffmpeg・ffprobe・書き込み権限・動画ファイルをチェックし、結果リストと保存用テキストを返す |
| 2 | `WebView2VideoPlayer.cs` | `public bool InitFailed => _initFailed;` プロパティを追加（診断から参照用） |
| 3 | `MainForm.cs` | 「動作確認」ボタン（`btnDiagnostic`）をログ欄上部に追加。`BtnDiagnostic_Click` で `EnvironmentDiagnostics.RunAllAsync()` を呼び出し結果ダイアログを表示 |
| 4 | `MainForm.cs` | `ShowDiagnosticResultDialog` を追加。RichTextBox で診断結果を色分け表示。「診断結果を保存」ボタンで `logs/diagnostic_{timestamp}.txt` を保存 |
| 5 | `Program.cs` | `WriteStartupInfo()` を追加。起動時に `logs/startup_latest.log` を生成。バージョン・実行フォルダ・OS・WebView2・ffmpeg・ffprobe の状況を記録 |
| 6 | `MovieConverter.csproj` | バージョンを 0.4.2.0 / v0.4.2 に更新 |
| 7 | ドキュメント各種 | README・user_manual・admin_manual・test_scenarios・release_checklist・release-note・development_report を v0.4.2 対応に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| 「動作確認」ボタンの名称 | 「環境診断」より一般職員に分かりやすい「動作確認」を採用 |
| 診断ロジックを別ファイル（`EnvironmentDiagnostics.cs`）に分離 | MainForm.cs が既に 1700 行超のため、診断ロジックを独立させて保守性を確保 |
| WebView2 検出に `CoreWebView2Environment.GetAvailableBrowserVersionString()` を使用 | WebView2 を初期化せずに Registry ベースで確認できる静的メソッド。STA スレッドでの呼び出しで正常動作する |
| ffmpeg/ffprobe の起動確認は 5 秒タイムアウト | 診断ダイアログ表示まで長時間待たせない。`CancellationTokenSource.CreateLinkedTokenSource` でオーバーオールの 30 秒タイムアウトとも連携 |
| 起動時ログは `startup_latest.log`（上書き）方式 | 毎回のログ蓄積を避けつつ、最新の環境情報を常に参照可能にする。クラッシュログ（`startup_error_*.log`）とは別ファイルで役割を明確化 |
| 診断結果の保存先は `logs/` フォルダ固定 | ログ保存（入力ファイルの場所優先）とは異なり、診断結果は管理者向け情報のため常に `logs/` に保存する |

#### 未対応事項

| 事項 | 判断 |
|------|------|
| ffprobe による動画情報取得の診断 | 診断項目の要件には含まれていたが、既存の `LoadVideoInfoAsync` で機能しているため重複実装を避けた。ffprobe の起動確認（`-version`）で代替 |
| WebView2 初期化の完了待ちと連携 | 診断は WebView2 のインストール有無を API で確認するため、`IsReady` フラグとの連携は不要と判断 |
| 出力先フォルダ選択 | 今バージョン対象外 |

---

## v0.4.1 確認・修正記録

### v0.4.1 対応内容

#### 不具合の内容

実利用環境（WebView2 ランタイム未インストール）で起動した際、動画ファイルを読み込むとアプリが「動画を読み込み中...」のまま応答しなくなる問題が発生した。

#### 根本原因

`WebView2VideoPlayer.InitializeAsync()` が WebView2 ランタイム不在で例外をキャッチすると `IsReady = false` のまま終わるが、その後 `LoadVideo()` が呼ばれても `if (!IsReady) return;` で即 return するだけだった。`VideoLoaded` も `VideoError` も発火しないため、`MainForm` 側は「読み込み中」のまま固まる。

#### 修正内容

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `WebView2VideoPlayer.cs` | `_initFailed` フラグを追加。`InitializeAsync` catch / `OnInitializationCompleted` `!IsSuccess` 時にセット。`LoadVideo()` 呼び出し時に `_initFailed` なら即 `VideoError` を発火 |
| 2 | `FfprobeRunner.cs` | `VideoInfo` レコードに `DurationSeconds`（double）を追加。ffprobe JSON の生の秒数をそのまま保持 |
| 3 | `MainForm.cs` | `_previewUnavailableMode` フラグを追加。`OnVideoPlayerError` で `_webView2Player.IsReady == false` を検出した場合、事前変換ダイアログを出さず「プレビュー不可モード」に移行 |
| 4 | `MainForm.cs` | `ActivateConversionWithoutPreview()` を追加。テキストボックスを有効化し、ffprobe が duration を取得済みなら自動で全体変換設定（開始=0、終了=duration）を適用 |
| 5 | `MainForm.cs` | `LoadVideoInfoAsync()` にフック追加。ffprobe 完了時に `_previewUnavailableMode` かつ `_duration == 0` なら `_duration` を更新して `ActivateConversionWithoutPreview()` を呼ぶ（ffprobe が先に完了するタイミング差を吸収） |
| 6 | `MainForm.cs` | `SetConvertingState()` を更新。プレビュー不可モードでは変換後もテキストボックスを有効に保つ |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| WebView2 なしでも変換を可能にする | WebView2 はプレビュー専用であり、ffmpeg による変換とは独立している。プレビューができなくても、ffprobe が duration を取得できれば全体変換は実行可能 |
| 事前変換ダイアログはスキップ | WebView2 が存在しない状態では事前変換（faststart）しても再度読み込みに失敗するため、ダイアログを表示しても意味がない |
| duration 設定はタイミング差に対応 | `VideoError` は即座に発火するが ffprobe は非同期で数秒かかる。両パスで `ActivateConversionWithoutPreview()` を呼ぶことで、いずれが先に来ても正しく動作する |
| `IsReady` で判定する | `_initFailed` を公開プロパティにする代わりに、既存の `IsReady` プロパティで init 失敗を検出。`IsReady == false` かつ `VideoError` 受信＝init 失敗と判断 |

#### v0.4.1 実機確認結果

| 確認項目 | 結果 |
|----------|------|
| WebView2 ランタイム未インストール環境での起動 | ✓ 正常起動 |
| 動画ファイル読み込み時にフリーズしないこと | ✓ フリーズせず |
| プレビュー不可モードへの移行 | ✓ 正常移行 |
| 開始・終了時刻テキストボックスの有効化 | ✓ 有効化される |
| WebView2 なしでの変換実行 | ✓ 変換完了 |

---

## v0.4.0 確認・修正記録

### v0.4.0 対応内容

#### 背景・目的

v0.3.3 で起動安定化を確認した後、一般職員が迷わず使えるようにするための改善を実施した。機能の拡張（複数ファイル、出力先選択等）は今回対象外とし、変換結果の分かりやすさ・設定説明・エラー診断・ログ保存に絞って対応した。

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `MainForm.cs` | 変換完了時の結果ダイアログを `MessageBox` からカスタムダイアログ（`ShowConversionResultDialog`）に変更。出力ファイル名・出力先・変換前後サイズ・削減率・処理時間を表示 |
| 2 | `MainForm.cs` | 変換完了ダイアログに「出力フォルダを開く」「再生」ボタンを追加。`Process.Start` で explorer / 関連付けプレイヤーを起動 |
| 3 | `MainForm.cs` | 圧縮設定の説明テキスト（`lblModeHint`）を全5設定分に改善。利用者視点の表現に変更（「速い。画質は変わりません。」等） |
| 4 | `MainForm.cs` | エラー診断メソッド（`DiagnoseConversionError`）を追加。ffmpegの終了コード（DLL不足: 0xC0000135等）とログ内容から原因を診断してメッセージを返す |
| 5 | `MainForm.cs` | `CheckFfmpegAvailability` を改善。ffprobe未配置時の注意ログを追加。メッセージに「配置先:」「→」を明示 |
| 6 | `MainForm.cs` | ログ保存ボタン（`btnSaveLog`）を追加。ログ欄上部に配置。押下でログ内容を `movieconverter_log_{timestamp}.txt` として保存（入力ファイルと同フォルダ優先） |
| 7 | `MainForm.cs` | `TableLayoutPanel` の行数を 8 → 9 に変更。Row 7 にログ操作ボタン行（28px）を追加 |
| 8 | `MovieConverter.csproj` | バージョンを `0.4.0.0` / `v0.4.0` に更新 |
| 9 | ドキュメント各種 | README・user_manual・admin_manual・test_scenarios・release_checklist を v0.4.0 に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| 結果ダイアログはカスタム Form を採用 | `MessageBox` では変換前後サイズ比較・ボタン追加が困難なため。実装はすべて `ShowConversionResultDialog` メソッドに集約 |
| ログ保存先は「入力ファイルと同フォルダ」優先 | 利用者が出力ファイルと同じ場所でログを探せるように。未選択時は `logs/` フォルダにフォールバック |
| エラー診断は終了コード＋ログ文字列の両方を判断根拠にする | ffmpeg の終了コードだけでは原因が分からない場合があるため。DLL不足（0xC0000135）は終了コード、権限・容量・破損はログ文字列で検出 |
| 「出力ファイルを再生」は `UseShellExecute = true` で起動 | 再生に使うプレイヤーをアプリが指定しない。利用者のファイル関連付けに従う |
| 出力先フォルダ選択・ファイル名指定は対象外 | 今バージョンの目的は操作改善であり、機能拡張は対象外。現行の「元ファイルと同フォルダ・タイムスタンプ付き」仕様を維持 |

#### 未対応事項

| 事項 | 判断 |
|------|------|
| 出力先フォルダ選択 | 今バージョン対象外。利用頻度が高くなれば将来バージョンで検討 |
| 出力ファイル名指定 | 同上 |
| ffprobe未配置時の動画情報欄の説明 | 現状は空白のまま。ラベルで「ffprobe.exe 未配置」を表示する改善は将来検討 |
| WebView2プレビュー失敗時のより詳細な診断 | 現状はログにメッセージを出力するのみ。将来のバージョンで改善検討 |

---

## v0.3.3 確認・修正記録

### v0.3.3 対応内容

#### 背景・目的

v0.3.2 で起動クラッシュを修正し、実機で起動・動画変換を確認した。v0.3.3 では新機能追加は行わず、v0.3.2 復旧内容の整理・ドキュメント安定化を実施する。

#### v0.3.2 実機確認結果

| 確認項目 | 結果 |
|----------|------|
| セルフコンテインド発行版の起動 | ✓ 正常起動 |
| タスクマネージャでのプロセス存続 | ✓ 落ちない |
| イベントビューアーの .NET Runtime エラー | ✓ なし |
| WebView2 標準プレビューでのMP4読み込み | ✓ 正常 |
| 動画変換（高速カット・圧縮変換） | ✓ 正常完了 |
| 事前変換ダイアログ | ✓ 正常表示 |

#### 実施した変更（ドキュメントのみ・コード変更なし）

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `manuals/user_manual.md` | v0.3.1 由来の代替プレビュー方式（ステップ 2-B・エラー節）を削除。現行の WebView2 単一方式に合わせて整理 |
| 2 | `manuals/admin_manual.md` | 「両方式とも失敗した場合（v0.3.1以降）」の代替プレビュー案内を削除し、事前変換ダイアログへの案内に戻す |
| 3 | `docs/release_checklist.md` | セクション 1-A「起動安定性確認」を追加（v0.3.1 クラッシュ再発防止の確認項目） |
| 4 | `development_report.md` | v0.3.3 記録追加。v0.3.2 実機確認結果・代替プレビュー今後の方針を明記 |

#### 代替プレビュー方式の今後の方針

v0.3.1 の WPF MediaElement 代替プレビューは起動クラッシュを引き起こしたため、v0.3.2 以降は保留扱いとする。今後の方針を以下の通り整理する。

| 方針 | 内容 |
|------|------|
| WebView2 継続 + 事前変換で対応（現行） | WebView2 標準プレビューを継続し、再生できない MP4 は事前変換（`-c copy -movflags +faststart`）で対応する。v0.3.2 実機確認済み |
| WPF MediaElement 再採用は慎重に判断 | `UseWpf=true` がセルフコンテインド単一ファイル発行で `DllNotFoundException` を引き起こした。再採用には実機での起動確認が必須 |
| FFmpeg 静止画プレビュー方式の検討 | プレビューできない MP4 に対し、ffmpeg でキーフレームを静止画出力してサムネイル表示する方式。WPF 依存なし。検討候補 |
| LibVLCSharp は慎重に検討 | 対応形式が広く安定しているが、`libvlc*.dll` の同梱・ライセンス確認・配布物の増加が必要。閉域環境では管理コストが高い |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| コード変更なし | v0.3.3 はドキュメント安定化が目的。機能追加・コード変更は行わない |
| `WpfMediaElementVideoPlayer.cs` は保持 | コンパイル対象外のまま保存。将来の再実装の参考用 |
| `IVideoPlayer` インターフェースは維持 | 将来の代替プレビュー実装に備え、`IVideoPlayer` + `WebView2VideoPlayer` の構成を維持する |

---

## v0.3.2 確認・修正記録

### v0.3.2 対応内容

#### 背景・発端（起動クラッシュ）

v0.3.1 でリリースしたビルドが起動直後にクラッシュし、タスクマネージャにも残らない問題が発生した。

イベントビューアーに以下の例外が記録されていた。

```text
System.DllNotFoundException: Dll was not found.
at MS.Internal.WindowsBase.NativeMethodsSetLastError.SetWindowLongPtrWndProc
at MS.Win32.HwndSubclass.HookWindowProc
at MS.Win32.HwndSubclass.SubclassWndProc
```

`MS.Internal.WindowsBase` / `MS.Win32.HwndSubclass` は WPF の内部クラスであり、v0.3.1 で追加した `<UseWpf>true</UseWpf>` が根本原因と判断した。

#### 原因分析

| 要因 | 内容 |
|------|------|
| `<UseWpf>true</UseWpf>` の追加 | WPF ランタイム（WindowsBase.dll・PresentationCore.dll 等）の読み込みが必要になった |
| セルフコンテインド単一ファイル発行 | WPF 依存 DLL が正しく同梱・展開されない場合に DllNotFoundException が発生する |
| WindowsBase.dll の問題 | `SetWindowLongPtrWndProc`（WPF の HwndSubclass）の呼び出しで DLL が見つからない |
| v0.3.0 までは起動成功 | `<UseWpf>true</UseWpf>` がなく WPF 依存がなかったため起動していた |

**教訓:** セルフコンテインド単一ファイル発行では、WPF（PresentationFramework / WindowsBase 等）のような大型フレームワークは Windows の実行環境に依存する部分があり、単純に `UseWpf=true` を追加するだけでは動作しない環境がある。実機での起動確認が必須。

#### 対応方針

起動安定性を最優先として、以下を実施する。

1. `<UseWpf>true</UseWpf>` を削除（WinForms + WebView2 の構成に戻す）
2. `WpfMediaElementVideoPlayer.cs` をコンパイル対象から除外（ファイルは保存）
3. `MainForm.cs` からデュアルプレイヤー切り替えインフラを削除し、`_player = _webView2Player` に一本化
4. `Program.cs` に未処理例外ログ（`logs/startup_error_yyyyMMdd_HHmmss.log`）を追加
5. 代替プレビュー方式は再検討事項とし今回は実装しない

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `MovieConverter.csproj` | `<UseWpf>true</UseWpf>` を削除。`<Compile Remove="WpfMediaElementVideoPlayer.cs" />` を追加。バージョンを 0.3.1.0 → 0.3.2.0 / v0.3.1 → v0.3.2 に更新 |
| 2 | `Program.cs` | `AppDomain.CurrentDomain.UnhandledException` + `Application.ThreadException` ハンドラを追加。`WriteStartupLog()` で `logs/startup_error_{timestamp}.log` に例外情報（種別・メッセージ・スタックトレース・バージョン・OS）を書き出す |
| 3 | `MainForm.cs` | `_wpfPlayer`・`_previewContainer`・`cmbPlayerMode`・`_switchingPlayer`・`_playerVersion` フィールドを削除。`SubscribePlayerEvents()` のバージョンガードを削除してシンプル化。`SwitchToPlayer()`・`CmbPlayerMode_SelectedIndexChanged()` を削除。`InitializeComponent()` からプレビュー方式コンボボックスを削除。`OnVideoPlayerError()` から代替プレビューへの切り替えダイアログを削除。`SetConvertingState()` から `cmbPlayerMode.Enabled` を削除。タイトルを v0.3.2 に更新 |
| 4 | ドキュメント | README.md・manuals/admin_manual.md・docs/release_checklist.md・docs/test_scenarios.md・development_report.md を v0.3.2 対応に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| WpfMediaElementVideoPlayer.cs を削除しない | コンパイル対象から除外するのみ。将来の再実装時に参考にできる |
| IVideoPlayer インターフェースを維持 | 将来の代替プレビュー方式追加に備え、`IVideoPlayer` + `WebView2VideoPlayer` の構成を維持する |
| 起動時例外ログを Program.cs に追加 | 今後同様の起動クラッシュが発生した場合に原因特定が容易になる。ファイルパス: `{appDir}/logs/startup_error_{timestamp}.log` |
| 代替プレビュー方式の再実装は今回しない | WPF 以外の方式（LibVLCSharp 等）の評価・実機確認が必要。v0.4.0 以降の候補とする |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| 新しい代替プレビュー方式の追加 | 対象外（v0.4.0 以降で再検討） |
| LibVLCSharp 採用 | 対象外（v0.4.0 以降で再検討） |
| WPF MediaElement の再実装 | 対象外（単一ファイル発行との相性検証が必要） |

---

## v0.3.1 確認・修正記録

### v0.3.1 対応内容

#### 背景・発端

v0.2.2 で `IVideoPlayer` 抽象インターフェースを導入し「将来の代替プレビュー方式を差し替え可能にする」設計を行った。v0.3.1 では WPF MediaElement を代替プレビュー方式として実装し、標準プレビュー（WebView2）が失敗した場合のフォールバック手段を提供する。

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `WpfMediaElementVideoPlayer.cs` | 新規追加。`IVideoPlayer` の WPF MediaElement 実装。ElementHost で WinForms に埋め込む。`_loadingOnly` フラグで読み込みトリガー用 Play/Pause を UI に伝播しない。WinForms Timer 250ms で再生位置をポーリング。ElementHost への WinForms DragDrop 登録 |
| 2 | `MovieConverter.csproj` | `<UseWpf>true</UseWpf>` を追加（WindowsFormsIntegration が自動参照される）。バージョンを 0.3.0.0 → 0.3.1.0 / v0.3.0 → v0.3.1 に更新 |
| 3 | `MainForm.cs` | `_webView2Player`・`_wpfPlayer`・`_previewContainer`・`cmbPlayerMode`・`_switchingPlayer`・`_playerVersion` フィールドを追加。`SubscribePlayerEvents()` にバージョンガード（`_playerVersion` 比較）を実装し、プレイヤー切り替え後に古いラムダハンドラが動作しないようにする。`SwitchToPlayer()` で `_previewContainer` のコントロールを差し替え。`CmbPlayerMode_SelectedIndexChanged` で手動切り替え。`OnVideoPlayerError` で標準プレビュー失敗時に代替プレビューへの切り替えダイアログを表示（自動切り替えなし）。`InitializeComponent` にプレビュー方式コンボボックスを追加（右上）。`SetConvertingState` で `cmbPlayerMode.Enabled` を制御 |
| 4 | ドキュメント | README.md・user_manual.md・admin_manual.md・tool_design.md・test_scenarios.md・release_checklist.md・release-note.md・development_report.md を v0.3.1 対応に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| 代替プレビューへの自動切り替えをしない | 標準プレビュー失敗時に自動で切り替えると、利用者が意図しない状態になる。YesNo ダイアログで同意を取得してから切り替える |
| ラムダイベントハンドラのバージョンガード | プレイヤー切り替え後にラムダの解除ができないため、`_playerVersion` をインクリメントし、各ラムダが捕捉した `int v` と比較することで古いハンドラを無効化する |
| `_previewContainer` でコントロールを差し替え | TableLayoutPanel のセルを直接差し替えるより、ラッパー Panel の Controls を入れ替える方がシンプルで安全 |
| WPF MediaElement の読み込みトリガー | `LoadedBehavior = Manual` では `Source` 設定後に `Play()` を呼ばないと `MediaOpened` が発火しない。`_loadingOnly = true` で内部的な Play/Pause が UI に伝播しないようにする |
| 位置ポーリングに WinForms Timer を使用 | WPF MediaElement に `PositionChanged` イベントがないため 250ms ポーリング。WinForms Timer は Tick が UI スレッドで発火するため `InvokeRequired` 不要 |
| WebView2 を継続使用 | WebView2 は Windows 10 20H2 以降・Windows 11 で安定して動作する標準プレビューとして継続する。WPF MediaElement は補完的なフォールバックとして位置付ける |

#### LibVLCSharp を採用しなかった理由

| 比較項目 | WPF MediaElement | LibVLCSharp |
|---------|-----------------|-------------|
| 外部DLL | 不要（.NET 標準） | libvlc*.dll の配置が必要 |
| 配布容易性 | ○ | △（DLL一式の同梱・ライセンス確認が必要） |
| 閉域環境での配布 | 問題なし | DLL管理が複雑になる |
| 採用 | 採用（v0.3.1） | 今回は採用しない |

LibVLCSharp は再生対応形式が広く安定しているが、libvlc DLL の配置とライセンス確認が必要になる。閉域環境への配布容易性を優先し、v0.3.1 では WPF MediaElement を採用した。

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| LibVLCSharp 対応 | 配布容易性・ライセンスの観点から今回は採用しない |
| WebView2 の削除・置き換え | WebView2 は継続使用。対象外 |
| MP4 以外の形式対応 | 対象外 |
| UI 全面再設計 | 対象外 |
| 代替プレビューの実機確認 | Windows 実機が必要 |

---

## v0.3.0 確認・修正記録

### v0.3.0 対応内容

#### 背景・発端

v0.2.2 で `SpeedPreset` enum を先行追加していた。v0.3.0 では「速度優先」プリセットを UI に追加し、一般利用者が `-preset veryfast` の高速エンコードを選択できるようにした。

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `ConversionSettings.cs` | `QualityPreset` に `SpeedPriority = 1` を追加。既存値を 1 つずつシフト（`HighQuality = 2`・`Standard = 3`・`SmallSize = 4`）。明示的な整数値指定は `BtnConvert_Click` が `SelectedIndex` を直接キャストするため必須 |
| 2 | `FfmpegRunner.cs` | `BuildArguments` に `SpeedPriority` ケースを追加（`-c:v libx264 -crf 28 -preset veryfast -c:a aac -b:a 128k`）。FastCut 以外の else ブランチに入るため解像度フィルタが正常に適用される |
| 3 | `MainForm.cs` | `cmbQuality` に「速度優先」をインデックス 1 に挿入。`CmbQuality_SelectedIndexChanged` ヒント文を switch 式に変更し速度優先の説明を追加。`BtnConvert_Click` ログに `SpeedPriority` 専用分岐を追加（「出力方式: 圧縮変換（速度優先）」）。タイトルを v0.3.0 に更新 |
| 4 | `MovieConverter.csproj` | バージョンを 0.2.3.0 → 0.3.0.0 / v0.2.3 → v0.3.0 に更新 |
| 5 | ドキュメント | README.md・user_manual.md・admin_manual.md・tool_design.md・test_scenarios.md・release_checklist.md・release-note.md・development_report.md を v0.3.0 対応に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| CRF 28 + veryfast を選択 | 「速度優先」の目的は標準と同等の品質目標（CRF 28）を保ちながらエンコード時間を短縮すること。CRF を下げると品質が上がるが速度優先の意図と矛盾するため CRF は Standard と同値にした |
| SpeedPriority の解像度有効化 | FastCut でないため解像度フィルタが適用される。`cmbResolution.Enabled = cmbQuality.SelectedIndex != 0` のロジックは変更不要で動作する |
| ヒント文の switch 式化 | if-else if の連鎖より可読性が高く、今後のプリセット追加時も変更が容易 |
| QualityPreset 整数値の明示 | `cmbQuality.SelectedIndex` を `(QualityPreset)` キャストしているため値とインデックスが一致しなければならない。明示指定により意図を明確化 |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| 速度優先 + 解像度変更の実機確認 | Windows 実機が必要 |
| ハードウェアエンコード（h264_nvenc 等） | 環境依存大・対象外 |

---

## v0.2.3 確認・修正記録

### v0.2.3 対応内容

#### 背景・発端

v0.2.2 で大規模リファクタリング（IVideoPlayer 抽象化・SpeedPreset 追加）を実施した。
v0.2.3 では実機打鍵テストは実施せず、コードベースの安全性・保守性・ドキュメント整合性をコードレビューとして確認した。

#### コードベース確認結果

| 確認観点 | 結果 | 備考 |
|----------|------|------|
| 責務分離（UI / ffmpeg / ffprobe / プレビュー） | ✓ 適切 | MainForm / FfmpegRunner / FfprobeRunner / WebView2VideoPlayer に分離済み |
| 呼び出し順（コンストラクタ） | ✓ 適切 | `new WebView2VideoPlayer()` → `InitializeComponent()` → `SubscribePlayerEvents()` → `InitializeAsync()` の順で問題なし |
| 例外発生時の UI 状態回復 | ✓ 適切 | BtnConvert_Click / RunPreconvertAsync 両方で catch → StopConversionTimer → Dispose → SetConvertingState(false) を確認 |
| キャンセル時の不完全ファイル削除 | ✓ 適切 | OnConversionCompleted / OnPreconvertCompleted 両方で `exitCode == null` 時に File.Delete を確認 |
| ffmpeg 未配置時の扱い | ✓ 適切 | CheckFfmpegAvailability() で起動時ログ警告・ステータス表示・変換ボタン無効化。ValidateBeforeConvert でも二重確認 |
| ffprobe 未配置時の扱い | ✓ 適切 | `IsAvailable = false` → LoadVideoInfoAsync が early return。エラー表示なし・他機能に影響なし |
| 進捗率・ETA 計算（全体変換） | ✓ 適切 | `totalDuration = _duration` を RunAsync に渡す |
| 進捗率・ETA 計算（範囲変換） | ✓ 適切 | `totalDuration = _endSeconds - _startSeconds` |
| 進捗率・ETA 計算（高速カット） | ✓ 適切 | `-c copy` は `time=` を出力しないためマーキー表示のまま経過時間のみ。意図通り |
| 進捗率・ETA 計算（事前変換・_duration=0） | ✓ 適切 | プレビュー失敗時は `_duration=0`。`totalDurationSeconds=0` の場合 FfmpegRunner が進捗コールバックをスキップしマーキー継続。問題なし |
| 日本語・空白パス対応 | ✓ 設計済み | ffmpeg 引数をダブルクォートで囲む・`Uri.EscapeDataString` でファイル名エンコード・`SanitizeFileName` で出力ファイル名の特殊文字置換 |
| ネットワークドライブ（UNCパス） | ✓ 設計上問題なし | `File.Exists` はUNCパス対応。ffmpeg引数もダブルクォートで囲む |
| ログ・エラーメッセージの利用者向け表現 | ✓ 適切 | 専門用語（moov atom・faststart等）を利用者向け画面に露出しない方針が維持されている |
| スレッド安全性（InvokeRequired） | ✓ 適切 | OnConversionCompleted・OnPreconvertCompleted・SetVideoInfoText・AppendLog・SetStatus で確認済み |
| WebView2 イベントのスレッド | ✓ 適切 | WebView2 の WebMessageReceived・NavigationCompleted 等は UI スレッドで発火するため InvokeRequired 不要 |

#### 発見した不整合・修正内容

| # | 種別 | 内容 | 対応 |
|---|------|------|------|
| 1 | コード冗長 | `AppendLogSafe` が `AppendLog` と完全に同じ実装（`AppendLog` 自体が InvokeRequired を処理済み） | `AppendLogSafe` を削除。`ConversionLogCallback` の呼び出しを `AppendLog` に変更 |
| 2 | テスト齟齬 | `docs/test_scenarios.md` セクション 10-0 「全体変換モード」のテスト手順がv0.1.4当初のラジオボタン UI を前提としていた（git `75e6e68` でラジオボタン廃止・自動判定方式に変更済み） | 現行の自動判定方式（開始≈0 かつ 終了≈動画時間 → 全体変換）に合わせてテスト手順を更新 |
| 3 | バージョン不整合 | README.md・manuals・test_scenarios.md・release_checklist.md が v0.2.1 のまま | 各ファイルを v0.2.3 に更新 |
| 4 | 開発記録不備 | development_report.md に v0.2.2 および v0.2.3 の記録がなかった | 本セクションおよび v0.2.2 セクションを追加 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| `AppendLogSafe` の削除 | 冗長コードの除去。外部動作変化なし。`AppendLog` が既に InvokeRequired 対応済みのため二重ラップ不要 |
| コードレビューのみ・実機テストなし | v0.2.2 主要機能は実機確認済み。v0.2.3 の修正内容（冗長コード除去・ドキュメント更新）は実機動作に影響しない |

---

## v0.2.2 確認・修正記録

### v0.2.2 対応内容

#### 背景・発端

v0.2.1 で ffprobe 動画情報表示・ETA 表示を追加した。v0.2.2 では今後の機能追加を見据えた大規模リファクタリングを実施した。

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `IVideoPlayer.cs` | 新規追加。WebView2 非依存の動画プレイヤー抽象インターフェース。`PreviewControl`・`IsReady`・`InitializeAsync`・`LoadVideo`・再生コントロール（Play/Pause/Seek/SetVolume/SetMute）・イベント（VideoLoaded/TimeUpdated/PlaybackStarted/PlaybackPaused/PlaybackEnded/PlaybackBlocked/VideoError/FileDropped/LogMessage）を定義 |
| 2 | `WebView2VideoPlayer.cs` | 新規追加。`IVideoPlayer` の WebView2 実装。file-uri 主方式 + virtual-host フォールバック・D&D インターセプト（NavigationStarting/NewWindowRequested）・WebView2 初期化を内包。MainForm から WebView2 固有コードをすべて移管 |
| 3 | `MainForm.cs` | `IVideoPlayer _player` フィールドに統一。`using Microsoft.Web.WebView2.*` 参照を排除。`SubscribePlayerEvents()` でプレイヤーイベントをバインド。コンストラクタで `_player = new WebView2VideoPlayer()` を `InitializeComponent()` より前に実行 |
| 4 | `ConversionSettings.cs` | `SpeedPreset` enum を追加（`Default` = medium / `Fast` = fast / `VeryFast` = veryfast）。`ConversionSettings` に `Speed` プロパティを追加（デフォルト: `Default`） |
| 5 | `FfmpegRunner.cs` | `GetPresetName(SpeedPreset)` ヘルパーを追加。`BuildArguments` で ffmpeg `-preset` オプションを `SpeedPreset` から動的に決定（従来は `medium` 固定） |
| 6 | `MovieConverter.csproj` | バージョンを 0.2.1.0 → 0.2.2.0 / v0.2.1 → v0.2.2 に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| IVideoPlayer 導入 | WebView2 固有コードを MainForm から分離し将来の代替プレビュー方式を差し替え可能にする。現時点の外部動作は変化なし |
| コンストラクタ順序 | `InitializeComponent()` が `_player.PreviewControl` を参照するため、`new WebView2VideoPlayer()` を先に実行する必要がある |
| SpeedPreset の先行追加 | UI 実装（v0.3.x 予定）に先立って ConversionSettings と FfmpegRunner の対応を完了させた。現状は `SpeedPreset.Default` 固定 |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| 速度優先プリセット UI | v0.3.x で追加予定 |
| WebView2 以外のプレビュー実装 | IVideoPlayer 実装を追加するだけで差し替え可能。v0.3.x 以降の検討事項 |

---

## v0.4.0 以降の機能候補

v0.3.1 完了後、以下を v0.4.0 以降の候補として整理する。

| 機能 | 概要 | 優先度 |
|------|------|--------|
| 出力先フォルダ選択 | 現在は入力ファイルと同じフォルダ固定。任意フォルダへの出力を可能にする | 中 |
| LibVLCSharp 対応プレビュー | WPF MediaElement より再生対応形式が広い。libvlc DLL 配置・ライセンス確認が必要 | 低 |
| 出力ファイル名指定 | タイムスタンプ自動付加に加えて任意のファイル名も指定できるようにする | 低 |
| 複数ファイル変換 | 同じ設定で複数の MP4 をまとめて変換 | 低 |
| 複数区間カット | 1 つの動画から複数の範囲を一度に切り出す | 低 |
| サムネイル一覧 | プレビュー前にサムネイル一覧で内容を確認できる | 低 |

---

## v0.2.1 確認・修正記録

### v0.2.1 対応内容

#### 背景・発端

v0.2.0 で事前変換機能を実装した。v0.2.1 では、v0.3.0候補に挙げていた「ffprobe による動画情報表示」と「変換残り時間（ETA）表示」を実装した。

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `FfprobeRunner.cs` | 新規追加。`VideoInfo` レコード（長さ・解像度・映像コーデック・音声コーデック・フレームレート・サイズ・ビットレート）。`GetVideoInfoAsync` でffprobeをサブプロセス実行しJSONパース。`FriendlyVideoCodec`・`FriendlyAudioCodec`・`ParseFrameRate`・`FormatFileSize`・`FormatBitrate` のヘルパーメソッドを実装 |
| 2 | `MainForm.cs` | `lblVideoInfo` を Row 0 に追加（Row 0 高さ 68→96px）。`_ffprobe` フィールド追加。`_lastProgressRatio` フィールド追加。`LoadFile` でファイル選択時に `LoadVideoInfoAsync` を非同期起動。`LoadVideoInfoAsync`・`SetVideoInfoText` メソッドを追加。`OnConversionProgress` で `_lastProgressRatio` を更新。`ElapsedTimer_Tick` を更新し進捗率5%超でETA表示、5%以下で「残り時間を計算中...」表示。タイトルを v0.2.1 に更新 |
| 3 | `MovieConverter.csproj` | バージョンを 0.2.1.0 / v0.2.1 に更新 |
| 4 | ドキュメント | README.md・user_manual.md・admin_manual.md・ffmpeg_setup.md・test_scenarios.md・release_checklist.md・release-note.md・development_report.md を v0.2.1 対応に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| ffprobe 任意配置 | ffprobe.exe が存在しない場合はアプリ正常動作を継続。動画情報欄は空白（エラーなし） |
| 動画情報取得タイムアウト | 15秒でキャンセル。大容量ファイルで長時間待たせない |
| ETA計算式 | `残り時間 = 経過時間 × (1 - 進捗率) / 進捗率`。線形推定。高速カット・事前変換はベストエフォート |
| ETA開始閾値 | 5%未満は「残り時間を計算中...」表示。初期の不安定な進捗率でETA算出すると誤表示になるため |
| ステータス表示 | マーキー時は経過時間のみ。確定バー時は進捗率+ETA表示 |

---

## v0.2.0 確認・修正記録

### v0.2.0 対応内容

#### 背景・発端

v0.1.2で「プレビューできないMP4はカット位置を指定できないため変換不可」という制約を文書化した。v0.2.0 では、moov atom 末尾配置が原因のプレビュー失敗を利用者自身で解決できる手段（「MP4を整える」ボタン）を実装した。

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `FfmpegRunner.cs` | `BuildPreconvertedOutputPath` を追加（`{baseName}_preconverted_{timestamp}.mp4`）。`RunFaststartAsync` を追加（`-c copy -movflags +faststart` コマンド）。プロセス実行コアを `RunProcessAsync` として共通化 |
| 2 | `MainForm.cs` | `_showPreconvertDialog` フラグを追加。`LoadFile` に `allowPreconvertDialog` パラメータを追加。`OnVideoPlayerError` 内で両方式失敗時に `ShowPreconvertConsentDialog()` を呼び出し同意を確認。`RunPreconvertAsync`・`OnPreconvertCompleted`・`ShowPreconvertConsentDialog` を実装。`_activeOperationLabel` フィールドを追加し経過時間タイマーのラベルを動的切り替え。タイトルを v0.2.0 に更新 |
| 3 | `MovieConverter.csproj` | バージョンを 0.2.0.0 / v0.2.0 に更新 |
| 4 | ドキュメント | README.md・user_manual.md・admin_manual.md・tool_design.md・test_scenarios.md・release_checklist.md・release-note.md・development_report.md を v0.2.0 対応に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| ダイアログによる同意取得 | 大容量ファイルのコピー処理を利用者の知らないうちに開始しない。「実行する」を押した場合のみ実行 |
| 元ファイル変更なし | `{name}_preconverted_{timestamp}.mp4` として常に別ファイルを作成。上書きしない |
| ダイアログ再表示抑制 | `_showPreconvertDialog` + `LoadFile(allowPreconvertDialog: false)` により、事前変換後の自動読み込みでダイアログが再表示されるのを防止 |
| 再エンコードなし | `-c copy` を使用。画質劣化・サイズ変化なし（moov atom の再配置のみ） |
| 利用者向けUI表現 | 「事前変換」「カット位置を確認するため」等、技術用語（faststart・moov atom）を露出しない |
| 管理者向け説明 | admin_manual.md に内部処理（ffmpegオプション・再エンコードなし等）を明記 |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| 変換出力への自動 faststart 化 | 変換後ファイルのmoov atom位置はffmpegデフォルト（末尾）になる場合がある。v0.3.0以降の検討事項 |
| 手動での事前変換ボタン | ダイアログ方式に統一。利用者が能動的にボタンを探す必要がなく分かりやすい |

---

## v0.1.6 確認・修正記録

### v0.1.6 対応内容

#### 背景・発端

v0.1.5 までで主要機能（MP4 プレビュー・高速カット・全体変換・圧縮変換・D&D・音量スライダー・進捗表示）の実装が完了した。庁内試用に向けて配布・説明・エラー案内の整理を行う。

#### 実施した変更（ドキュメントのみ、コード変更なし）

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `README.md` | バージョンを v0.1.6 に更新。フォルダ構成の詳細化（DLL 配置の強調）。「よくあるトラブル」一覧（8項目）を追加 |
| 2 | `manuals/admin_manual.md` | バージョンを v0.1.6 に更新。「2-3. 初回確認チェックリスト」（10項目）を追加。「8-4. 変換に時間がかかる」「8-5. 高速カットで開始位置がずれる」を障害対応に追加 |
| 3 | `manuals/user_manual.md` | バージョンを v0.1.6 に更新。先頭に「目的別ガイド」（やりたいこと→推奨設定の表・所要時間目安）を追加。高速カット・圧縮変換・音量スライダーの注意点をまとめた説明を先頭に集約。MP4以外は対象外であることを明記 |
| 4 | `docs/release-note.md` | v0.1.6 セクションを追加 |
| 5 | `development_report.md` | v0.1.6 記録と v0.2.0 以降の機能候補一覧を追加 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| コード変更なし | v0.1.6 は配布前整備が目的。機能追加はしない |
| ユーザーマニュアルの目的別ガイドを先頭に配置 | 操作手順を読む前に「どれを選べばよいか」を分かるようにすることで、初回利用の迷いを減らす |
| 初回チェックリストを admin_manual に追加 | 管理者が配布前に実施すべき確認を明示し、配布後のトラブルを減らす |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| 大きな機能追加 | 試作版配布前の整備が目的。v0.2.0 以降に引き継ぐ |
| operator_manual.md の更新 | 内容が admin_manual と重複している部分が多く、配布対象から除外して問題ない |

---

## v0.1.5 確認・修正記録

### v0.1.5 対応内容

#### 背景・発端

v0.1.4 実利用フィードバックとして以下が報告された。

1. 圧縮変換や長時間動画の変換中に「終わる気配がない」と感じる
2. 変換ログに ffmpeg の生出力が混在し、一般職員に分かりにくい

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `FfmpegRunner.cs` | `RunAsync` に `totalDurationSeconds`・`progressCallback` パラメータを追加 |
| 2 | `FfmpegRunner.cs` | `TryParseProgressTime` メソッドを追加。ffmpeg stderr の `time=HH:MM:SS.ss` を正規表現でパースし、進捗率（0.0〜1.0）をコールバックに通知 |
| 3 | `FfmpegRunner.cs` | 進捗行（`time=` 含む）はコールバック呼び出し後に logCallback をスキップ。`[実行] ffmpeg args` の行を削除（MainForm 側でユーザー向け情報を既にログ出力しているため冗長） |
| 4 | `MainForm.cs` | `ProgressBar`（pbProgress）を変換パネル上部に追加（行高: 48→82px）。変換開始時はマーキー表示、進捗取得後は 0〜100% の確定表示に切り替え |
| 5 | `MainForm.cs` | `System.Windows.Forms.Timer`（1秒間隔）で経過時間を計測。ステータスラベルに「状態: 変換中... 経過時間 HH:MM:SS  N%」を表示 |
| 6 | `MainForm.cs` | 完了ログに所要時間を追加（`所要時間: HH:MM:SS`） |
| 7 | `MainForm.cs` | `ConversionLogCallback` を追加。`[` で始まるタグ付き行のみ txtLog に表示、ffmpeg 生出力はバッファ（`_ffmpegOutputBuffer`）に蓄積 |
| 8 | `MainForm.cs` | 変換失敗時のみ `_ffmpegOutputBuffer` を txtLog にダンプして管理者が確認できるようにする |
| 9 | `MainForm.cs` | `StopConversionTimer` ヘルパーを追加。完了・キャンセル・例外いずれの終了経路でもタイマー停止・プログレスバー非表示を確実に実行 |
| 10 | `MovieConverter.csproj` | バージョンを 0.1.5.0 / v0.1.5 に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| ffmpeg生出力のフィルタ基準 | `[` で始まる行のみ表示する規約。自前ログは全て `[種別] メッセージ` 形式で出力するため、ffmpeg の生出力（`frame=...`、`Input #0, ...` 等）と確実に区別できる |
| プログレスバーの初期状態をマーキーにする | `-c copy`（高速カット）では `time=` が出力されない場合があり、確定表示できない。マーキー→確定の一方向切り替えで両ケースを自然にカバーする |
| totalDurationSeconds の渡し方 | FullVideo の場合は `_duration`（動画全体時間）、RangeOnly の場合は `endSeconds - startSeconds`（指定範囲）を渡す。ffmpeg の出力する time= は常に入力の先頭からの絶対時間ではなく変換済み出力時間を示すため、この方針で正しく計算できる |
| 経過時間タイマーに WinForms Timer を使用 | Tick イベントが UI スレッドで発火するため `InvokeRequired` 不要。BackgroundWorker/Task より実装がシンプル |
| ffmpegバッファを StringBuilder + lock で管理 | ErrorDataReceived はバックグラウンドスレッドから呼ばれるため競合防止に lock が必要 |

#### 対応しなかった内容

| 項目 | 理由・方針 |
|------|-----------|
| `-preset fast`/`veryfast` 追加 | v0.1.5の範囲外。要望があればv0.1.6以降で追加 |
| ハードウェアエンコード（h264_nvenc等） | 環境依存が大きく初期版では対象外 |
| 変換の残り時間（ETA）表示 | ffmpegのspeed値から計算可能だが精度が低いため保留 |

---

## v0.1.4 確認・修正記録

### v0.1.4 対応内容

#### 背景・発端

実利用フィードバックとして以下が報告された。

1. 画質変換したいだけなのに開始・終了位置指定が必須で操作ステップが多い
2. D&DしたらWebView2内でブラウザ再生され、アプリ内プレビューに反映されない
3. プレビュー音量が調整できない
4. 圧縮変換が遅い（対応方針の整理を行う）

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `ConversionSettings.cs` | `ConversionMode` 列挙型（`RangeOnly`/`FullVideo`）を追加。`ConversionSettings` に `Mode` プロパティを追加 |
| 2 | `FfmpegRunner.cs` | `Mode == FullVideo` 時は `-ss`/`-to` をコマンドに付加しない |
| 3 | `MainForm.cs` | `rbtRangeOnly`/`rbtFullVideo` ラジオボタンを pnlCut 上部に追加（row高さ108→140） |
| 4 | `MainForm.cs` | `RbtConversionMode_CheckedChanged`: FullVideo時はカット位置UI無効化、FastCut選択中なら標準に切替 |
| 5 | `MainForm.cs` | `UpdateConvertButton`/`ValidateCanConvertSilent`/`ValidateBeforeConvert` を FullVideo対応に更新（開始・終了不要） |
| 6 | `MainForm.cs` | `trkVolume`/`btnMute` を pnlPlayback 右端に追加。`TrkVolume_Scroll`/`BtnMute_Click` ハンドラを実装 |
| 7 | `MainForm.cs` | `OnWebView2NavigationStarting`: WebView2 に `.mp4` をD&Dした際のブラウザナビゲーションをキャンセルして `LoadFile()` にリダイレクト |
| 8 | `Assets/player.html` | `volume`/`mute` コマンド受信ハンドラを追加 |
| 9 | `MovieConverter.csproj` | バージョンを 0.1.4.0 / v0.1.4 に更新 |

#### 設計判断

| 判断事項 | 内容 |
|---------|------|
| D&D修正にオーバーレイパネルを使わない | `NavigationStarting` でMP4ナビゲーションをキャンセルする方式は、WebView2の既存インタラクション（クリック・スクロール）を全く妨げないため採用 |
| FullVideo + FastCut を自動切替 | `-c copy` + `-i input.mp4` で全体コピーは有効なコマンドだが「動画全体を変換」の利用意図（圧縮）と合わないため、FullVideo選択時にFastCutを標準へ自動切り替え |
| 音量はプレビューのみ | 出力動画の音量変更は要件外。スライダー操作はplayer.html側のvideo.volumeを変更するだけで、FFmpegコマンドには影響しない |

#### 変換時間短縮の整理（v0.1.4ドキュメント追記）

| 事項 | 内容 |
|------|------|
| `-preset fast`/`veryfast` | libx264の`-preset medium`を変更すれば速くなる（画質はわずかに低下）。v0.1.5以降で導入検討 |
| 進捗表示 | FFmpegのstderrに出力される`time=...`をパースすれば進捗バーを実装できる。v0.1.5以降の候補 |
| ハードウェアエンコード | `h264_nvenc`/`h264_qsv`対応。環境依存が大きいため初期版では対象外 |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| 変換時間短縮（実装） | v0.1.5以降。-preset変更・プログレスバーは設計整理のみ |
| 出力動画の音量変更 | 要件外 |
| 複数ファイル・区間 | 対象外 |

---

## v0.1.3 確認・修正記録

### v0.1.3 対応内容（2）: 高速カット機能追加

#### 背景・発端

実機確認で、カット変換が長時間動画（例: 数時間の会議録画）で非常に長い時間かかることが判明した。
既存の実装はすべて `-c:v libx264` による再エンコード方式であり、解像度「元のまま」を選んでも全体を再エンコードしていた。

#### 対応方針

利用者は「カットだけしたい（圧縮は不要）」というケースが多いと判断し、`-c copy`（再エンコードなし）の高速カット方式を追加する。

| 判断事項 | 内容 |
|---------|------|
| 初期値を高速カットにする | 長時間動画のカット用途が主体のため。利用者が「圧縮が必要」と判断した場合のみ切り替える |
| カット精度のずれを許容する | `-c copy` はキーフレーム境界に丸められるが、一般職員の利用では許容範囲と判断 |
| 解像度設定を無効化する | 高速カット時に解像度コンボを操作しても意味がないため、UI上でグレーアウトする |

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `ConversionSettings.cs` | `QualityPreset` 列挙型に `FastCut`（値0）を追加 |
| 2 | `FfmpegRunner.cs` | `BuildArguments` で `FastCut` 時は `-c copy` を使用。解像度フィルタ・再エンコードオプションをスキップ |
| 3 | `MainForm.cs` | `cmbQuality` に「しない（高速カット）」を追加し初期値を 0 に変更 |
| 4 | `MainForm.cs` | `CmbQuality_SelectedIndexChanged` ハンドラを追加。`cmbResolution` の有効/無効と `lblModeHint` テキストを切り替える |
| 5 | `MainForm.cs` | `lblModeHint` ラベルを追加し、高速カット/圧縮変換の特徴を画面に表示 |
| 6 | `MainForm.cs` | `SetConvertingState` で `cmbResolution.Enabled` に高速カット判定を追加 |
| 7 | `MainForm.cs` | `BtnConvert_Click` で「出力方式: 高速カット（再エンコードなし）」または「圧縮変換（再エンコードあり）」をログ出力 |
| 8 | `MainForm.cs` | タイトルを v0.1.3 に更新 |
| 9 | `MovieConverter.csproj` | バージョンを 0.1.3.0 / v0.1.3 に更新 |
| 10〜 | ドキュメント各種 | README, user_manual, admin_manual, tool_design, test_scenarios, release_checklist, release-note, development_report を更新 |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| 高速カット後の自動faststart化 | 変換出力ファイルに moov atom が末尾配置になるケースへの対応だが、実装コストが増大するためv0.2.0以降 |
| `-c copy` 時の音声ずれ対応 | 入力に問題がある場合のみ発生する。初期版では対象外 |
| カット精度モード切替（前後のキーフレーム選択） | 高度な機能であり初期版の対象外 |

---

### v0.1.3 対応内容（1）: ffmpeg DLL配置手順追記

#### 背景・発端

実機での変換実行時に、終了コード `-1073741515` が返る事象を確認した。

| 項目 | 内容 |
|------|------|
| 終了コード | -1073741515（0xC0000135） |
| Windows上の意味 | DLL not found（必要なDLLが見つからない） |
| 原因 | `bin\ffmpeg\` に `ffmpeg.exe` のみ配置し、付属DLLを配置していなかった |

#### 原因分析

FFmpegには「スタティックリンク版」と「DLL同梱版」の2種類のビルドがある。
DLL同梱版は `ffmpeg.exe` 単体では動作せず、`avcodec-*.dll`、`avformat-*.dll`、`avutil-*.dll`、`swresample-*.dll`、`swscale-*.dll` 等のDLLが同じフォルダに必要。

配布元ZIPを展開した際にDLLをコピーせず `ffmpeg.exe` だけを取り出して配置した場合に発生する。

#### 対応方針

- アプリ本体では特別な対応は不要（終了コードは既にログに表示されている）
- ドキュメントを更新してDLL配置の必要性と確認手順を明記する
- 配置確認コマンド（`bin\ffmpeg\ffmpeg.exe -version`）をセットアップ手順に明記する

#### 実施した変更（ドキュメントのみ、コード変更なし）

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `docs/ffmpeg_setup.md` | DLL配置必須の強調、フォルダ構成例にDLL一覧追記、`-version` 確認手順追加、-1073741515 トラブルシュート追加 |
| 2 | `manuals/admin_manual.md` | 配布パッケージ構成にDLL明記、配置手順に `-version` 確認ステップ追加、8-3に -1073741515 セクション追加 |
| 3 | `README.md` | ffmpeg 配置説明にDLL注意・`-version` 確認を追加、v0.1.3 バージョン履歴追記 |
| 4 | `docs/test_scenarios.md` | セクション10（v0.1.3）にDLL配置確認・-1073741515 確認テストを追加 |
| 5 | `docs/release_checklist.md` | DLL配置確認・`-version` 確認チェック項目を追加 |
| 6 | `docs/release-note.md` | v0.1.3 リリースノートを追加 |
| 7 | `development_report.md` | 本セクションを追加 |

#### 対応しなかった内容

| 事項 | 理由 |
|------|------|
| アプリ側でのDLL欠落自動検出 | 終了コードは既にログに記録されている。ドキュメントで十分と判断 |
| ffmpeg ビルド種別の自動判別 | 実装コスト増大のため対象外 |

---

## v0.1.2 確認・修正記録

### v0.1.2 対応内容

#### 背景・発端

実機確認で、以下のMP4動画を読み込んだところプレビューに失敗した。

| 項目 | 内容 |
|------|------|
| コンテナ | MPEG-4 / MP4 (isom/iso2/avc1/mp41) |
| 映像コーデック | AVC / H.264 (avc1)、High@L3.1 |
| 解像度 | 1280×720、30fps |
| 色形式 | YUV 4:2:0 8bit |
| 音声コーデック | AAC LC |
| 長さ | 約3時間20分 |
| サイズ | 約1.55GiB |
| エンコーダ | Lavf61.7.100 |

この動画は形式として初期版の対象内であり、「AVC非対応」ではない。

#### 原因の考察

確定的な原因特定はできていないが、以下の可能性がある（優先順位は不明）。

| 可能性 | 説明 |
|--------|------|
| 長時間・大容量制約 | WebView2のHTML videoが長時間/大容量MP4の読み込みに失敗する可能性 |
| faststart未対応 | MP4のmoov atomが末尾に配置されており、先読みできずに失敗する可能性 |
| WebView2 Runtime制約 | Edge/WebView2のバッファリング・読み込み制約 |
| 仮想ホスト方式との相性 | SetVirtualHostNameToFolderMapping経由の大容量ファイル読み込み |

#### 対応方針の判断

- 自動補正（自動faststart化・プレビュー用一時ファイル生成）は実装しない
- v0.1.2ではエラーメッセージと切り分け手順の案内改善に留める
- 自動faststart化・プレビュー用一時ファイル生成はv0.2.0以降の検討事項とする

#### 原因分析

Edgeで再生できるのに本ツールで再生できない事象から、コーデック非対応ではなく読み込み方式（仮想ホスト）の問題と判断。

| 可能性 | 説明 |
|--------|------|
| 仮想ホスト経由 range request 不全 | `SetVirtualHostNameToFolderMapping` 経由のHTTP range requestが大容量MP4で正常機能しない可能性 |
| moov atom末尾配置 | faststart未対応MP4での仮想ホスト先読み失敗 |
| WebView2バッファリング制約 | 長時間・大容量ファイルの内部バッファリング上限 |

#### 対応方針の判断

読み込み方式をffile-uri主方式に切り替える。

- **理由:** Edgeで再生できる = ChromiumのネイティブFile APIは機能する。問題は仮想ホスト経由の間接アクセス。player.htmlをfile://で読み込めば動画もfile://でアクセスでき、CORS制約なし・Chroimiumネイティブ処理で安定性向上が見込める
- **フォールバック:** file-uri失敗時はplayer.htmlを再ナビゲートしてvirtual-host方式で再試行

#### 実施した変更

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `MainForm.cs` | WebView2 初期化時のplayer.html読み込みを `https://app.local/Assets/player.html`（仮想ホスト）から `file:///<appDir>/Assets/player.html`（file-uri）に変更 |
| 2 | `MainForm.cs` | `LoadVideoInPlayer` を file-uri主方式に変更。`_loadAttempt` フィールドで試行状態を管理 |
| 3 | `MainForm.cs` | `LoadVideoViaVirtualHost` メソッドを追加（フォールバック専用） |
| 4 | `MainForm.cs` | `OnNavigationCompleted` ハンドラを追加。フォールバック再ナビゲーション後に動画読み込みを実行 |
| 5 | `MainForm.cs` | `OnVideoPlayerError` にフォールバック分岐を追加。file-uri失敗時はapp.local再設定→player.html再ナビゲーション→virtual-host読み込み |
| 6 | `MainForm.cs` | `loaded`/`error` メッセージから `method` フィールドを受け取りログに記録 |
| 7 | `Assets/player.html` | `load` コマンドから `method` を受け取り `currentMethod` に保存。`loaded`/`error` メッセージで method を返送 |
| 8 | `Assets/player.html` | エラーコード4のメッセージを「Edgeで再生できる動画でも〜」に更新 |
| 9 | `docs/test_scenarios.md` | 読み込み方式確認とEdge再生確認の切り分け手順を追加（セクション8） |
| 10 | `docs/tool_design.md` | セクション4.5をv0.1.2実装方針（file-uri主/virtual-host fallback）で改訂 |
| 11 | `README.md` | プレビュー方式説明・「プレビュー不可時は変換不可」を明記 |
| 12 | `manuals/user_manual.md` | プレビュー不可時の制約（変換不可）と対処手順を更新 |
| 13 | `manuals/admin_manual.md` | ログ確認方法と切り分け手順（Step1〜3）を追記 |
| 14 | `docs/release_checklist.md` | 読み込み方式確認チェック項目を追加 |

#### 追加修正: NotAllowedError 誤判定の修正

**事象:**
- file-uri方式でファイル読み込み成功（`loadedmetadata` 発火）
- 動画時間取得成功
- その後 C# の再生ボタンが `play()` を発行 → `NotAllowedError: play() failed because the user didn't interact with the document first.` で失敗
- この失敗が `error` 型で送信されたため、フォールバック条件（`_loadAttempt == 0` + error）が成立し virtual-host へ誤フォールバック

**原因:**
WebView2/Chromiumの自動再生制限。C# の PostWebMessageAsString 経由で `play()` を呼ぶ場合、WebView2 内のユーザージェスチャーと見なされないため発生する。動画形式・読み込み方式の問題ではない。

**修正内容:**

| # | 変更対象 | 内容 |
|---|----------|------|
| 1 | `player.html` | `play` コマンドの catch で `NotAllowedError` を判別し、`error` 型ではなく `play-blocked` 型で送信。フォールバックをトリガーしない |
| 2 | `player.html` | `loadedmetadata` 発火後にオーバーレイ再生ボタン（`#play-overlay`）を表示。WebView2 内のクリック = 正規ユーザージェスチャーとして `play()` を呼ぶことで NotAllowedError を回避 |
| 3 | `player.html` | `play` イベント発火時（再生開始確定時）にオーバーレイを非表示。`play-blocked` 時は再度オーバーレイを表示 |
| 4 | `MainForm.cs` | `OnWebMessageReceived` に `play-blocked` ケースを追加。`_isPlaying = false` に戻し、ステータスに案内メッセージを表示。`OnVideoPlayerError` を呼ばないのでフォールバックしない |

#### 対応しなかった内容（v0.2.0以降）

| 事項 | 理由 |
|------|------|
| 自動faststart化 | 実装コスト、変換処理の複雑化、利用者への影響を考慮してv0.1.2対象外 |
| プレビュー用一時ファイル生成 | 同上 |
| ffprobe同梱必須化 | 配布サイズ増加、初期版では不要と判断 |
| 変換前自動正規化 | 同上 |

---

## v0.1.1 確認・修正記録

### v0.1.1 ビルド確認結果

| 対象 | 方法 | 結果 |
|------|------|------|
| FfmpegRunner.cs・ConversionSettings.cs（非WinFormsコード） | Linux環境で net8.0 コンソールプロジェクトとして dotnet build | **成功** — 0 警告、0 エラー |
| MainForm.cs（WinForms本体） | ビルド未実施 | **未実施** — 下記参照 |
| MovieConverter.csproj（net8.0-windows全体） | ビルド未実施 | **未実施** — 下記参照 |

**WinForms ビルド未実施の理由:**

Linux 環境では `Microsoft.NET.Sdk.WindowsDesktop` が存在しないため、
`net8.0-windows` + `UseWindowsForms=true` のビルドができない。
`dotnet workload search` でも Windows Desktop ワークロードは存在しなかった。

**対応方針:**

- Windows 実機または Windows CI 環境での `dotnet build -c Release` が必要
- Linux でのビルド CI は対象外とする（WinForms の根本的制約）
- 非UI コードはビルド検証済みのため、論理的なコンパイルエラーのリスクは低い

---

### v0.1.1 コードレビュー結果と修正内容

#### 修正した不具合

| # | 問題 | 修正内容 | ファイル |
|---|------|----------|---------|
| 1 | `AppDomain.CurrentDomain.BaseDirectory` はシングルファイル発行時に一時展開ディレクトリを返すため、`bin\ffmpeg\ffmpeg.exe` の検索パスが誤る | `Environment.ProcessPath` から実行ファイルの親ディレクトリを取得するよう変更 | FfmpegRunner.cs |
| 2 | WebView2 初期化完了前にファイルをドラッグ＆ドロップすると、初期化完了後もプレビューに反映されない | `WebView2_InitializationCompleted` で `_inputFile` が設定済みの場合に `LoadVideoInPlayer` を呼び出す処理を追加 | MainForm.cs |
| 3 | 新しいファイルを読み込む際に `_isPlaying` フラグがリセットされず、「再生」ボタンの表示がずれる可能性がある | `LoadFile` で `UpdatePlayPauseButton()` を追加 | MainForm.cs |
| 4 | `BtnConvert_Click` で `FileNotFoundException` / 予期しない例外が発生した場合、`_cancelSource` が Dispose されずリソースリークになる | catch ブロックで `_cancelSource?.Dispose()` を追加 | MainForm.cs |
| 5 | 動画プレイヤーでエラーが発生した場合、ログには出るがステータスや操作ボタンの状態が更新されない | `OnVideoPlayerError()` メソッドを追加してボタン無効化とステータス更新 | MainForm.cs |

#### 改善した点

| # | 改善内容 | ファイル |
|---|----------|---------|
| 1 | 動画読み込み成功時にログに動画時間を出力 | MainForm.cs |
| 2 | player.html のエラー時にプレイヤー内にエラーメッセージを表示（赤色テキスト） | Assets/player.html |
| 3 | player.html で `load` コマンド受信時にエラー表示と前の動画を非表示にする | Assets/player.html |
| 4 | エラーメッセージを詳細化（デコードエラー・非対応コーデックを区別） | Assets/player.html |

---

### v0.1.1 日本語・空白パスに関する設計確認

コードレビューにより以下を確認した（実機テストは未実施）:

| 観点 | 設計上の対応 | 状況 |
|------|------------|------|
| パスの引用符 | FFmpeg 引数の入力・出力パスを `"` で囲む | 設計済み |
| URL エンコード | `Uri.EscapeDataString()` でファイル名を URL エンコード | 設計済み |
| 仮想ホストマッピング | `SetVirtualHostNameToFolderMapping` は任意のディレクトリを指定可能 | 設計済み |
| ファイル名の無害化 | `SanitizeFileName()` で出力ファイル名の特殊文字を置換 | 設計済み |
| 実機での動作確認 | 日本語パス・スペース含むパス | **未実施** — Windows 実機が必要 |

---

### v0.1.1 WebView2・ffmpeg・コーデックに関する注意事項

**WebView2:**
- Windows 10 20H2 以降・Windows 11 は標準インストール済み
- Windows 10 2004 以前では別途「Microsoft Edge WebView2 Runtime」のインストールが必要
- 初期化失敗時はプレビューエリアにエラーメッセージを表示する
- ユーザーデータは `%TEMP%\MovieConverter_WebView2\` に保存される（変換処理には影響しない）

**ffmpeg:**
- `bin\ffmpeg\ffmpeg.exe` が配置されていない場合、起動時にログにメッセージを表示
- 変換ボタンは ffmpeg が未配置の間は無効化される
- シングルファイル発行時は `Environment.ProcessPath` で実行ファイルと同階層の `bin\ffmpeg\` を参照（v0.1.1 修正済み）

**MP4コーデック:**
- libx264 / AAC を使用するため、ffmpeg ビルドにこれらが含まれていることが必要
- WebView2 のプレイヤーは Edge（Chromium）ベースのため MP4/H.264 は標準対応
- 一部の古い形式やカスタムコーデックの MP4 はプレイヤーで再生できない場合がある
- プレイヤーで再生できない場合でも、ffmpeg が対応していれば変換は成功する可能性がある（ただしプレビューなしの操作になる）

---

### v0.1.1 未対応事項

| 事項 | 理由 |
|------|------|
| Windows 実機での全機能動作確認 | 実行環境が Linux のため |
| 日本語・空白パスの実機確認 | Windows 実機が必要 |
| WebView2 仮想ホストマッピングの動作確認 | Windows 実機が必要 |
| セルフコンテインド発行後の確認 | Windows 実機が必要 |
| 変換進捗プログレスバー | v0.1.1 対象外（次バージョン候補） |
| FFmpeg 出力から進捗率パース | v0.1.1 対象外（次バージョン候補） |

---

### v0.2.0 以降の候補

| 機能 | 優先度 | 備考 |
|------|--------|------|
| 変換進捗のプログレスバー表示 | 中 | FFmpeg の `time=` 出力をパース |
| Windows CI でのビルド自動確認 | 高 | GitHub Actions / Azure Pipelines |
| 日本語・空白パスの自動テスト | 中 | CI で確認 |
| MP4 以外の形式への対応 | 低 | 需要確認後に検討 |
| 複数ファイル一括変換 | 低 | 需要確認後に検討 |

---

## 1. 作成したファイル

### ソースコード

| ファイル | 内容 |
|----------|------|
| `src/MovieConverter/MovieConverter.csproj` | .NET 8 WinForms プロジェクトファイル |
| `src/MovieConverter/Program.cs` | アプリエントリポイント |
| `src/MovieConverter/ConversionSettings.cs` | 変換設定データモデル（品質・解像度列挙型） |
| `src/MovieConverter/FfmpegRunner.cs` | ffmpeg.exe 外部プロセス実行クラス |
| `src/MovieConverter/MainForm.cs` | メインフォーム（UI + ロジック全体） |
| `src/MovieConverter/Assets/player.html` | WebView2 用動画プレイヤーHTML |

### ドキュメント

| ファイル | 内容 |
|----------|------|
| `README.md` | ツール概要・セットアップ手順 |
| `docs/tool_design.md` | 技術設計書・判断理由 |
| `docs/release_checklist.md` | リリース前チェックリスト |
| `docs/test_scenarios.md` | テストシナリオ（正常系・異常系・再実行系） |
| `docs/operation_handover.md` | 運用引継ぎ文書 |
| `docs/ffmpeg_setup.md` | ffmpeg.exe 配置手順 |
| `docs/license_notice.md` | ライセンス確認事項 |
| `manuals/admin_manual.md` | 管理者向け手順書 |
| `manuals/operator_manual.md` | 運用担当部署向け手順書 |
| `manuals/user_manual.md` | 利用者向け操作手順書 |
| `development_report.md` | 本文書 |

---

## 2. 主要な設計判断

### 2-1. フレームワーク: .NET 8 + WinForms

**判断:** WPFよりWinFormsを採用した。WinFormsは実装容易性・保守性が高く、XAML習熟が不要。
.NET 8はLTSバージョンであり、async/await・System.Text.Json等の現代的なAPIが利用可能。

**トレードオフ:** .NET Framework 4.8（既定でWin10/11にインストール済み）と比較して、
.NET 8ランタイムの別途インストールが必要になる。対応策として `--self-contained` 発行を推奨。

### 2-2. 動画プレビュー: WebView2 + HTML5 video

**判断:** HTML5の`<video>`タグはMP4/H.264の標準サポートが良好で、
JavaScript経由でC#との双方向通信が可能。ローカルHTMLのみを使用し外部依存なし。

**仮想ホスト方式の採用:** WebView2の `SetVirtualHostNameToFolderMapping` により、
`file://` の同一オリジン問題を回避してローカル動画ファイルをHTTPSとして安全にサーブ。

### 2-3. ffmpeg.exe 外部プロセス呼び出し方式

**判断:** DLL直接利用より外部プロセスを選択。理由はライセンスリスクの相対的低減と実装コストの削減。
プロセス境界での分離により、FFmpegのクラッシュがアプリに影響しない安全性も得られる。

### 2-4. 再エンコード方式（-c copy を使わない）

**判断:** `-c copy` はカット位置がキーフレームに丸められるため、指定位置とのずれが生じる。
一般職員向けツールでは「指定した位置と違う」という苦情への対応コストが高い。
再エンコード（libx264）により精度を優先した。処理時間の増加は庁内用途では許容範囲と判断。

### 2-5. 初期版対象をMP4のみとした理由

一般職員が扱う動画の大半はMP4形式である。複数形式への対応は品質保証・テストコストが増大し、
初期版の安定性を損なうリスクがある。需要を確認しながらv0.2.0以降で段階的に対応する。

---

## 3. 方針適合の確認

| 確認項目 | 状況 |
|----------|------|
| 外部通信を行わない | ○ WebView2はローカルHTMLのみ、外部URL不使用 |
| 外部API/CDN依存なし | ○ NuGet取得はビルド時のみ、実行時は不要 |
| 個人情報を保存しない | ○ ログはメモリ上のTextBoxのみ、ファイル出力なし |
| 元ファイル上書きなし | ○ タイムスタンプ付きファイル名、-y オプション不使用 |
| 管理者権限不要 | ○ 一般ユーザー権限で実行可能な設計 |
| 専門用語を避けたUI | ○ 「圧縮設定」「開始位置」等、一般的な表現を使用 |
| エラーに対処が書かれている | ○ エラーダイアログに「〜してください」の案内を含む |

---

## 4. 整形確認結果

### Markdown文書

| ファイル | 行数確認 | 状況 |
|----------|---------|------|
| README.md | 約110行 | 適切 |
| docs/tool_design.md | 約180行 | 適切 |
| docs/test_scenarios.md | 約160行 | 適切 |
| docs/release_checklist.md | 約100行 | 適切 |
| docs/operation_handover.md | 約130行 | 適切 |
| docs/ffmpeg_setup.md | 約90行 | 適切 |
| docs/license_notice.md | 約90行 | 適切 |
| manuals/admin_manual.md | 約150行 | 適切 |
| manuals/operator_manual.md | 約110行 | 適切 |
| manuals/user_manual.md | 約120行 | 適切 |

### コード

| ファイル | 状況 |
|----------|------|
| MainForm.cs | 約570行、クラス・メソッド・条件分岐ごとに適切に改行・インデント |
| FfmpegRunner.cs | 約160行、適切に構造化 |
| ConversionSettings.cs | 約20行、シンプルな定義 |

---

## 5. ビルド確認

**結果: 未実施**

**理由:** 実行環境（Linuxコンテナ）に `dotnet` コマンドがインストールされていないため。

**対応方針:**

- Windows開発環境でのビルド確認が必要
- ビルドコマンド: `dotnet build -c Release` （`src/MovieConverter/` フォルダで実行）
- セルフコンテインド発行: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`
- NuGetパッケージ (`Microsoft.Web.WebView2`) のリストアにインターネット接続が必要

**実機確認時の注意点:**

1. `Microsoft.Web.WebView2` NuGet パッケージのバージョン `1.0.2365.46` が存在するか確認する
   （存在しない場合は最新の安定版バージョンに変更する）
2. WebView2 の仮想ホストマッピング（`SetVirtualHostNameToFolderMapping`）の動作確認が必要
3. 日本語・スペース含むパスでの変換動作確認が必要

---

## 6. 判断しづらかった点

### WebView2 でのローカル動画ファイルのサーブ方法

`NavigateToString` で生成されたHTML（null オリジン）から `file://` URLへのアクセスはCORSにより制限される。
解決策として `SetVirtualHostNameToFolderMapping` で動画ファイルのディレクトリを仮想HTTPSホストにマップする方式を採用した。
この方法の動作確認は実機で行う必要がある。

### ffmpeg.exe のパス取得

`AppDomain.CurrentDomain.BaseDirectory` を使用。
セルフコンテインド単一ファイル発行時は `Process.GetCurrentProcess().MainModule?.FileName` 等の別アプローチが必要な場合がある。
`bin\ffmpeg\` フォルダの解凍先を実行ファイルと同じフォルダにするよう配布手順で案内した。

---

## 7. 未対応事項と対応予定

| 事項 | 優先度 | 対応予定 |
|------|--------|---------|
| dotnet build による実機ビルド確認 | 高 | Windows環境で実施 |
| 実機での総合テスト | 高 | Windows 10/11 実機で実施 |
| ffmpegの進捗プログレスバー | 中 | v0.2.0 以降 |
| MP4以外の形式対応 | 低 | 需要確認後に検討 |
| セルフコンテインド発行の動作確認 | 中 | Windows環境で実施 |
| AppDomain.BaseDirectory が単一ファイル発行時に正しいか確認 | 高 | Windows環境で実施 |

---

## 8. 今回あえて作成しなかったもの

| 対象 | 理由 |
|------|------|
| MainForm.Designer.cs | 全コントロールをMainForm.csに集約し、保守性を優先した（Visual Studio設計者形式を使わない） |
| app.manifest（DPIマニフェスト） | 初期版では優先度が低いため省略。視覚的な問題が生じた場合にv0.2.0で追加する |
| 設定ファイル（config/appsettings） | 保存すべき設定が現時点では存在しないため不要 |
| ログファイル出力 | 個人情報保護の観点からファイルログは採用しない。画面ログのみとする |

---

## 9. FFmpeg同梱に関するライセンス確認事項

- FFmpeg本体はリポジトリに含めていない
- `bin/ffmpeg/` フォルダは空で作成済み（配置場所の確保のみ）
- 利用時は `docs/license_notice.md` に従い、GPL/LGPL の区別を確認すること
- FFmpegは改変せず、外部プロセスとして呼び出すのみ

---

*作成: 2026-05-23  最終更新: 2026-05-24  バージョン: v0.3.3*
