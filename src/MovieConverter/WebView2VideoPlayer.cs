using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MovieConverter
{
    /// <summary>
    /// WebView2 + player.html を使った IVideoPlayer 実装。
    /// file-uri 方式（主）と virtual-host 方式（フォールバック）の自動切り替えを内包する。
    /// </summary>
    public class WebView2VideoPlayer : IVideoPlayer
    {
        // ─── コントロール ────────────────────────────────────────────
        private readonly Panel _container;
        private readonly WebView2 _webView;
        private readonly Label _hint;

        // ─── 状態 ────────────────────────────────────────────────────
        private string _currentFile = string.Empty;
        private int _loadAttempt;           // 0 = file-uri（主）、1 = virtual-host（フォールバック）
        private string? _pendingLoadAfterNav;
        private bool _initFailed;

        // ─── IVideoPlayer プロパティ ──────────────────────────────────
        public Control PreviewControl => _container;
        public bool IsReady { get; private set; }

        // ─── IVideoPlayer イベント ────────────────────────────────────
        public event Action<double>? VideoLoaded;
        public event Action<double, double>? TimeUpdated;
        public event Action? PlaybackStarted;
        public event Action? PlaybackPaused;
        public event Action? PlaybackEnded;
        public event Action? PlaybackBlocked;
        public event Action<string>? VideoError;
        public event Action<string>? FileDropped;
        public event Action<string>? LogMessage;

        // ─── コンストラクタ ───────────────────────────────────────────
        public WebView2VideoPlayer()
        {
            _hint = new Label
            {
                Text = "MP4ファイルを選択すると、ここにプレビューが表示されます",
                ForeColor = Color.FromArgb(150, 150, 150),
                BackColor = Color.Transparent,
                Font = new Font("Meiryo UI", 10f),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            _webView.CoreWebView2InitializationCompleted += OnInitializationCompleted;

            _container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Margin = new Padding(0, 2, 0, 2)
            };
            _container.Controls.Add(_webView);
            _container.Controls.Add(_hint);
        }

        // ─── 初期化 ───────────────────────────────────────────────────
        public async Task InitializeAsync(string appDirectory)
        {
            try
            {
                string userDataDir = Path.Combine(Path.GetTempPath(), "MovieConverter_WebView2");
                var env = await CoreWebView2Environment
                    .CreateAsync(null, userDataDir)
                    .ConfigureAwait(true);
                await _webView.EnsureCoreWebView2Async(env).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _initFailed = true;
                _hint.Text =
                    "動画プレビューを初期化できませんでした。\n" +
                    "WebView2ランタイム（Microsoft Edge WebView2 Runtime）が\n" +
                    "インストールされているか確認してください。";
                LogMessage?.Invoke($"[WebView2 エラー] {ex.Message}");
                // 初期化失敗済みのままファイルが選択された場合に備えて、
                // ここでは _currentFile が空なので VideoError は LoadVideo 側で発火する
            }
        }

        private void OnInitializationCompleted(
            object? sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                _initFailed = true;
                LogMessage?.Invoke($"[WebView2 初期化失敗] {e.InitializationException?.Message}");
                if (!string.IsNullOrEmpty(_currentFile))
                    VideoError?.Invoke("WebView2 が初期化できませんでした");
                return;
            }
            try
            {
                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                // D&D で MP4 をドロップした場合のナビゲーションと新規ウィンドウをキャンセルしてアプリ側に委譲
                _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;

                string appDir = Path.GetDirectoryName(Environment.ProcessPath)
                    ?? AppContext.BaseDirectory;
                string playerPath = Path.Combine(appDir, "Assets", "player.html");
                _webView.CoreWebView2.Navigate(new Uri(playerPath).AbsoluteUri);

                IsReady = true;
                _webView.Visible = true;
                _hint.Visible = false;
                LogMessage?.Invoke("[準備完了] 動画プレビューの初期化に成功しました。（file-uri方式）");

                // 初期化完了前にファイルが選択されていた場合は読み込む
                if (!string.IsNullOrEmpty(_currentFile))
                {
                    LogMessage?.Invoke("[再読み込み] 選択済みファイルをプレビューに読み込みます。");
                    LoadVideoInternal();
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"[WebView2 セットアップ エラー] {ex.Message}");
            }
        }

        // ─── 動画読み込み ─────────────────────────────────────────────
        public void LoadVideo(string filePath)
        {
            _currentFile = filePath;
            if (_initFailed)
            {
                // WebView2 が初期化失敗済み → 即座にエラーを通知して MainForm に制御を渡す
                VideoError?.Invoke("WebView2 が初期化できませんでした");
                return;
            }
            if (!IsReady) return;
            LoadVideoInternal();
        }

        private void LoadVideoInternal()
        {
            _loadAttempt = 0;
            string src = new Uri(_currentFile).AbsoluteUri;
            LogMessage?.Invoke("[プレイヤー] file-uri方式で読み込みます");
            Send($"{{\"cmd\":\"load\",\"src\":\"{EscapeJson(src)}\",\"method\":\"file-uri\"}}");
        }

        private void LoadVideoViaVirtualHost()
        {
            string? dir = Path.GetDirectoryName(_currentFile);
            if (dir == null) return;
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "video.local", dir, CoreWebView2HostResourceAccessKind.Allow);
            string fileName = Uri.EscapeDataString(Path.GetFileName(_currentFile));
            string src = $"https://video.local/{fileName}";
            LogMessage?.Invoke("[フォールバック] virtual-host方式で読み込みます");
            Send($"{{\"cmd\":\"load\",\"src\":\"{EscapeJson(src)}\",\"method\":\"virtual-host\"}}");
        }

        private void SetupAppLocalVirtualHost()
        {
            string appDir = Path.GetDirectoryName(Environment.ProcessPath)
                ?? AppContext.BaseDirectory;
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", appDir, CoreWebView2HostResourceAccessKind.Allow);
        }

        // ─── 再生コマンド ─────────────────────────────────────────────
        public void Play()   => Send("{\"cmd\":\"play\"}");
        public void Pause()  => Send("{\"cmd\":\"pause\"}");
        public void Seek(double seconds) => Send($"{{\"cmd\":\"seek\",\"t\":{seconds:F3}}}");
        public void SetVolume(double volume) => Send($"{{\"cmd\":\"volume\",\"v\":{volume:F2}}}");
        public void SetMute(bool muted)  =>
            Send($"{{\"cmd\":\"mute\",\"on\":{(muted ? "true" : "false")}}}");

        private void Send(string json)
        {
            if (!IsReady) return;
            try { _webView.CoreWebView2.PostWebMessageAsString(json); }
            catch { /* WebView2 が破棄されている場合は無視 */ }
        }

        // ─── WebView2 イベントハンドラ ────────────────────────────────
        private void OnWebMessageReceived(
            object? sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            string json = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string type = root.TryGetProperty("type", out var tp)
                    ? tp.GetString() ?? "" : "";

                switch (type)
                {
                    case "loaded":
                        double duration = root.TryGetProperty("d", out var dp)
                            ? dp.GetDouble() : 0;
                        string method = root.TryGetProperty("method", out var mp)
                            ? mp.GetString() ?? "unknown" : "unknown";
                        LogMessage?.Invoke($"[読み込み成功] 方式: {method}");
                        VideoLoaded?.Invoke(duration);
                        break;

                    case "timeupdate":
                        double t = root.TryGetProperty("t", out var ttp)
                            ? ttp.GetDouble() : 0;
                        double d = root.TryGetProperty("d", out var dp2)
                            ? dp2.GetDouble() : 0;
                        TimeUpdated?.Invoke(t, d);
                        break;

                    case "playing":
                        PlaybackStarted?.Invoke();
                        break;

                    case "paused":
                        PlaybackPaused?.Invoke();
                        break;

                    case "ended":
                        PlaybackEnded?.Invoke();
                        break;

                    case "play-blocked":
                        LogMessage?.Invoke(
                            "[再生] 自動再生制限のため再生できませんでした。" +
                            "プレイヤー内の ▶ ボタンを押して再生してください。");
                        PlaybackBlocked?.Invoke();
                        break;

                    case "error":
                        string msg = root.TryGetProperty("msg", out var msgp)
                            ? msgp.GetString() ?? "不明なエラー" : "不明なエラー";
                        string errMethod = root.TryGetProperty("method", out var emp)
                            ? emp.GetString() ?? "unknown" : "unknown";
                        OnPlayerError(msg, errMethod);
                        break;
                }
            }
            catch { /* JSON パースエラーは無視 */ }
        }

        private void OnPlayerError(string message, string method)
        {
            // file-uri 方式失敗 → virtual-host 方式にフォールバック
            if (_loadAttempt == 0 && !string.IsNullOrEmpty(_currentFile))
            {
                _loadAttempt = 1;
                LogMessage?.Invoke("[フォールバック] file-uri方式が失敗しました。virtual-host方式を試みます。");
                SetupAppLocalVirtualHost();
                _pendingLoadAfterNav = _currentFile;
                _webView.CoreWebView2.Navigate("https://app.local/Assets/player.html");
                return;
            }
            // 両方式とも失敗
            LogMessage?.Invoke($"[プレイヤー エラー] プレビューで再生できませんでした。（方式: {method}）");
            LogMessage?.Invoke($"  詳細: {message}");
            VideoError?.Invoke(message);
        }

        private void OnNavigationCompleted(
            object? sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess || string.IsNullOrEmpty(_pendingLoadAfterNav)) return;
            _pendingLoadAfterNav = null;
            LoadVideoViaVirtualHost();
        }

        private void OnNavigationStarting(
            object? sender,
            CoreWebView2NavigationStartingEventArgs e)
        {
            if (!e.Uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                string localPath = new Uri(e.Uri).LocalPath;
                if (Path.GetExtension(localPath).ToLowerInvariant() == ".mp4")
                {
                    e.Cancel = true;
                    FileDropped?.Invoke(localPath);
                }
            }
            catch { /* URI パース失敗は無視 */ }
        }

        private void OnNewWindowRequested(
            object? sender,
            CoreWebView2NewWindowRequestedEventArgs e)
        {
            // D&D で MP4 を WebView2 にドロップすると別ウィンドウ再生しようとする場合をキャンセル
            e.Handled = true;
            try
            {
                if (e.Uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    string localPath = new Uri(e.Uri).LocalPath;
                    if (Path.GetExtension(localPath).ToLowerInvariant() == ".mp4")
                        FileDropped?.Invoke(localPath);
                }
            }
            catch { /* URI パース失敗は無視 */ }
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
