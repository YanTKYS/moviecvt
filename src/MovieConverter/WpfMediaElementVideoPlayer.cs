using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace MovieConverter
{
    /// <summary>
    /// WPF MediaElement を ElementHost で WinForms に埋め込んだ代替プレビュー実装。
    /// WebView2 が動作しない端末環境や動画向けのフォールバック方式。
    /// </summary>
    public class WpfMediaElementVideoPlayer : IVideoPlayer
    {
        private readonly ElementHost _host;
        private readonly System.Windows.Controls.MediaElement _mediaElement;
        private readonly System.Windows.Controls.TextBlock _hintText;
        private readonly System.Windows.Forms.Timer _positionTimer;

        // LoadVideo 内部で Play() して読み込みを開始するが、
        // その間は PlaybackStarted / PlaybackPaused を UI に伝えない
        private bool _loadingOnly;
        private double _duration;

        // ─── IVideoPlayer ──────────────────────────────────────────────
        public Control PreviewControl => _host;
        public bool IsReady { get; private set; }

        public event Action<double>?          VideoLoaded;
        public event Action<double, double>?  TimeUpdated;
        public event Action?                  PlaybackStarted;
        public event Action?                  PlaybackPaused;
        public event Action?                  PlaybackEnded;
        public event Action?                  PlaybackBlocked;
        public event Action<string>?          VideoError;
        public event Action<string>?          FileDropped;
        public event Action<string>?          LogMessage;

        // ─── コンストラクタ ───────────────────────────────────────────
        public WpfMediaElementVideoPlayer()
        {
            _mediaElement = new System.Windows.Controls.MediaElement
            {
                LoadedBehavior   = System.Windows.Controls.MediaState.Manual,
                UnloadedBehavior = System.Windows.Controls.MediaState.Stop,
                Stretch          = System.Windows.Media.Stretch.Uniform,
                Volume           = 0.8
            };
            _mediaElement.MediaOpened += OnMediaOpened;
            _mediaElement.MediaFailed += OnMediaFailed;
            _mediaElement.MediaEnded  += OnMediaEnded;

            _hintText = new System.Windows.Controls.TextBlock
            {
                Text = "MP4ファイルを選択すると、ここにプレビューが表示されます",
                Foreground              = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment     = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment       = System.Windows.VerticalAlignment.Center,
                FontFamily              = new System.Windows.Media.FontFamily("Meiryo UI"),
                FontSize                = 13,
                TextWrapping            = System.Windows.TextWrapping.Wrap,
                TextAlignment           = System.Windows.TextAlignment.Center
            };

            var grid = new System.Windows.Controls.Grid();
            grid.Background = System.Windows.Media.Brushes.Black;
            grid.Children.Add(_mediaElement);
            grid.Children.Add(_hintText);

            _host = new ElementHost
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Child     = grid
            };

            // WinForms OLE ドラッグ＆ドロップ（ElementHost は WPF D&D とは別系統）
            _host.AllowDrop = true;
            _host.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            _host.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] files &&
                    files.Length > 0 &&
                    Path.GetExtension(files[0]).ToLowerInvariant() == ".mp4")
                    FileDropped?.Invoke(files[0]);
            };

            // 再生位置ポーリング（WPF に position-changed イベントがないため）
            _positionTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _positionTimer.Tick += OnPositionTimerTick;

            IsReady = true;
        }

        // WPF MediaElement は同期初期化のため appDirectory 不要
        public Task InitializeAsync(string appDirectory) => Task.CompletedTask;

        // ─── 動画読み込み ─────────────────────────────────────────────
        public void LoadVideo(string filePath)
        {
            _positionTimer.Stop();
            _duration = 0;
            _loadingOnly = true;
            _hintText.Visibility = System.Windows.Visibility.Hidden;
            _mediaElement.Source = new Uri(filePath);
            // LoadedBehavior=Manual では Play() 呼び出しで読み込みが始まり MediaOpened が発火する
            _mediaElement.Play();
            LogMessage?.Invoke("[代替プレイヤー] 動画を読み込み中...");
        }

        // ─── 再生コントロール ─────────────────────────────────────────
        public void Play()
        {
            _loadingOnly = false;
            _mediaElement.Play();
            _positionTimer.Start();
            PlaybackStarted?.Invoke();
        }

        public void Pause()
        {
            _mediaElement.Pause();
            _positionTimer.Stop();
            if (!_loadingOnly)
                PlaybackPaused?.Invoke();
            _loadingOnly = false;
        }

        public void Seek(double seconds)
        {
            double clamped = Math.Max(0, Math.Min(seconds, _duration > 0 ? _duration : seconds));
            _mediaElement.Position = TimeSpan.FromSeconds(clamped);
            TimeUpdated?.Invoke(clamped, _duration);
        }

        public void SetVolume(double volume)
            => _mediaElement.Volume = Math.Max(0.0, Math.Min(1.0, volume));

        public void SetMute(bool muted)
            => _mediaElement.IsMuted = muted;

        // ─── WPF イベントハンドラ（UI スレッドで発火）──────────────────
        private void OnMediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            _duration = _mediaElement.NaturalDuration.HasTimeSpan
                ? _mediaElement.NaturalDuration.TimeSpan.TotalSeconds
                : 0;
            _hintText.Visibility = System.Windows.Visibility.Collapsed;

            // 読み込みトリガーとして呼んだ Play() を停止し、ユーザーに再生を委ねる
            _mediaElement.Pause();           // _loadingOnly=true なので PlaybackPaused は発火しない
            _positionTimer.Stop();
            _loadingOnly = false;

            LogMessage?.Invoke("[代替プレイヤー 読み込み成功]");
            VideoLoaded?.Invoke(_duration);
        }

        private void OnMediaFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            _positionTimer.Stop();
            _loadingOnly = false;
            _hintText.Visibility = System.Windows.Visibility.Visible;
            string msg = e.ErrorException?.Message ?? "動画の読み込みに失敗しました";
            LogMessage?.Invoke($"[代替プレイヤー エラー] {msg}");
            VideoError?.Invoke(msg);
        }

        private void OnMediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            _positionTimer.Stop();
            PlaybackEnded?.Invoke();
        }

        private void OnPositionTimerTick(object? sender, EventArgs e)
        {
            if (_duration > 0)
                TimeUpdated?.Invoke(_mediaElement.Position.TotalSeconds, _duration);
        }
    }
}
