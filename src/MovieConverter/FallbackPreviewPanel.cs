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
    /// ffmpeg でサムネイル画像を生成し、コマ送りでカット位置を確認できる。
    /// 動画の再生は行わない（サムネイル表示のみ）。
    /// </summary>
    public sealed class FallbackPreviewPanel : Panel
    {
        // ─── コントロール ────────────────────────────────────────────
        private readonly Label _infoLabel;
        private readonly PictureBox _pic;
        private readonly Panel _ctrlPanel;
        private readonly Button _btnBack10;
        private readonly Button _btnBack5;
        private readonly TextBox _txtTime;
        private readonly Button _btnGo;
        private readonly Button _btnFwd5;
        private readonly Button _btnFwd10;
        private readonly Label _lblDuration;
        private readonly Label _lblNote;

        // ─── 状態 ────────────────────────────────────────────────────
        private string? _videoFile;
        private double _duration;
        private double _currentTime;
        private readonly string _ffmpegPath;
        private readonly string _tempDir;
        private CancellationTokenSource? _thumbCts;

        // ─── イベント ────────────────────────────────────────────────
        /// <summary>ナビゲーション操作で現在位置が変化したとき発火する。引数: 秒。</summary>
        public event Action<double>? TimeChanged;

        public double CurrentTime => _currentTime;

        // ─── コンストラクタ ───────────────────────────────────────────
        public FallbackPreviewPanel(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath;
            _tempDir = Path.Combine(Path.GetTempPath(), "MovieConverter_preview");
            BackColor = Color.FromArgb(30, 30, 30);

            // 上部: 情報ラベル（28px）
            _infoLabel = new Label
            {
                Text = "代替プレビュー（WebView2 なし）— MP4ファイルを読み込んでください",
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.FromArgb(45, 45, 48),
                Font = new Font("Meiryo UI", 8.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            // 中央: サムネイル表示
            _pic = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // 下部: ナビゲーションパネル（80px）
            _ctrlPanel = new Panel { BackColor = Color.FromArgb(45, 45, 48) };

            // ナビゲーション行 (Y=4)
            _btnBack10 = NavBtn("◀◀ 10秒", 4,   4, 90);
            _btnBack10.Click += (s, e) => Navigate(-10);
            _btnBack5  = NavBtn("◀ 5秒",  98,  4, 80);
            _btnBack5.Click  += (s, e) => Navigate(-5);

            _txtTime = new TextBox
            {
                Location  = new Point(182, 6),
                Size      = new Size(80, 24),
                Text      = "00:00:00",
                TextAlign = HorizontalAlignment.Center,
                Font      = new Font("Consolas", 9f),
                Enabled   = false
            };
            _txtTime.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Return) { e.SuppressKeyPress = true; SeekToTextBox(); }
            };

            _btnGo   = NavBtn("移動",    266, 4, 50);
            _btnGo.Click   += (s, e) => SeekToTextBox();
            _btnFwd5  = NavBtn("5秒 ▶",  320, 4, 80);
            _btnFwd5.Click  += (s, e) => Navigate(5);
            _btnFwd10 = NavBtn("10秒 ▶▶", 404, 4, 90);
            _btnFwd10.Click += (s, e) => Navigate(10);

            _lblDuration = new Label
            {
                Location  = new Point(498, 7),
                Size      = new Size(220, 22),
                Text      = "全体: --:--:--",
                ForeColor = Color.FromArgb(200, 200, 200),
                Font      = new Font("Meiryo UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 説明行 (Y=38)
            _lblNote = new Label
            {
                Location  = new Point(4, 38),
                Size      = new Size(760, 36),
                Text      = "ナビゲーションボタンで位置を移動し、「現在位置を開始に設定」「現在位置を終了に設定」ボタンで変換範囲を指定してください。",
                ForeColor = Color.FromArgb(200, 200, 200),
                Font      = new Font("Meiryo UI", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _ctrlPanel.Controls.AddRange(new Control[]
            {
                _btnBack10, _btnBack5, _txtTime, _btnGo, _btnFwd5, _btnFwd10,
                _lblDuration, _lblNote
            });

            Controls.AddRange(new Control[] { _infoLabel, _pic, _ctrlPanel });
        }

        // ─── レイアウト ───────────────────────────────────────────────
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            const int infoH = 28;
            const int ctrlH = 80;
            int w    = Math.Max(0, Width);
            int h    = Math.Max(0, Height);
            int picH = Math.Max(0, h - infoH - ctrlH);
            _infoLabel.Bounds = new Rectangle(0, 0,              w, infoH);
            _pic.Bounds       = new Rectangle(0, infoH,          w, picH);
            _ctrlPanel.Bounds = new Rectangle(0, h - ctrlH,      w, ctrlH);
        }

        // ─── 公開メソッド ─────────────────────────────────────────────
        public void LoadVideo(string filePath, double duration)
        {
            _videoFile    = filePath;
            _duration     = duration;
            _currentTime  = 0;
            _txtTime.Text = "00:00:00";
            UpdateInfoLabel();
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
            SetButtonsEnabled(!converting && _videoFile != null);
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
            if (TryParseHms(_txtTime.Text, out double t))
            {
                t = Math.Max(0, t);
                if (_duration > 0) t = Math.Min(t, _duration);
                SeekTo(t);
            }
            else
            {
                _txtTime.Text = SecondsToHms(_currentTime);
            }
        }

        private void SeekTo(double seconds)
        {
            _currentTime  = seconds;
            _txtTime.Text = SecondsToHms(seconds);
            UpdateInfoLabel();
            TimeChanged?.Invoke(seconds);
            _ = GenerateThumbnailAsync(seconds);
        }

        private void UpdateInfoLabel()
        {
            string dur = _duration > 0 ? SecondsToHms(_duration) : "--:--:--";
            _infoLabel.Text =
                $"代替プレビュー（サムネイル）— 現在位置: {SecondsToHms(_currentTime)} / {dur}";
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

            try
            {
                Directory.CreateDirectory(_tempDir);
                string thumbFile = Path.Combine(_tempDir, "thumb.jpg");

                int hh  = (int)(seconds / 3600);
                int mm  = (int)((seconds % 3600) / 60);
                double ss = seconds % 60;
                string timeArg = $"{hh:D2}:{mm:D2}:{ss:06.3f}";
                string args = $"-ss {timeArg} -i \"{_videoFile}\" " +
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
                    bmp = new Bitmap(tmp); // copy to release the stream

                if (ct.IsCancellationRequested) { bmp.Dispose(); return; }

                if (InvokeRequired)
                {
                    Invoke(new Action(() => SetImage(bmp)));
                }
                else
                {
                    SetImage(bmp);
                }
            }
            catch (OperationCanceledException) { }
            catch { }
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
            _btnFwd5.Enabled   = enabled;
            _btnFwd10.Enabled  = enabled;
            _btnGo.Enabled     = enabled;
            _txtTime.Enabled   = enabled;
        }

        private static Button NavBtn(string text, int x, int y, int w) =>
            new Button
            {
                Text                  = text,
                Location              = new Point(x, y),
                Size                  = new Size(w, 28),
                UseVisualStyleBackColor = true,
                Font                  = new Font("Meiryo UI", 8.5f),
                Enabled               = false
            };

        private static string SecondsToHms(double s)
        {
            if (double.IsNaN(s) || double.IsInfinity(s)) return "00:00:00";
            int h   = (int)(s / 3600);
            int m   = (int)((s % 3600) / 60);
            int sec = (int)(s % 60);
            return $"{h:D2}:{m:D2}:{sec:D2}";
        }

        private static bool TryParseHms(string text, out double seconds)
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
            if (double.TryParse(text, out double secs) && secs >= 0)
            { seconds = secs; return true; }
            return false;
        }
    }
}
