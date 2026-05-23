using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MovieConverter
{
    public class MainForm : Form
    {
        // ─── コントロール ───────────────────────────────────────────
        private Panel pnlFile = null!;
        private Button btnBrowse = null!;
        private Label lblFilePath = null!;
        private Label lblFileSize = null!;

        private Panel pnlPreview = null!;
        private WebView2 webView2 = null!;
        private Label lblPreviewHint = null!;

        private Panel pnlSeek = null!;
        private Label lblCurrentTime = null!;
        private TrackBar trkSeek = null!;
        private Label lblTotalTime = null!;

        private Panel pnlPlayback = null!;
        private Button btnBack5 = null!;
        private Button btnPlayPause = null!;
        private Button btnForward5 = null!;

        private Panel pnlCut = null!;
        private Button btnSetStart = null!;
        private TextBox txtStartTime = null!;
        private Label lblStartLabel = null!;
        private Button btnSetEnd = null!;
        private TextBox txtEndTime = null!;
        private Label lblEndLabel = null!;
        private Label lblRange = null!;

        private Panel pnlSettings = null!;
        private Label lblQualityLabel = null!;
        private ComboBox cmbQuality = null!;
        private Label lblResolutionLabel = null!;
        private ComboBox cmbResolution = null!;

        private Panel pnlConvert = null!;
        private Button btnConvert = null!;
        private Button btnCancel = null!;
        private Label lblStatus = null!;

        private TextBox txtLog = null!;

        // ─── 状態 ────────────────────────────────────────────────────
        private string? _inputFile;
        private double _duration;
        private double _currentTime;
        private double? _startSeconds;
        private double? _endSeconds;
        private bool _isPlaying;
        private bool _isSeekBarUpdating;
        private bool _webView2Ready;
        private bool _videoLoaded;
        private CancellationTokenSource? _cancelSource;
        private readonly FfmpegRunner _ffmpeg = new();

        // ─── コンストラクタ ──────────────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            SetupDragDrop();
            _ = InitializeWebView2Async();
            CheckFfmpegAvailability();
        }

        // ─── UI 初期化 ───────────────────────────────────────────────
        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "動画簡易変換ツール  v0.1.0";
            ClientSize = new Size(820, 900);
            MinimumSize = new Size(780, 820);
            Font = new Font("Meiryo UI", 9f);
            BackColor = Color.FromArgb(245, 245, 248);

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                Padding = new Padding(8),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                BackColor = Color.Transparent
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));    // 0: file
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // 1: preview
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));    // 2: seek bar
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));    // 3: playback buttons
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));   // 4: cut position
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));    // 5: settings
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));    // 6: convert
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));   // 7: log

            // ── Row 0: ファイル選択 ──
            pnlFile = CreateSectionPanel();
            pnlFile.Padding = new Padding(4, 0, 4, 0);

            btnBrowse = new Button
            {
                Text = "ファイルを選択...",
                Size = new Size(140, 30),
                Location = new Point(4, 18),
                UseVisualStyleBackColor = true
            };
            btnBrowse.Click += BtnBrowse_Click;

            lblFilePath = new Label
            {
                Text = "ファイル: (未選択)",
                Location = new Point(154, 12),
                Size = new Size(460, 18),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoEllipsis = true
            };

            lblFileSize = new Label
            {
                Text = "",
                Location = new Point(154, 32),
                Size = new Size(460, 18),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            pnlFile.Controls.AddRange(new Control[] { btnBrowse, lblFilePath, lblFileSize });
            tableLayout.Controls.Add(pnlFile, 0, 0);

            // ── Row 1: 動画プレビュー ──
            pnlPreview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Margin = new Padding(0, 2, 0, 2)
            };

            lblPreviewHint = new Label
            {
                Text = "MP4ファイルを選択すると、ここにプレビューが表示されます",
                ForeColor = Color.FromArgb(150, 150, 150),
                BackColor = Color.Transparent,
                Font = new Font("Meiryo UI", 10f),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            webView2 = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            webView2.CoreWebView2InitializationCompleted += WebView2_InitializationCompleted;

            pnlPreview.Controls.Add(webView2);
            pnlPreview.Controls.Add(lblPreviewHint);
            tableLayout.Controls.Add(pnlPreview, 0, 1);

            // ── Row 2: シークバー ──
            pnlSeek = CreateSectionPanel();
            pnlSeek.Padding = new Padding(4, 0, 4, 0);

            lblCurrentTime = new Label
            {
                Text = "00:00:00",
                Location = new Point(4, 10),
                Size = new Size(68, 22),
                Font = new Font("Meiryo UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleRight
            };

            trkSeek = new TrackBar
            {
                Location = new Point(78, 6),
                Size = new Size(580, 30),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                TickStyle = TickStyle.None,
                Enabled = false
            };
            trkSeek.Scroll += TrkSeek_Scroll;

            lblTotalTime = new Label
            {
                Text = "00:00:00",
                Location = new Point(664, 10),
                Size = new Size(68, 22),
                Font = new Font("Meiryo UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlSeek.Controls.AddRange(new Control[] { lblCurrentTime, trkSeek, lblTotalTime });
            tableLayout.Controls.Add(pnlSeek, 0, 2);

            // ── Row 3: 再生コントロール ──
            pnlPlayback = CreateSectionPanel();
            pnlPlayback.Padding = new Padding(4, 2, 4, 2);

            btnBack5 = CreatePlayButton("◀ 5秒戻る", 200, 8);
            btnBack5.Click += (s, e) => SeekRelative(-5.0);

            btnPlayPause = CreatePlayButton("▶  再生", 360, 8);
            btnPlayPause.Width = 120;
            btnPlayPause.Font = new Font("Meiryo UI", 9f, FontStyle.Bold);
            btnPlayPause.Click += BtnPlayPause_Click;
            btnPlayPause.Enabled = false;

            btnForward5 = CreatePlayButton("5秒進む ▶", 490, 8);
            btnForward5.Click += (s, e) => SeekRelative(5.0);

            pnlPlayback.Controls.AddRange(new Control[] { btnBack5, btnPlayPause, btnForward5 });
            tableLayout.Controls.Add(pnlPlayback, 0, 3);

            // ── Row 4: カット位置 ──
            pnlCut = CreateSectionPanel();
            pnlCut.Padding = new Padding(4, 4, 4, 4);

            btnSetStart = new Button
            {
                Text = "現在位置を開始に設定",
                Location = new Point(4, 8),
                Size = new Size(160, 28),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnSetStart.Click += BtnSetStart_Click;

            lblStartLabel = new Label
            {
                Text = "開始位置:",
                Location = new Point(174, 13),
                Size = new Size(60, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtStartTime = new TextBox
            {
                Location = new Point(238, 11),
                Size = new Size(90, 24),
                Text = "--:--:--",
                TextAlign = HorizontalAlignment.Center,
                Enabled = false
            };
            txtStartTime.Leave += TxtStartTime_Leave;

            btnSetEnd = new Button
            {
                Text = "現在位置を終了に設定",
                Location = new Point(4, 44),
                Size = new Size(160, 28),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnSetEnd.Click += BtnSetEnd_Click;

            lblEndLabel = new Label
            {
                Text = "終了位置:",
                Location = new Point(174, 49),
                Size = new Size(60, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtEndTime = new TextBox
            {
                Location = new Point(238, 47),
                Size = new Size(90, 24),
                Text = "--:--:--",
                TextAlign = HorizontalAlignment.Center,
                Enabled = false
            };
            txtEndTime.Leave += TxtEndTime_Leave;

            lblRange = new Label
            {
                Text = "指定範囲: --",
                Location = new Point(338, 28),
                Size = new Size(240, 28),
                Font = new Font("Meiryo UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 160),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlCut.Controls.AddRange(new Control[]
            {
                btnSetStart, lblStartLabel, txtStartTime,
                btnSetEnd, lblEndLabel, txtEndTime,
                lblRange
            });
            tableLayout.Controls.Add(pnlCut, 0, 4);

            // ── Row 5: 変換設定 ──
            pnlSettings = CreateSectionPanel();
            pnlSettings.Padding = new Padding(4, 4, 4, 4);

            lblQualityLabel = new Label
            {
                Text = "圧縮設定:",
                Location = new Point(4, 18),
                Size = new Size(70, 24),
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbQuality = new ComboBox
            {
                Location = new Point(80, 16),
                Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbQuality.Items.AddRange(new object[] { "画質優先", "標準", "容量優先" });
            cmbQuality.SelectedIndex = 1; // 標準

            lblResolutionLabel = new Label
            {
                Text = "解像度:",
                Location = new Point(240, 18),
                Size = new Size(60, 24),
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbResolution = new ComboBox
            {
                Location = new Point(308, 16),
                Size = new Size(110, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbResolution.Items.AddRange(new object[] { "元のまま", "720p", "480p" });
            cmbResolution.SelectedIndex = 1; // 720p

            var lblOutputNote = new Label
            {
                Text = "出力先: 元ファイルと同じフォルダ",
                Location = new Point(440, 18),
                Size = new Size(290, 24),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlSettings.Controls.AddRange(new Control[]
            {
                lblQualityLabel, cmbQuality,
                lblResolutionLabel, cmbResolution,
                lblOutputNote
            });
            tableLayout.Controls.Add(pnlSettings, 0, 5);

            // ── Row 6: 変換実行 ──
            pnlConvert = CreateSectionPanel();
            pnlConvert.Padding = new Padding(4, 4, 4, 4);

            btnConvert = new Button
            {
                Text = "変換実行",
                Location = new Point(4, 8),
                Size = new Size(110, 32),
                UseVisualStyleBackColor = true,
                Font = new Font("Meiryo UI", 9f, FontStyle.Bold),
                Enabled = false
            };
            btnConvert.Click += BtnConvert_Click;

            btnCancel = new Button
            {
                Text = "キャンセル",
                Location = new Point(124, 8),
                Size = new Size(90, 32),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnCancel.Click += BtnCancel_Click;

            lblStatus = new Label
            {
                Text = "状態: 待機中",
                Location = new Point(228, 13),
                Size = new Size(500, 22),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            pnlConvert.Controls.AddRange(new Control[] { btnConvert, btnCancel, lblStatus });
            tableLayout.Controls.Add(pnlConvert, 0, 6);

            // ── Row 7: ログ ──
            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250),
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Consolas", 8.5f),
                Margin = new Padding(0, 2, 0, 0),
                WordWrap = false
            };
            tableLayout.Controls.Add(txtLog, 0, 7);

            Controls.Add(tableLayout);
            ResumeLayout(false);
        }

        private static Panel CreateSectionPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 2, 0, 2),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static Button CreatePlayButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(110, 28),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
        }

        // ─── ドラッグ＆ドロップ ─────────────────────────────────────
        private void SetupDragDrop()
        {
            AllowDrop = true;
            DragEnter += (s, e) =>
            {
                if (e.Data != null &&
                    e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            DragDrop += (s, e) =>
            {
                if (e.Data == null) return;
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || files.Length == 0) return;
                LoadFile(files[0]);
            };
        }

        // ─── FFmpeg 存在確認 ──────────────────────────────────────────
        private void CheckFfmpegAvailability()
        {
            if (!_ffmpeg.IsAvailable)
            {
                AppendLog("⚠ ffmpeg.exe が見つかりません。");
                AppendLog($"  配置場所: {_ffmpeg.FfmpegPath}");
                AppendLog("  docs/ffmpeg_setup.md を参照して配置してください。");
                SetStatus("⚠ ffmpeg.exe 未配置 — 変換は実行できません", Color.OrangeRed);
            }
        }

        // ─── WebView2 初期化 ──────────────────────────────────────────
        private async Task InitializeWebView2Async()
        {
            try
            {
                string userDataDir = Path.Combine(
                    Path.GetTempPath(), "MovieConverter_WebView2");
                var env = await CoreWebView2Environment
                    .CreateAsync(null, userDataDir)
                    .ConfigureAwait(true); // UI スレッドに戻る

                await webView2.EnsureCoreWebView2Async(env)
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                lblPreviewHint.Text =
                    "動画プレビューを初期化できませんでした。\n" +
                    "WebView2ランタイム（Microsoft Edge WebView2 Runtime）が\n" +
                    "インストールされているか確認してください。";
                AppendLog($"[WebView2 エラー] {ex.Message}");
            }
        }

        private void WebView2_InitializationCompleted(
            object? sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                AppendLog($"[WebView2 初期化失敗] {e.InitializationException?.Message}");
                return;
            }

            try
            {
                // アプリディレクトリを仮想ホストにマップ（プレイヤーHTML読み込み用）
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                webView2.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "app.local", appDir,
                    CoreWebView2HostResourceAccessKind.Allow);

                webView2.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                webView2.CoreWebView2.Navigate("https://app.local/Assets/player.html");

                _webView2Ready = true;
                webView2.Visible = true;
                lblPreviewHint.Visible = false;
                AppendLog("[準備完了] 動画プレビューの初期化に成功しました。");
            }
            catch (Exception ex)
            {
                AppendLog($"[WebView2 セットアップ エラー] {ex.Message}");
            }
        }

        // ─── WebMessage (JS → C#) ────────────────────────────────────
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
                string type = root.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString() ?? ""
                    : "";

                switch (type)
                {
                    case "loaded":
                        _duration = root.TryGetProperty("d", out var dp)
                            ? dp.GetDouble() : 0;
                        _videoLoaded = true;
                        OnVideoLoaded();
                        break;

                    case "timeupdate":
                        if (root.TryGetProperty("t", out var tp))
                            _currentTime = tp.GetDouble();
                        if (root.TryGetProperty("d", out var dp2) && dp2.GetDouble() > 0)
                            _duration = dp2.GetDouble();
                        UpdateTimeDisplay();
                        break;

                    case "playing":
                        _isPlaying = true;
                        UpdatePlayPauseButton();
                        break;

                    case "paused":
                    case "ended":
                        _isPlaying = false;
                        UpdatePlayPauseButton();
                        break;

                    case "error":
                        string msg = root.TryGetProperty("msg", out var mp)
                            ? mp.GetString() ?? "不明なエラー"
                            : "不明なエラー";
                        AppendLog($"[プレイヤー エラー] {msg}");
                        break;
                }
            }
            catch { /* JSON パースエラーは無視 */ }
        }

        // ─── プレイヤーへのコマンド送信 (C# → JS) ───────────────────
        private void SendToPlayer(string json)
        {
            if (!_webView2Ready) return;
            try
            {
                webView2.CoreWebView2.PostWebMessageAsString(json);
            }
            catch { /* WebView2 が破棄されている場合は無視 */ }
        }

        private void LoadVideoInPlayer(string filePath)
        {
            if (!_webView2Ready) return;

            string? dir = Path.GetDirectoryName(filePath);
            if (dir == null) return;

            // 動画ディレクトリを仮想ホストにマップ（ファイルアクセス用）
            webView2.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "video.local", dir,
                CoreWebView2HostResourceAccessKind.Allow);

            string fileName = Uri.EscapeDataString(Path.GetFileName(filePath));
            string src = $"https://video.local/{fileName}";
            SendToPlayer($"{{\"cmd\":\"load\",\"src\":\"{EscapeJsonString(src)}\"}}");
        }

        private static string EscapeJsonString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        // ─── ファイル選択 ────────────────────────────────────────────
        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "MP4ファイルを選択してください",
                Filter = "MP4ファイル (*.mp4)|*.mp4|すべてのファイル (*.*)|*.*",
                FilterIndex = 1
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadFile(dlg.FileName);
        }

        private void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ShowUserError("ファイルが見つかりません。",
                    "指定されたファイルが存在しません。\n別のファイルを選択してください。");
                return;
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext != ".mp4")
            {
                ShowUserError("対応していないファイル形式です。",
                    "このツールは MP4 ファイルのみ対応しています。\n" +
                    $"選択されたファイル: {Path.GetFileName(filePath)}\n\n" +
                    ".mp4 ファイルを選択してください。");
                return;
            }

            _inputFile = filePath;
            _videoLoaded = false;
            _startSeconds = null;
            _endSeconds = null;
            _currentTime = 0;
            _duration = 0;
            _isPlaying = false;

            var fi = new FileInfo(filePath);
            lblFilePath.Text = $"ファイル: {fi.FullName}";
            lblFileSize.Text = $"サイズ: {FormatFileSize(fi.Length)}";

            ResetCutDisplay();
            SetStatus("状態: 動画を読み込み中...", Color.FromArgb(60, 60, 60));
            AppendLog($"[読み込み] {fi.FullName} ({FormatFileSize(fi.Length)})");

            if (_webView2Ready)
                LoadVideoInPlayer(filePath);
            else
                AppendLog("[警告] WebView2 未初期化のためプレビューをスキップします。");
        }

        private void OnVideoLoaded()
        {
            trkSeek.Maximum = Math.Max(1, (int)_duration);
            trkSeek.Value = 0;
            trkSeek.Enabled = true;

            lblTotalTime.Text = SecondsToHms(_duration);
            lblCurrentTime.Text = "00:00:00";

            btnPlayPause.Enabled = true;
            btnBack5.Enabled = true;
            btnForward5.Enabled = true;
            btnSetStart.Enabled = true;
            btnSetEnd.Enabled = true;
            txtStartTime.Enabled = true;
            txtEndTime.Enabled = true;

            UpdateConvertButton();
            SetStatus("状態: 待機中 — 開始・終了位置を設定して「変換実行」を押してください",
                Color.FromArgb(60, 60, 60));
        }

        // ─── 再生コントロール ─────────────────────────────────────────
        private void BtnPlayPause_Click(object? sender, EventArgs e)
        {
            if (!_videoLoaded) return;
            if (_isPlaying)
                SendToPlayer("{\"cmd\":\"pause\"}");
            else
                SendToPlayer("{\"cmd\":\"play\"}");
        }

        private void SeekRelative(double delta)
        {
            if (!_videoLoaded) return;
            double newTime = Math.Max(0, Math.Min(_currentTime + delta, _duration));
            SendToPlayer($"{{\"cmd\":\"seek\",\"t\":{newTime:F3}}}");
        }

        private void TrkSeek_Scroll(object? sender, EventArgs e)
        {
            if (!_videoLoaded || _isSeekBarUpdating) return;
            double seekTo = trkSeek.Value;
            SendToPlayer($"{{\"cmd\":\"seek\",\"t\":{seekTo:F3}}}");
        }

        private void UpdateTimeDisplay()
        {
            lblCurrentTime.Text = SecondsToHms(_currentTime);
            if (_duration > 0)
                lblTotalTime.Text = SecondsToHms(_duration);

            // フィードバックループ防止フラグを立ててからシークバーを更新
            _isSeekBarUpdating = true;
            if (_duration > 0)
            {
                int newVal = (int)Math.Min(_currentTime, _duration);
                trkSeek.Value = Math.Max(trkSeek.Minimum,
                    Math.Min(newVal, trkSeek.Maximum));
            }
            _isSeekBarUpdating = false;
        }

        private void UpdatePlayPauseButton()
        {
            btnPlayPause.Text = _isPlaying ? "⏸  一時停止" : "▶  再生";
        }

        // ─── カット位置 ──────────────────────────────────────────────
        private void BtnSetStart_Click(object? sender, EventArgs e)
        {
            _startSeconds = _currentTime;
            txtStartTime.Text = SecondsToHms(_currentTime);
            UpdateRangeLabel();
            UpdateConvertButton();
        }

        private void BtnSetEnd_Click(object? sender, EventArgs e)
        {
            _endSeconds = _currentTime;
            txtEndTime.Text = SecondsToHms(_currentTime);
            UpdateRangeLabel();
            UpdateConvertButton();
        }

        private void TxtStartTime_Leave(object? sender, EventArgs e)
        {
            if (TryParseHms(txtStartTime.Text, out double sec))
            {
                _startSeconds = sec;
                txtStartTime.Text = SecondsToHms(sec);
                UpdateRangeLabel();
                UpdateConvertButton();
            }
            else
            {
                txtStartTime.Text = _startSeconds.HasValue
                    ? SecondsToHms(_startSeconds.Value) : "--:--:--";
            }
        }

        private void TxtEndTime_Leave(object? sender, EventArgs e)
        {
            if (TryParseHms(txtEndTime.Text, out double sec))
            {
                _endSeconds = sec;
                txtEndTime.Text = SecondsToHms(sec);
                UpdateRangeLabel();
                UpdateConvertButton();
            }
            else
            {
                txtEndTime.Text = _endSeconds.HasValue
                    ? SecondsToHms(_endSeconds.Value) : "--:--:--";
            }
        }

        private void UpdateRangeLabel()
        {
            if (_startSeconds.HasValue && _endSeconds.HasValue)
            {
                double range = _endSeconds.Value - _startSeconds.Value;
                if (range > 0)
                    lblRange.Text = $"指定範囲: {SecondsToHms(range)}";
                else
                    lblRange.Text = "指定範囲: (終了位置が開始以前です)";
            }
            else
            {
                lblRange.Text = "指定範囲: --";
            }
        }

        private void ResetCutDisplay()
        {
            txtStartTime.Text = "--:--:--";
            txtEndTime.Text = "--:--:--";
            lblRange.Text = "指定範囲: --";
            UpdateConvertButton();
        }

        // ─── 変換実行 ────────────────────────────────────────────────
        private void UpdateConvertButton()
        {
            bool canConvert =
                !string.IsNullOrEmpty(_inputFile) &&
                File.Exists(_inputFile) &&
                _startSeconds.HasValue &&
                _endSeconds.HasValue &&
                _endSeconds.Value > _startSeconds.Value &&
                _ffmpeg.IsAvailable;

            btnConvert.Enabled = canConvert;
        }

        private async void BtnConvert_Click(object? sender, EventArgs e)
        {
            if (!ValidateBeforeConvert()) return;

            string outputFile = _ffmpeg.BuildOutputPath(_inputFile!);

            if (File.Exists(outputFile))
            {
                ShowUserError("出力ファイルが既に存在します。",
                    $"ファイル名: {Path.GetFileName(outputFile)}\n\n" +
                    "しばらく時間をおいてから再実行してください。");
                return;
            }

            var settings = new ConversionSettings
            {
                InputFile = _inputFile!,
                StartTime = TimeSpan.FromSeconds(_startSeconds!.Value),
                EndTime = TimeSpan.FromSeconds(_endSeconds!.Value),
                Quality = (QualityPreset)cmbQuality.SelectedIndex,
                Resolution = (ResolutionPreset)cmbResolution.SelectedIndex
            };

            SetConvertingState(true);
            txtLog.Clear();
            AppendLog($"[変換開始] {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            AppendLog($"  入力: {settings.InputFile}");
            AppendLog($"  範囲: {SecondsToHms(_startSeconds!.Value)} 〜 {SecondsToHms(_endSeconds!.Value)}");
            AppendLog($"  品質: {cmbQuality.Text} / 解像度: {cmbResolution.Text}");
            AppendLog($"  出力: {outputFile}");
            SetStatus("状態: 変換中...", Color.FromArgb(0, 120, 200));

            _cancelSource = new CancellationTokenSource();
            var ct = _cancelSource.Token;

            try
            {
                await _ffmpeg.RunAsync(
                    settings,
                    outputFile,
                    ct,
                    log => AppendLogSafe(log),
                    (success, exitCode) => OnConversionCompleted(success, exitCode, outputFile));
            }
            catch (FileNotFoundException ex)
            {
                AppendLog($"[エラー] {ex.Message}");
                SetStatus("状態: エラーが発生しました", Color.OrangeRed);
                ShowUserError("変換を開始できませんでした。", ex.Message);
                SetConvertingState(false);
            }
            catch (Exception ex)
            {
                AppendLog($"[予期しないエラー] {ex.Message}");
                SetStatus("状態: エラー", Color.OrangeRed);
                SetConvertingState(false);
            }
        }

        private bool ValidateBeforeConvert()
        {
            if (string.IsNullOrEmpty(_inputFile) || !File.Exists(_inputFile))
            {
                ShowUserError("入力ファイルが見つかりません。",
                    "ファイルを再度選択してください。");
                return false;
            }
            if (!_startSeconds.HasValue || !_endSeconds.HasValue)
            {
                ShowUserError("開始位置と終了位置を設定してください。",
                    "「現在位置を開始に設定」「現在位置を終了に設定」ボタンを押して\n" +
                    "変換したい範囲を指定してから実行してください。");
                return false;
            }
            if (_endSeconds.Value <= _startSeconds.Value)
            {
                ShowUserError("終了位置が開始位置以前です。",
                    "終了位置は開始位置より後に設定してください。");
                return false;
            }
            if (!_ffmpeg.IsAvailable)
            {
                ShowUserError("ffmpeg.exe が見つかりません。",
                    $"配置場所: {_ffmpeg.FfmpegPath}\n\n" +
                    "docs/ffmpeg_setup.md を参照して配置してください。");
                return false;
            }
            return true;
        }

        private void OnConversionCompleted(bool success, int? exitCode, string outputFile)
        {
            // このコールバックはバックグラウンドスレッドから呼ばれる可能性がある
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConversionCompleted(success, exitCode, outputFile)));
                return;
            }

            _cancelSource?.Dispose();
            _cancelSource = null;
            SetConvertingState(false);

            if (exitCode == null)
            {
                // キャンセル
                AppendLog("[キャンセル] 変換をキャンセルしました。");
                SetStatus("状態: キャンセルされました", Color.FromArgb(100, 100, 100));

                // 不完全な出力ファイルを削除
                try
                {
                    if (File.Exists(outputFile)) File.Delete(outputFile);
                }
                catch { /* 削除失敗は無視 */ }
                return;
            }

            if (success && File.Exists(outputFile))
            {
                var fi = new FileInfo(outputFile);
                AppendLog($"[完了] 変換成功 — {DateTime.Now:HH:mm:ss}");
                AppendLog($"  出力ファイル: {fi.FullName}");
                AppendLog($"  出力サイズ: {FormatFileSize(fi.Length)}");
                SetStatus($"状態: 変換完了  出力サイズ: {FormatFileSize(fi.Length)}",
                    Color.FromArgb(0, 120, 60));

                MessageBox.Show(
                    $"変換が完了しました。\n\n" +
                    $"出力ファイル:\n{fi.FullName}\n\n" +
                    $"サイズ: {FormatFileSize(fi.Length)}",
                    "変換完了",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                AppendLog($"[失敗] 変換に失敗しました（終了コード: {exitCode}）。");
                AppendLog("  上記のログを確認してください。");
                SetStatus("状態: 変換に失敗しました", Color.OrangeRed);

                // 不完全な出力ファイルを削除
                try
                {
                    if (File.Exists(outputFile)) File.Delete(outputFile);
                }
                catch { /* 削除失敗は無視 */ }

                ShowUserError(
                    "変換に失敗しました。",
                    "考えられる原因:\n" +
                    "  ・ 入力ファイルが破損している\n" +
                    "  ・ 出力先に書き込み権限がない\n" +
                    "  ・ ディスク容量が不足している\n\n" +
                    "下部のログ欄に詳細が表示されています。");
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            _cancelSource?.Cancel();
            btnCancel.Enabled = false;
            SetStatus("状態: キャンセル中...", Color.FromArgb(100, 100, 100));
        }

        private void SetConvertingState(bool converting)
        {
            btnConvert.Enabled = !converting && ValidateCanConvertSilent();
            btnCancel.Enabled = converting;
            cmbQuality.Enabled = !converting;
            cmbResolution.Enabled = !converting;
            btnBrowse.Enabled = !converting;
            btnSetStart.Enabled = !converting && _videoLoaded;
            btnSetEnd.Enabled = !converting && _videoLoaded;
            txtStartTime.Enabled = !converting && _videoLoaded;
            txtEndTime.Enabled = !converting && _videoLoaded;
        }

        private bool ValidateCanConvertSilent()
        {
            return
                !string.IsNullOrEmpty(_inputFile) &&
                File.Exists(_inputFile) &&
                _startSeconds.HasValue &&
                _endSeconds.HasValue &&
                _endSeconds.Value > _startSeconds.Value &&
                _ffmpeg.IsAvailable;
        }

        // ─── ログ・ステータス ─────────────────────────────────────────
        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendLog(message)));
                return;
            }
            txtLog.AppendText(message + Environment.NewLine);
        }

        private void AppendLogSafe(string message)
        {
            // バックグラウンドスレッドから呼ばれることを想定
            if (InvokeRequired)
                Invoke(new Action(() => AppendLog(message)));
            else
                AppendLog(message);
        }

        private void SetStatus(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(text, color)));
                return;
            }
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        // ─── ユーティリティ ───────────────────────────────────────────
        private static string SecondsToHms(double totalSeconds)
        {
            if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds))
                return "00:00:00";
            int h = (int)(totalSeconds / 3600);
            int m = (int)((totalSeconds % 3600) / 60);
            int s = (int)(totalSeconds % 60);
            return $"{h:D2}:{m:D2}:{s:D2}";
        }

        private static bool TryParseHms(string text, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();

            // HH:mm:ss
            if (TimeSpan.TryParseExact(text, @"hh\:mm\:ss", null, out var ts1))
            {
                seconds = ts1.TotalSeconds;
                return true;
            }
            // H:mm:ss
            if (TimeSpan.TryParseExact(text, @"h\:mm\:ss", null, out var ts2))
            {
                seconds = ts2.TotalSeconds;
                return true;
            }
            // mm:ss
            if (TimeSpan.TryParseExact(text, @"mm\:ss", null, out var ts3))
            {
                seconds = ts3.TotalSeconds;
                return true;
            }
            // 秒数のみ
            if (double.TryParse(text, out double secs) && secs >= 0)
            {
                seconds = secs;
                return true;
            }
            return false;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1_024)
                return $"{bytes / 1_024.0:F1} KB";
            return $"{bytes} B";
        }

        private static void ShowUserError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ─── フォームクローズ ──────────────────────────────────────────
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cancelSource?.Cancel();
            base.OnFormClosing(e);
        }
    }
}
