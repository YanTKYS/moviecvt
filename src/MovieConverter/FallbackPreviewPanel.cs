using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MovieConverter
{
    /// <summary>
    /// WebView2 が使えない環境向けの代替プレビュー。
    /// 動画再生は行わず、指定時刻のサムネイル画像を表示してカット位置を確認する。
    /// </summary>
    public sealed class FallbackPreviewPanel : Panel
    {
        // ─── コントロール ────────────────────────────────────────────
        private readonly Label      _descLabel;
        private readonly PictureBox _pic;
        private readonly Panel      _ctrlPanel;

        // ナビゲーション行
        private readonly Button _btnBack10;
        private readonly Button _btnBack5;
        private readonly Button _btnBack1;
        private readonly Label  _lblCurrentTime;
        private readonly Button _btnFwd1;
        private readonly Button _btnFwd5;
        private readonly Button _btnFwd10;

        // 移動行
        private readonly Label   _lblJumpPrefix;
        private readonly TextBox _txtTime;
        private readonly Button  _btnGo;
        private readonly Label   _lblDuration;

        // ステータス行
        private readonly Label _lblStatus;

        // ─── 状態 ────────────────────────────────────────────────────
        private string? _videoFile;
        private double  _duration;
        private double  _currentTime;
        private readonly string _ffmpegPath;
        private readonly string _tempDir;
        private CancellationTokenSource? _thumbCts;
        private bool _generating;
        private bool _converting;

        // ─── イベント ────────────────────────────────────────────────
        /// <summary>ナビゲーション操作で現在位置が変化したとき発火する。引数: 秒。</summary>
        public event Action<double>? TimeChanged;

        public double CurrentTime => _currentTime;

        // ─── コンストラクタ ───────────────────────────────────────────
        public FallbackPreviewPanel(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
            _tempDir    = Path.Combine(Path.GetTempPath(), "MovieConverter_preview");
            BackColor   = Color.FromArgb(30, 30, 30);

            // ── 説明ラベル（上部） ──
            _descLabel = new Label
            {
                Text      = "指定した時刻の画像を表示しています（動画の再生はできません）。" +
                            "画像を確認してから「現在位置を開始に設定」「現在位置を終了に設定」でカット範囲を指定できます。",
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.FromArgb(45, 45, 48),
                Font      = new Font("Meiryo UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 8, 0),
                AutoSize  = false
            };

            // ── サムネイル ──
            _pic = new PictureBox
            {
                SizeMode  = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // ── ナビゲーションパネル（下部） ──
            _ctrlPanel = new Panel { BackColor = Color.FromArgb(45, 45, 48) };

            // ナビゲーション行 (y=6)
            _btnBack10 = NavBtn("◀◀ 10秒",  4,  6, 80);
            _btnBack5  = NavBtn("◀ 5秒",   88,  6, 68);
            _btnBack1  = NavBtn("◀ 1秒",  160,  6, 60);

            _lblCurrentTime = new Label
            {
                Location  = new Point(224, 6),
                Size      = new Size(120, 28),
                Text      = "--:--:--",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 48),
                Font      = new Font("Consolas", 13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false
            };

            _btnFwd1  = NavBtn("1秒 ▶",  348, 6, 60);
            _btnFwd5  = NavBtn("5秒 ▶",  412, 6, 68);
            _btnFwd10 = NavBtn("10秒 ▶▶", 484, 6, 80);

            _btnBack10.Click += (s, e) => Navigate(-10);
            _btnBack5.Click  += (s, e) => Navigate(-5);
            _btnBack1.Click  += (s, e) => Navigate(-1);
            _btnFwd1.Click   += (s, e) => Navigate(1);
            _btnFwd5.Click   += (s, e) => Navigate(5);
            _btnFwd10.Click  += (s, e) => Navigate(10);

            // 移動行 (y=40)
            _lblJumpPrefix = new Label
            {
                Text      = "時刻指定:",
                Location  = new Point(4, 44),
                Size      = new Size(60, 22),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font      = new Font("Meiryo UI", 8.5f),
                TextAlign = ContentAlignment.MiddleRight
            };

            _txtTime = new TextBox
            {
                Location  = new Point(68, 42),
                Size      = new Size(88, 24),
                Text      = "00:00:00",
                TextAlign = HorizontalAlignment.Center,
                Font      = new Font("Consolas", 9f),
                Enabled   = false
            };
            _txtTime.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Return) { e.SuppressKeyPress = true; SeekToTextBox(); }
            };
            _txtTime.TextChanged += (s, e) =>
            {
                _txtTime.BackColor = SystemColors.Window;
                if (_lblStatus.Text?.Contains("形式が正しくありません") == true)
                {
                    _lblStatus.ForeColor = Color.FromArgb(220, 180, 60);
                    _lblStatus.Text = "";
                }
            };

            _btnGo = NavBtn("移動", 160, 40, 54);
            _btnGo.Click += (s, e) => SeekToTextBox();

            _lblDuration = new Label
            {
                Location  = new Point(218, 44),
                Size      = new Size(200, 22),
                Text      = "全体: --:--:--",
                ForeColor = Color.FromArgb(180, 180, 180),
                Font      = new Font("Meiryo UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ステータス行 (y=72)
            _lblStatus = new Label
            {
                Location  = new Point(4, 72),
                Size      = new Size(760, 22),
                Text      = "",
                ForeColor = Color.FromArgb(220, 180, 60),
                Font      = new Font("Meiryo UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _ctrlPanel.Controls.AddRange(new Control[]
            {
                _btnBack10, _btnBack5, _btnBack1,
                _lblCurrentTime,
                _btnFwd1, _btnFwd5, _btnFwd10,
                _lblJumpPrefix, _txtTime, _btnGo, _lblDuration,
                _lblStatus
            });

            Controls.AddRange(new Control[] { _descLabel, _pic, _ctrlPanel });
        }

        // ─── レイアウト ───────────────────────────────────────────────
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            const int descH = 40;
            const int ctrlH = 100;
            int w    = Math.Max(0, Width);
            int h    = Math.Max(0, Height);
            int picH = Math.Max(0, h - descH - ctrlH);
            _descLabel.Bounds = new Rectangle(0, 0,         w, descH);
            _pic.Bounds       = new Rectangle(0, descH,     w, picH);
            _ctrlPanel.Bounds = new Rectangle(0, h - ctrlH, w, ctrlH);
            _lblStatus.Width  = Math.Max(100, w - 8);
            _lblDuration.Width = Math.Max(60, w - 218 - 8);
        }

        // ─── 公開メソッド ─────────────────────────────────────────────
        public void LoadVideo(string filePath, double duration)
        {
            _videoFile         = filePath;
            _duration          = duration;
            _currentTime       = 0;
            _txtTime.Text      = "00:00:00";
            _txtTime.BackColor = SystemColors.Window;
            _lblStatus.Text    = "";
            _lblStatus.ForeColor = Color.FromArgb(220, 180, 60);
            UpdateCurrentTimeLabel();
            UpdateDurationLabel();
            SetButtonsEnabled(true);
            _ = GenerateThumbnailAsync(0);
        }

        public void SetDuration(double duration)
        {
            _duration = duration;
            UpdateDurationLabel();
        }

        public void SetConvertingState(bool converting)
        {
            _converting = converting;
            SetButtonsEnabled(!converting && _videoFile != null && !_generating);
        }

        public void CleanupTempFiles()
        {
            _thumbCts?.Cancel();
            _thumbCts = null;
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch { }
        }

        // ─── ナビゲーション ───────────────────────────────────────────
        private void Navigate(double delta)
        {
            if (_videoFile == null) return;
            double t = _currentTime + delta;
            t = Math.Max(0, t);
            if (_duration > 0) t = Math.Min(t, _duration);
            SeekTo(t);
        }

        private void SeekToTextBox()
        {
            string input = (_txtTime.Text ?? "").Trim();
            if (TryParseTime(input, out double t))
            {
                t = Math.Max(0, t);
                if (_duration > 0) t = Math.Min(t, _duration);
                _txtTime.BackColor   = SystemColors.Window;
                _lblStatus.ForeColor = Color.FromArgb(220, 180, 60);
                _lblStatus.Text      = "";
                SeekTo(t);
            }
            else
            {
                _txtTime.BackColor   = Color.LightPink;
                _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
                _lblStatus.Text      = "⚠ 時刻の形式が正しくありません。例: 00:01:30 または 90（秒数）";
                _ = ClearErrorStatusAfterDelayAsync();
            }
        }

        private async Task ClearErrorStatusAfterDelayAsync()
        {
            await Task.Delay(3000).ConfigureAwait(true);
            if (_lblStatus.Text?.Contains("形式が正しくありません") == true)
            {
                _lblStatus.Text      = "";
                _lblStatus.ForeColor = Color.FromArgb(220, 180, 60);
            }
            if (_txtTime.BackColor == Color.LightPink)
                _txtTime.BackColor = SystemColors.Window;
        }

        private void SeekTo(double seconds)
        {
            _currentTime  = seconds;
            _txtTime.Text = SecondsToHms(seconds);
            UpdateCurrentTimeLabel();
            TimeChanged?.Invoke(seconds);
            _ = GenerateThumbnailAsync(seconds);
        }

        private void UpdateCurrentTimeLabel()
        {
            _lblCurrentTime.Text = _videoFile != null
                ? SecondsToHms(_currentTime)
                : "--:--:--";
        }

        private void UpdateDurationLabel()
        {
            _lblDuration.Text = _duration > 0
                ? $"全体: {SecondsToHms(_duration)}"
                : "全体: --:--:--";
        }

        // ─── サムネイル生成 ───────────────────────────────────────────
        private async Task GenerateThumbnailAsync(double seconds)
        {
            if (_videoFile == null || !File.Exists(_ffmpegPath)) return;

            _thumbCts?.Cancel();
            var cts = new CancellationTokenSource();
            _thumbCts = cts;
            var ct = cts.Token;

            _generating = true;
            SetButtonsEnabled(false);
            _lblStatus.ForeColor = Color.FromArgb(220, 180, 60);
            _lblStatus.Text = "⏳ 画像を更新中...";

            try
            {
                Directory.CreateDirectory(_tempDir);
                string thumbFile = Path.Combine(_tempDir, "thumb.jpg");

                int    hh      = (int)(seconds / 3600);
                int    mm      = (int)((seconds % 3600) / 60);
                double ss      = seconds % 60;
                string timeArg = $"{hh:D2}:{mm:D2}:{ss:06.3f}";
                string args    = $"-ss {timeArg} -i \"{_videoFile}\" " +
                                 $"-frames:v 1 -q:v 2 -y \"{thumbFile}\"";

                using var proc = new Process();
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName               = _ffmpegPath,
                    Arguments              = args,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                proc.OutputDataReceived += (s, e) => { };
                proc.ErrorDataReceived  += (s, e) => { };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                bool exited = await Task.Run(() => proc.WaitForExit(5000)).ConfigureAwait(true);
                if (ct.IsCancellationRequested)
                {
                    if (!exited) { try { proc.Kill(); } catch { } }
                    return;
                }
                if (!exited) { try { proc.Kill(); } catch { } return; }
                if (!File.Exists(thumbFile)) return;

                byte[] data = await Task.Run(() => File.ReadAllBytes(thumbFile), ct)
                    .ConfigureAwait(true);
                if (ct.IsCancellationRequested) return;

                Bitmap bmp;
                using (var ms = new MemoryStream(data))
                using (var tmp = new Bitmap(ms))
                    bmp = new Bitmap(tmp);

                if (ct.IsCancellationRequested) { bmp.Dispose(); return; }

                if (InvokeRequired)
                    Invoke(new Action(() => SetImage(bmp)));
                else
                    SetImage(bmp);
            }
            catch (OperationCanceledException) { }
            catch { }
            finally
            {
                if (!ct.IsCancellationRequested)
                {
                    _generating = false;
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            SetButtonsEnabled(!_converting && _videoFile != null);
                            if (_lblStatus.Text == "⏳ 画像を更新中...")
                                _lblStatus.Text = "";
                        }));
                    }
                    else
                    {
                        SetButtonsEnabled(!_converting && _videoFile != null);
                        if (_lblStatus.Text == "⏳ 画像を更新中...")
                            _lblStatus.Text = "";
                    }
                }
            }
        }

        private void SetImage(Bitmap bmp)
        {
            var old = _pic.Image;
            _pic.Image = bmp;
            old?.Dispose();
        }

        // ─── ユーティリティ ───────────────────────────────────────────
        private void SetButtonsEnabled(bool enabled)
        {
            _btnBack10.Enabled = enabled;
            _btnBack5.Enabled  = enabled;
            _btnBack1.Enabled  = enabled;
            _btnFwd1.Enabled   = enabled;
            _btnFwd5.Enabled   = enabled;
            _btnFwd10.Enabled  = enabled;
            _btnGo.Enabled     = enabled;
            _txtTime.Enabled   = enabled;
        }

        private static Button NavBtn(string text, int x, int y, int w) =>
            new Button
            {
                Text                    = text,
                Location                = new Point(x, y),
                Size                    = new Size(w, 28),
                UseVisualStyleBackColor = true,
                Font                    = new Font("Meiryo UI", 8.5f),
                Enabled                 = false
            };

        private static string SecondsToHms(double s)
        {
            if (double.IsNaN(s) || double.IsInfinity(s)) return "00:00:00";
            int h   = (int)(s / 3600);
            int m   = (int)((s % 3600) / 60);
            int sec = (int)(s % 60);
            return $"{h:D2}:{m:D2}:{sec:D2}";
        }

        private static bool TryParseTime(string text, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            if (TimeSpan.TryParseExact(text, @"hh\:mm\:ss", null, out var ts1))
            { seconds = ts1.TotalSeconds; return true; }
            if (TimeSpan.TryParseExact(text, @"h\:mm\:ss",  null, out var ts2))
            { seconds = ts2.TotalSeconds; return true; }
            if (TimeSpan.TryParseExact(text, @"mm\:ss",     null, out var ts3))
            { seconds = ts3.TotalSeconds; return true; }
            if (TimeSpan.TryParseExact(text, @"m\:ss",      null, out var ts4))
            { seconds = ts4.TotalSeconds; return true; }
            if (double.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double secs) && secs >= 0)
            { seconds = secs; return true; }
            return false;
        }
    }
}
