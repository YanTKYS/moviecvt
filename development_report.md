# 開発報告書 — 動画簡易変換ツール v0.1.0

| 項目 | 内容 |
|------|------|
| ツール名 | 動画簡易変換ツール |
| バージョン | v0.1.1 |
| 初回作成日 | 2026-05-23 |
| v0.1.1更新日 | 2026-05-23 |
| 参照ガイド | reference/guide_context.md（同梱方式） |
| GitHub Pages | 403エラーにより参照不可 — guide_context.md で代替 |

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

*作成: 2026-05-23  更新（v0.1.1）: 2026-05-23*
