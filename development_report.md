# 開発報告書 — 動画簡易変換ツール v0.1.0

| 項目 | 内容 |
|------|------|
| ツール名 | 動画簡易変換ツール |
| バージョン | v0.1.6 |
| 初回作成日 | 2026-05-23 |
| v0.1.1更新日 | 2026-05-23 |
| v0.1.2更新日 | 2026-05-23 |
| v0.1.3更新日 | 2026-05-24 |
| v0.1.4更新日 | 2026-05-24 |
| v0.1.5更新日 | 2026-05-24 |
| v0.1.6更新日 | 2026-05-24 |
| 参照ガイド | reference/guide_context.md（同梱方式） |
| GitHub Pages | 403エラーにより参照不可 — guide_context.md で代替 |

---

## v0.2.0 以降の機能候補

v0.1.x の確認・フィードバックを踏まえ、以下を v0.2.0 以降の候補として整理する。

| 機能 | 概要 | 優先度 |
|------|------|--------|
| 自動 faststart 化 | 出力 MP4 に `-movflags +faststart` を自動適用。WebView2 プレビュー互換性・再生開始速度の向上 | 高 |
| 出力先フォルダ選択 | 現在は入力ファイルと同じフォルダ固定。任意フォルダへの出力を可能にする | 中 |
| 速度優先プリセット追加 | `-preset fast`/`veryfast` の選択肢を追加。変換速度と画質のトレードオフを選択可能に | 中 |
| WebView2 以外のプレビュー方式 | プレビューできない MP4 への対応。MediaPlayer コントロール等の代替プレビュー方式の検討 | 中 |
| 出力ファイル名指定 | タイムスタンプ自動付加に加えて任意のファイル名も指定できるようにする | 低 |
| 変換残り時間（ETA）表示 | ffmpeg の speed 値から残り時間を推定表示（精度は低い） | 低 |
| ffprobe による動画情報表示 | コーデック・フレームレート・ビットレート等の情報を UI 上に表示 | 低 |
| 複数ファイル変換 | 同じ設定で複数の MP4 をまとめて変換 | 低 |
| 複数区間カット | 1 つの動画から複数の範囲を一度に切り出す | 低 |
| サムネイル一覧 | プレビュー前にサムネイル一覧で内容を確認できる | 低 |

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

*作成: 2026-05-23  更新（v0.1.1）: 2026-05-23  更新（v0.1.3）: 2026-05-24*
