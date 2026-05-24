using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MovieConverter
{
    public class MainForm : Form
    {
        // ─── コントロール ───────────────────────────────────────────
        private Panel pnlFile = null!;
        private Button btnBrowse = null!;
        private Label lblFilePath = null!;
        private Label lblFileSize = null!;
        private Label lblVideoInfo = null!;

        // 動画プレビュープレイヤー（WebView2 実装または将来の代替実装）
        private IVideoPlayer _player = null!;

        private Panel pnlSeek = null!;
        private Label lblCurrentTime = null!;
        private TrackBar trkSeek = null!;
        private Label lblTotalTime = null!;

        private Panel pnlPlayback = null!;
        private Button btnBack5 = null!;
        private Button btnPlayPause = null!;
        private Button btnForward5 = null!;
        private TrackBar trkVolume = null!;
        private Button btnMute = null!;

        private Panel pnlCut = null!;
        private Button btnSetStart = null!;
        private TextBox txtStartTime = null!;
        private Label lblStartLabel = null!;
        private Button btnSetEnd = null!;
        private TextBox txtEndTime = null!;
        private Label lblEndLabel = null!;
        private Label lblRange = null!;
        private Label lblModeHint = null!;

        private Panel pnlSettings = null!;
        private Label lblQualityLabel = null!;
        private ComboBox cmbQuality = null!;
        private Label lblResolutionLabel = null!;
        private ComboBox cmbResolution = null!;

        private Panel pnlConvert = null!;
        private ProgressBar pbProgress = null!;
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
        private bool _videoLoaded;
        private CancellationTokenSource? _cancelSource;
        private readonly FfmpegRunner _ffmpeg = new();
        private readonly FfprobeRunner _ffprobe = new();
        private System.Windows.Forms.Timer? _elapsedTimer;
        private DateTime _conversionStart;
        private double _lastProgressRatio;
        private string _activeOperationLabel = "変換中";
        private bool _showPreconvertDialog = true;
        private readonly StringBuilder _ffmpegOutputBuffer = new();
        private readonly object _bufferLock = new();

        // ─── コンストラクタ ──────────────────────────────────────────
        public MainForm()
        {
            _player = new WebView2VideoPlayer();
            InitializeComponent();
            SubscribePlayerEvents();
            SetupDragDrop();

            string appDir = Path.GetDirectoryName(Environment.ProcessPath)
                ?? AppContext.BaseDirectory;
            _ = _player.InitializeAsync(appDir);
            CheckFfmpegAvailability();
        }

        // ─── プレイヤーイベント購読 ───────────────────────────────────
        private void SubscribePlayerEvents()
        {
            _player.VideoLoaded += duration =>
            {
                _duration = duration;
                _videoLoaded = true;
                OnVideoLoaded();
            };

            _player.TimeUpdated += (currentTime, duration) =>
            {
                _currentTime = currentTime;
                if (duration > 0) _duration = duration;
                UpdateTimeDisplay();
            };

            _player.PlaybackStarted += () =>
            {
                _isPlaying = true;
                UpdatePlayPauseButton();
            };

            _player.PlaybackPaused += () =>
            {
                _isPlaying = false;
                UpdatePlayPauseButton();
            };

            _player.PlaybackEnded += () =>
            {
                _isPlaying = false;
                UpdatePlayPauseButton();
            };

            _player.PlaybackBlocked += () =>
            {
                _isPlaying = false;
                UpdatePlayPauseButton();
                SetStatus("状態: プレイヤー内の ▶ ボタンを押して再生してください",
                    Color.FromArgb(60, 60, 60));
            };

            _player.VideoError += OnVideoPlayerError;
            _player.FileDropped += LoadFile;
            _player.LogMessage += AppendLog;
        }

        // ─── UI 初期化 ───────────────────────────────────────────────
        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "動画簡易変換ツール  v0.2.1";
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
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));    // 0: file
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // 1: preview
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));    // 2: seek bar
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));    // 3: playback buttons
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));   // 4: cut position
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));    // 5: settings
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));    // 6: convert + progress bar
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

            lblVideoInfo = new Label
            {
                Text = "",
                Location = new Point(4, 60),
                Size = new Size(760, 22),
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("Meiryo UI", 8.5f),
                AutoEllipsis = true
            };

            pnlFile.Controls.AddRange(new Control[] { btnBrowse, lblFilePath, lblFileSize, lblVideoInfo });
            tableLayout.Controls.Add(pnlFile, 0, 0);

            // ── Row 1: 動画プレビュー（IVideoPlayer が提供するコントロール） ──
            tableLayout.Controls.Add(_player.PreviewControl, 0, 1);

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

            var lblVolume = new Label
            {
                Text = "音量:",
                Location = new Point(618, 12),
                Size = new Size(40, 22),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            trkVolume = new TrackBar
            {
                Location = new Point(660, 4),
                Size = new Size(100, 30),
                Minimum = 0,
                Maximum = 100,
                Value = 80,
                TickStyle = TickStyle.None,
                Enabled = false
            };
            trkVolume.Scroll += TrkVolume_Scroll;

            btnMute = new Button
            {
                Text = "消音",
                Location = new Point(764, 8),
                Size = new Size(44, 28),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnMute.Click += BtnMute_Click;

            pnlPlayback.Controls.AddRange(new Control[] { btnBack5, btnPlayPause, btnForward5, lblVolume, trkVolume, btnMute });
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
                Size = new Size(260, 28),
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
            cmbQuality.Items.AddRange(new object[] { "しない（高速カット）", "画質優先", "標準", "容量優先" });
            cmbQuality.SelectedIndex = 0; // 高速カット
            cmbQuality.SelectedIndexChanged += CmbQuality_SelectedIndexChanged;

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
            cmbResolution.Enabled = false; // 初期値が高速カットのため無効

            var lblOutputNote = new Label
            {
                Text = "出力先: 元ファイルと同じフォルダ",
                Location = new Point(440, 18),
                Size = new Size(290, 24),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblModeHint = new Label
            {
                Text = "高速カットは再圧縮しないため高速ですが、開始位置が少しずれる場合があります。",
                Location = new Point(4, 48),
                Size = new Size(740, 18),
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Meiryo UI", 8.5f)
            };

            pnlSettings.Controls.AddRange(new Control[]
            {
                lblQualityLabel, cmbQuality,
                lblResolutionLabel, cmbResolution,
                lblOutputNote, lblModeHint
            });
            tableLayout.Controls.Add(pnlSettings, 0, 5);

            // ── Row 6: 進捗バー + 変換実行 ──
            pnlConvert = CreateSectionPanel();
            pnlConvert.Padding = new Padding(4, 4, 4, 4);

            pbProgress = new ProgressBar
            {
                Location = new Point(4, 4),
                Size = new Size(750, 14),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false
            };

            btnConvert = new Button
            {
                Text = "変換実行",
                Location = new Point(4, 26),
                Size = new Size(110, 32),
                UseVisualStyleBackColor = true,
                Font = new Font("Meiryo UI", 9f, FontStyle.Bold),
                Enabled = false
            };
            btnConvert.Click += BtnConvert_Click;

            btnCancel = new Button
            {
                Text = "キャンセル",
                Location = new Point(124, 26),
                Size = new Size(90, 32),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnCancel.Click += BtnCancel_Click;

            lblStatus = new Label
            {
                Text = "状態: 待機中",
                Location = new Point(228, 31),
                Size = new Size(500, 22),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            pnlConvert.Controls.AddRange(new Control[] { pbProgress, btnConvert, btnCancel, lblStatus });
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
            void OnDragEnter(object? s, DragEventArgs e)
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            }
            void OnDragDrop(object? s, DragEventArgs e)
            {
                if (e.Data == null) return;
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || files.Length == 0) return;
                LoadFile(files[0]);
            }

            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;

            // プレビュー領域へのドロップ（WebView2 初期化前に lblPreviewHint が見える期間含む）
            _player.PreviewControl.AllowDrop = true;
            _player.PreviewControl.DragEnter += OnDragEnter;
            _player.PreviewControl.DragDrop += OnDragDrop;
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

        private void LoadFile(string filePath, bool allowPreconvertDialog = true)
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

            _showPreconvertDialog = allowPreconvertDialog;
            _inputFile = filePath;
            _videoLoaded = false;
            _startSeconds = null;
            _endSeconds = null;
            _currentTime = 0;
            _duration = 0;
            _isPlaying = false;
            UpdatePlayPauseButton();

            var fi = new FileInfo(filePath);
            lblFilePath.Text = $"ファイル: {fi.FullName}";
            lblFileSize.Text = $"サイズ: {FormatFileSize(fi.Length)}";
            lblVideoInfo.Text = _ffprobe.IsAvailable ? "動画情報を取得中..." : "";
            lblVideoInfo.ForeColor = Color.FromArgb(140, 140, 140);
            _ = LoadVideoInfoAsync(filePath);

            ResetCutDisplay();
            SetStatus("状態: 動画を読み込み中...", Color.FromArgb(60, 60, 60));
            AppendLog($"[読み込み] {fi.FullName} ({FormatFileSize(fi.Length)})");

            _player.LoadVideo(filePath);
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
            trkVolume.Enabled = true;
            btnMute.Enabled = true;

            // 開始・終了位置を動画全体に初期設定する（変更なければ全体変換、変更すれば範囲変換）
            _startSeconds = 0;
            _endSeconds = _duration;
            txtStartTime.Text = SecondsToHms(0);
            txtEndTime.Text = SecondsToHms(_duration);
            UpdateRangeLabel();

            btnSetStart.Enabled = true;
            btnSetEnd.Enabled = true;
            txtStartTime.Enabled = true;
            txtEndTime.Enabled = true;

            AppendLog($"[読み込み完了] 動画時間: {SecondsToHms(_duration)}");
            UpdateConvertButton();
            SetStatus("状態: 待機中 — 開始・終了位置を調整して「変換実行」を押してください",
                Color.FromArgb(60, 60, 60));
        }

        private void OnVideoPlayerError(string errorMessage)
        {
            // WebView2VideoPlayer 内でファイル-uri → virtual-host フォールバックは完結済み。
            // このハンドラは「両方式とも失敗した」場合にのみ呼ばれる。
            _videoLoaded = false;
            btnPlayPause.Enabled = false;
            btnBack5.Enabled = false;
            btnForward5.Enabled = false;
            trkVolume.Enabled = false;
            btnMute.Enabled = false;
            btnSetStart.Enabled = false;
            btnSetEnd.Enabled = false;
            txtStartTime.Enabled = false;
            txtEndTime.Enabled = false;
            trkSeek.Enabled = false;
            UpdateConvertButton();

            // 事前変換ダイアログを案内する（ユーザー選択ファイルのみ、事前変換後ファイルは再案内しない）
            if (_showPreconvertDialog && !string.IsNullOrEmpty(_inputFile))
            {
                SetStatus("状態: この動画はそのままでは再生できません", Color.FromArgb(180, 100, 0));
                AppendLog("[プレビュー] この動画はそのままでは再生できません。");
                if (ShowPreconvertConsentDialog())
                {
                    _ = RunPreconvertAsync(_inputFile);
                    return;
                }
            }

            // ダイアログでキャンセルされた場合、または再案内不要の場合
            SetStatus("状態: 動画の読み込みに失敗しました", Color.OrangeRed);
            AppendLog("確認方法:");
            AppendLog("  1. Microsoft EdgeでこのMP4を直接開いて再生できるか確認してください。");
            AppendLog("  2. 長時間動画の場合、30秒程度のサンプルで確認してください。");
            AppendLog("  3. 解決しない場合は管理者またはDX担当に相談してください。");
        }

        private bool ShowPreconvertConsentDialog()
        {
            using var dlg = new Form
            {
                Text = "事前変換の確認",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(430, 196),
                Font = new Font("Meiryo UI", 9f),
                BackColor = Color.White
            };

            var lbl = new Label
            {
                Text = "この動画は、そのままではこの画面で再生できません。\r\n\r\n" +
                       "動画のカット位置を確認するため、事前変換が必要です。\r\n" +
                       "元の動画は変更せず、変換後の動画を別ファイルとして作成します。\r\n\r\n" +
                       "事前変換を実行しますか？",
                Location = new Point(16, 16),
                Size = new Size(398, 108),
                AutoSize = false
            };

            var btnOk = new Button
            {
                Text = "実行する",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 148),
                Size = new Size(90, 30),
                UseVisualStyleBackColor = true
            };

            var btnCancel = new Button
            {
                Text = "キャンセル",
                DialogResult = DialogResult.Cancel,
                Location = new Point(320, 148),
                Size = new Size(94, 30),
                UseVisualStyleBackColor = true
            };

            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            dlg.Controls.AddRange(new Control[] { lbl, btnOk, btnCancel });

            return dlg.ShowDialog(this) == DialogResult.OK;
        }

        private async Task RunPreconvertAsync(string inputFile)
        {
            string outputFile = _ffmpeg.BuildPreconvertedOutputPath(inputFile);

            if (File.Exists(outputFile))
            {
                ShowUserError("出力ファイルが既に存在します。",
                    "しばらく時間をおいてから再実行してください。");
                SetStatus("状態: 動画の読み込みに失敗しました", Color.OrangeRed);
                return;
            }

            _activeOperationLabel = "事前変換中";
            _lastProgressRatio = 0;
            SetConvertingState(true);
            lock (_bufferLock) _ffmpegOutputBuffer.Clear();

            AppendLog($"[事前変換開始] {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            AppendLog($"  入力: {inputFile}");
            AppendLog($"  出力: {outputFile}");
            AppendLog("事前変換中です。大きい動画では時間がかかる場合があります。");

            _conversionStart = DateTime.Now;
            _elapsedTimer?.Dispose();
            _elapsedTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _elapsedTimer.Tick += ElapsedTimer_Tick;
            _elapsedTimer.Start();

            pbProgress.Style = ProgressBarStyle.Marquee;
            pbProgress.Value = 0;
            pbProgress.Visible = true;
            SetStatus($"状態: {_activeOperationLabel}... 経過時間 00:00:00", Color.FromArgb(0, 120, 200));

            _cancelSource = new CancellationTokenSource();
            var ct = _cancelSource.Token;

            try
            {
                await _ffmpeg.RunFaststartAsync(
                    inputFile,
                    outputFile,
                    _duration,
                    ct,
                    ConversionLogCallback,
                    OnConversionProgress,
                    (success, exitCode) => OnPreconvertCompleted(success, exitCode, outputFile));
            }
            catch (FileNotFoundException ex)
            {
                StopConversionTimer();
                _cancelSource?.Dispose();
                _cancelSource = null;
                _activeOperationLabel = "変換中";
                AppendLog($"[エラー] {ex.Message}");
                SetStatus("状態: エラーが発生しました", Color.OrangeRed);
                ShowUserError("事前変換を開始できませんでした。", ex.Message);
                SetConvertingState(false);
            }
            catch (Exception ex)
            {
                StopConversionTimer();
                _cancelSource?.Dispose();
                _cancelSource = null;
                _activeOperationLabel = "変換中";
                AppendLog($"[予期しないエラー] {ex.Message}");
                SetStatus("状態: エラー", Color.OrangeRed);
                SetConvertingState(false);
            }
        }

        private void OnPreconvertCompleted(bool success, int? exitCode, string outputFile)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnPreconvertCompleted(success, exitCode, outputFile)));
                return;
            }

            StopConversionTimer();
            _cancelSource?.Dispose();
            _cancelSource = null;
            _activeOperationLabel = "変換中";
            SetConvertingState(false);

            if (exitCode == null)
            {
                AppendLog("[キャンセル] 事前変換をキャンセルしました。");
                SetStatus("状態: キャンセルされました", Color.FromArgb(100, 100, 100));
                try { if (File.Exists(outputFile)) File.Delete(outputFile); } catch { }
                return;
            }

            if (success && File.Exists(outputFile))
            {
                var elapsed = DateTime.Now - _conversionStart;
                string elapsedStr = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                AppendLog($"事前変換が完了しました。変換後の動画を読み込みます。");
                AppendLog($"  所要時間: {elapsedStr}");
                SetStatus("状態: 事前変換完了 — 変換後の動画を読み込み中...", Color.FromArgb(0, 120, 60));
                LoadFile(outputFile, allowPreconvertDialog: false);
            }
            else
            {
                AppendLog($"[失敗] 事前変換に失敗しました（終了コード: {exitCode}）。");
                string buf;
                lock (_bufferLock) buf = _ffmpegOutputBuffer.ToString();
                if (!string.IsNullOrWhiteSpace(buf))
                {
                    AppendLog("[詳細ログ]");
                    foreach (string l in buf.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        AppendLog("  " + l.TrimEnd('\r'));
                }
                SetStatus("状態: 事前変換に失敗しました", Color.OrangeRed);
                try { if (File.Exists(outputFile)) File.Delete(outputFile); } catch { }
                ShowUserError(
                    "事前変換に失敗しました。",
                    "動画ファイルまたは保存先を確認してください。\n\n" +
                    "考えられる原因:\n" +
                    "  ・ 動画ファイルが破損している\n" +
                    "  ・ 出力先に書き込み権限がない\n" +
                    "  ・ ディスク容量が不足している\n\n" +
                    "下部のログ欄に詳細が表示されています。");
            }
        }

        // ─── 再生コントロール ─────────────────────────────────────────
        private void BtnPlayPause_Click(object? sender, EventArgs e)
        {
            if (!_videoLoaded) return;
            if (_isPlaying) _player.Pause();
            else            _player.Play();
        }

        private void SeekRelative(double delta)
        {
            if (!_videoLoaded) return;
            double newTime = Math.Max(0, Math.Min(_currentTime + delta, _duration));
            _player.Seek(newTime);
        }

        private void TrkSeek_Scroll(object? sender, EventArgs e)
        {
            if (!_videoLoaded || _isSeekBarUpdating) return;
            _player.Seek(trkSeek.Value);
        }

        private void UpdateTimeDisplay()
        {
            lblCurrentTime.Text = SecondsToHms(_currentTime);
            if (_duration > 0)
                lblTotalTime.Text = SecondsToHms(_duration);

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
            if (!_ffmpeg.IsAvailable ||
                string.IsNullOrEmpty(_inputFile) ||
                !File.Exists(_inputFile))
            {
                btnConvert.Enabled = false;
                return;
            }
            btnConvert.Enabled = _startSeconds.HasValue &&
                                 _endSeconds.HasValue &&
                                 _endSeconds.Value > _startSeconds.Value;
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

            // 開始≈0 かつ 終了≈動画時間 なら全体変換モード（-ss/-to を付けない）
            bool isFullVideo = _startSeconds.HasValue && _endSeconds.HasValue &&
                               _startSeconds.Value < 0.5 &&
                               Math.Abs(_endSeconds.Value - _duration) < 0.5;
            var settings = new ConversionSettings
            {
                InputFile = _inputFile!,
                StartTime = _startSeconds.HasValue ? TimeSpan.FromSeconds(_startSeconds.Value) : TimeSpan.Zero,
                EndTime = _endSeconds.HasValue ? TimeSpan.FromSeconds(_endSeconds.Value) : TimeSpan.Zero,
                Quality = (QualityPreset)cmbQuality.SelectedIndex,
                Resolution = (ResolutionPreset)cmbResolution.SelectedIndex,
                Mode = isFullVideo ? ConversionMode.FullVideo : ConversionMode.RangeOnly
                // Speed: SpeedPreset.Default（将来 UI から選択できるようにする）
            };

            _activeOperationLabel = "変換中";
            _lastProgressRatio = 0;
            SetConvertingState(true);
            txtLog.Clear();
            lock (_bufferLock) _ffmpegOutputBuffer.Clear();

            AppendLog($"[変換開始] {DateTime.Now:yyyy/MM/dd HH:mm:ss}");
            AppendLog($"  入力: {settings.InputFile}");
            AppendLog($"  変換範囲: {(isFullVideo ? "動画全体" : $"{SecondsToHms(_startSeconds!.Value)} 〜 {SecondsToHms(_endSeconds!.Value)}")}");
            if (settings.Quality == QualityPreset.FastCut)
                AppendLog("  出力方式: 高速カット（再エンコードなし）");
            else
            {
                AppendLog("  出力方式: 圧縮変換（再エンコードあり）");
                AppendLog($"  品質: {cmbQuality.Text} / 解像度: {cmbResolution.Text}");
            }
            AppendLog($"  出力: {outputFile}");
            AppendLog("変換中です。しばらくお待ちください。");
            if (settings.Quality != QualityPreset.FastCut)
                AppendLog("長時間動画や圧縮変換では時間がかかります。");

            _conversionStart = DateTime.Now;
            _elapsedTimer?.Dispose();
            _elapsedTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _elapsedTimer.Tick += ElapsedTimer_Tick;
            _elapsedTimer.Start();

            pbProgress.Style = ProgressBarStyle.Marquee;
            pbProgress.Value = 0;
            pbProgress.Visible = true;
            SetStatus($"状態: {_activeOperationLabel}... 経過時間 00:00:00", Color.FromArgb(0, 120, 200));

            double totalDuration = isFullVideo
                ? _duration
                : (_endSeconds!.Value - _startSeconds!.Value);

            _cancelSource = new CancellationTokenSource();
            var ct = _cancelSource.Token;

            try
            {
                await _ffmpeg.RunAsync(
                    settings,
                    outputFile,
                    totalDuration,
                    ct,
                    ConversionLogCallback,
                    OnConversionProgress,
                    (success, exitCode) => OnConversionCompleted(success, exitCode, outputFile));
            }
            catch (FileNotFoundException ex)
            {
                StopConversionTimer();
                _cancelSource?.Dispose();
                _cancelSource = null;
                AppendLog($"[エラー] {ex.Message}");
                SetStatus("状態: エラーが発生しました", Color.OrangeRed);
                ShowUserError("変換を開始できませんでした。", ex.Message);
                SetConvertingState(false);
            }
            catch (Exception ex)
            {
                StopConversionTimer();
                _cancelSource?.Dispose();
                _cancelSource = null;
                AppendLog($"[予期しないエラー] {ex.Message}");
                SetStatus("状態: エラー", Color.OrangeRed);
                SetConvertingState(false);
            }
        }

        private void ConversionLogCallback(string line)
        {
            // 先頭が "[" の行は自前のタグ付きメッセージ（利用者向け）→ ログ表示
            // それ以外は ffmpeg の生出力 → バッファに蓄積のみ（失敗時にダンプ）
            if (line.StartsWith("[", StringComparison.Ordinal))
                AppendLogSafe(line);
            else
                lock (_bufferLock)
                    _ffmpegOutputBuffer.AppendLine(line);
        }

        private void OnConversionProgress(double ratio)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConversionProgress(ratio)));
                return;
            }
            _lastProgressRatio = ratio;
            int percent = Math.Max(0, Math.Min(100, (int)(ratio * 100)));
            if (pbProgress.Style != ProgressBarStyle.Continuous)
                pbProgress.Style = ProgressBarStyle.Continuous;
            if (percent > pbProgress.Value)
                pbProgress.Value = percent;
        }

        private void ElapsedTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _conversionStart;
            string t = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

            if (pbProgress.Style == ProgressBarStyle.Continuous)
            {
                int percent = pbProgress.Value;
                if (_lastProgressRatio > 0.05)
                {
                    double remainSec = elapsed.TotalSeconds * (1.0 - _lastProgressRatio) / _lastProgressRatio;
                    string rem = $"{(int)(remainSec / 3600):D2}:{(int)((remainSec % 3600) / 60):D2}:{(int)(remainSec % 60):D2}";
                    SetStatus($"状態: {_activeOperationLabel}... {percent}%  残り約 {rem}", Color.FromArgb(0, 120, 200));
                }
                else
                {
                    SetStatus($"状態: {_activeOperationLabel}... {percent}%  残り時間を計算中...", Color.FromArgb(0, 120, 200));
                }
            }
            else
            {
                SetStatus($"状態: {_activeOperationLabel}... 経過時間 {t}", Color.FromArgb(0, 120, 200));
            }
        }

        private void StopConversionTimer()
        {
            _elapsedTimer?.Stop();
            _elapsedTimer?.Dispose();
            _elapsedTimer = null;
            pbProgress.Visible = false;
            pbProgress.Value = 0;
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
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConversionCompleted(success, exitCode, outputFile)));
                return;
            }

            StopConversionTimer();
            _cancelSource?.Dispose();
            _cancelSource = null;
            _activeOperationLabel = "変換中";
            SetConvertingState(false);

            if (exitCode == null)
            {
                AppendLog("[キャンセル] 変換をキャンセルしました。");
                SetStatus("状態: キャンセルされました", Color.FromArgb(100, 100, 100));
                try { if (File.Exists(outputFile)) File.Delete(outputFile); } catch { }
                return;
            }

            if (success && File.Exists(outputFile))
            {
                var fi = new FileInfo(outputFile);
                var elapsed = DateTime.Now - _conversionStart;
                string elapsedStr = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                AppendLog($"[完了] 変換成功 — {DateTime.Now:HH:mm:ss}  所要時間: {elapsedStr}");
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
                string buf;
                lock (_bufferLock) buf = _ffmpegOutputBuffer.ToString();
                if (!string.IsNullOrWhiteSpace(buf))
                {
                    AppendLog("[詳細ログ]");
                    foreach (string l in buf.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        AppendLog("  " + l.TrimEnd('\r'));
                }
                SetStatus("状態: 変換に失敗しました", Color.OrangeRed);
                try { if (File.Exists(outputFile)) File.Delete(outputFile); } catch { }

                ShowUserError(
                    "変換に失敗しました。",
                    "考えられる原因:\n" +
                    "  ・ 入力ファイルが破損している\n" +
                    "  ・ 出力先に書き込み権限がない\n" +
                    "  ・ ディスク容量が不足している\n\n" +
                    "下部のログ欄に詳細が表示されています。");
            }
        }

        private void TrkVolume_Scroll(object? sender, EventArgs e)
        {
            _player.SetVolume(trkVolume.Value / 100.0);
            if (trkVolume.Value > 0)
            {
                btnMute.Text = "消音";
                _player.SetMute(false);
            }
        }

        private void BtnMute_Click(object? sender, EventArgs e)
        {
            bool nowMuted = btnMute.Text == "消音";
            btnMute.Text = nowMuted ? "音有" : "消音";
            _player.SetMute(nowMuted);
        }

        private void CmbQuality_SelectedIndexChanged(object? sender, EventArgs e)
        {
            bool isFastCut = cmbQuality.SelectedIndex == 0;
            cmbResolution.Enabled = !isFastCut;
            lblModeHint.Text = isFastCut
                ? "高速カットは再圧縮しないため高速ですが、開始位置が少しずれる場合があります。"
                : "圧縮して出力する場合は、動画全体を再変換するため時間がかかります。";
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
            cmbResolution.Enabled = !converting && cmbQuality.SelectedIndex != 0;
            btnBrowse.Enabled = !converting;
            bool rangeEnabled = !converting && _videoLoaded;
            btnSetStart.Enabled = rangeEnabled;
            btnSetEnd.Enabled = rangeEnabled;
            txtStartTime.Enabled = rangeEnabled;
            txtEndTime.Enabled = rangeEnabled;
        }

        private bool ValidateCanConvertSilent()
        {
            if (!_ffmpeg.IsAvailable ||
                string.IsNullOrEmpty(_inputFile) ||
                !File.Exists(_inputFile))
                return false;
            return _startSeconds.HasValue &&
                   _endSeconds.HasValue &&
                   _endSeconds.Value > _startSeconds.Value;
        }

        // ─── 動画情報表示 ─────────────────────────────────────────────
        private async Task LoadVideoInfoAsync(string filePath)
        {
            if (!_ffprobe.IsAvailable) return;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                var info = await _ffprobe.GetVideoInfoAsync(filePath, cts.Token)
                    .ConfigureAwait(true);
                if (info == null)
                {
                    SetVideoInfoText("動画情報を取得できませんでした", Color.FromArgb(150, 150, 150));
                    return;
                }
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(info.Duration))   parts.Add($"長さ: {info.Duration}");
                if (!string.IsNullOrEmpty(info.Resolution)) parts.Add($"解像度: {info.Resolution}");
                if (!string.IsNullOrEmpty(info.VideoCodec)) parts.Add($"映像: {info.VideoCodec}");
                if (!string.IsNullOrEmpty(info.AudioCodec)) parts.Add($"音声: {info.AudioCodec}");
                if (!string.IsNullOrEmpty(info.FrameRate))  parts.Add(info.FrameRate);
                if (!string.IsNullOrEmpty(info.FileSize))   parts.Add($"サイズ: {info.FileSize}");
                if (!string.IsNullOrEmpty(info.Bitrate))    parts.Add($"ビットレート: {info.Bitrate}");
                SetVideoInfoText(string.Join("  /  ", parts), Color.FromArgb(80, 80, 80));
            }
            catch
            {
                SetVideoInfoText("動画情報を取得できませんでした", Color.FromArgb(150, 150, 150));
            }
        }

        private void SetVideoInfoText(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetVideoInfoText(text, color)));
                return;
            }
            lblVideoInfo.Text = text;
            lblVideoInfo.ForeColor = color;
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

            if (TimeSpan.TryParseExact(text, @"hh\:mm\:ss", null, out var ts1))
            { seconds = ts1.TotalSeconds; return true; }
            if (TimeSpan.TryParseExact(text, @"h\:mm\:ss", null, out var ts2))
            { seconds = ts2.TotalSeconds; return true; }
            if (TimeSpan.TryParseExact(text, @"mm\:ss", null, out var ts3))
            { seconds = ts3.TotalSeconds; return true; }
            if (double.TryParse(text, out double secs) && secs >= 0)
            { seconds = secs; return true; }
            return false;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)     return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1_024)         return $"{bytes / 1_024.0:F1} KB";
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
