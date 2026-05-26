using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MovieConverter
{
    /// <summary>
    /// Windows Media Player COM（wmp.dll）を使った IVideoPlayer 実装。
    ///
    /// WMPLib COMReference は dotnet build (MSBuild CoreCLR) では使用不可（MSB4803）のため、
    /// dynamic（IDispatch 経由の遅延バインド）で COM 操作する。
    ///
    /// 【検証用途のみ】Microsoft は WMP COM を今後推奨しない方針のため、
    /// 本クラスは長期前提での主方式として使用しない。
    ///
    /// 設計上の制約:
    ///   - COM オブジェクトはアプリ起動時に初期化しない（LoadVideo 時に遅延初期化）
    ///   - COM 初期化失敗・再生失敗はアプリを終了させず VideoError イベントで通知する
    ///   - 再生位置更新はポーリングタイマーで取得する（COM イベント接続なし）
    ///   - 再生できないファイルは VideoError → MainForm が代替プレビューへ切り替える
    /// </summary>
    public sealed class WmpVideoPlayer : IVideoPlayer
    {
        // Windows Media Player ActiveX コントロールの CLSID
        private const string WmpOcxClsid = "6BF52A4F-394A-11D3-B153-00C04F79FAA6";

        // ─── AxHost ラッパー ──────────────────────────────────────────
        /// <summary>WMP OCX を AxHost 経由で WinForms パネルに埋め込む薄いラッパー。</summary>
        private sealed class WmpAxHost : AxHost
        {
            public WmpAxHost() : base(WmpOcxClsid) { }
            protected override void AttachInterfaces() { }
            // GetOcx() が返す COM オブジェクトを object として渡す。
            // 呼び出し側が dynamic にキャストして IDispatch 経由で操作する。
            public object? GetOcxObject() => GetOcx();
        }

        // ─── コントロール ──────────────────────────────────────────────
        private readonly Panel _container;
        private readonly Label _hint;
        private WmpAxHost? _axHost;
        private object? _wmp;   // WMP COM オブジェクト（dynamic でアクセス）

        // ─── ポーリングタイマー ────────────────────────────────────────
        private readonly System.Windows.Forms.Timer _pollTimer;

        // ─── 状態 ─────────────────────────────────────────────────────
        private bool _initFailed;
        private bool _initialized;
        private bool _mediaLoaded;
        private double _lastPosition  = -1;
        private int    _lastPlayState = -1;
        private int    _loadTimeoutTicks;
        private const  int LoadTimeoutPollCount = 10; // 10 × 500ms = 5秒でタイムアウト

        // ─── IVideoPlayer プロパティ ───────────────────────────────────
        public Control PreviewControl => _container;
        public bool    IsReady        => _initialized && !_initFailed;

        // ─── IVideoPlayer イベント ─────────────────────────────────────
        public event Action<double>?         VideoLoaded;
        public event Action<double, double>? TimeUpdated;
        public event Action?                 PlaybackStarted;
        public event Action?                 PlaybackPaused;
        public event Action?                 PlaybackEnded;
#pragma warning disable CS0067 // WMP COM はこれらのイベントを発火しない（IVideoPlayer 実装上の要件）
        public event Action?                 PlaybackBlocked;
        public event Action<string>?         FileDropped;
#pragma warning restore CS0067
        public event Action<string>?         VideoError;
        public event Action<string>?         LogMessage;

        // ─── コンストラクタ ────────────────────────────────────────────
        public WmpVideoPlayer()
        {
            _hint = new Label
            {
                Text      = "WMPプレビュー（検証）\nMP4ファイルを選択するとプレビューが表示されます",
                ForeColor = Color.FromArgb(150, 150, 150),
                BackColor = Color.Transparent,
                Font      = new Font("Meiryo UI", 9f),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };

            _container = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Margin    = new Padding(0, 2, 0, 2)
            };
            _container.Controls.Add(_hint);

            _pollTimer          = new System.Windows.Forms.Timer { Interval = 500 };
            _pollTimer.Tick    += Poll;
        }

        // ─── IVideoPlayer: 初期化（起動時には何もしない） ──────────────
        /// <summary>WMP COM は起動時に初期化しない。LoadVideo 時に遅延初期化する。</summary>
        public Task InitializeAsync(string appDirectory) => Task.CompletedTask;

        // ─── IVideoPlayer: 動画読み込み ────────────────────────────────
        public void LoadVideo(string filePath)
        {
            if (_initFailed)
            {
                VideoError?.Invoke("WMPプレビューは初期化に失敗しています");
                return;
            }

            _mediaLoaded       = false;
            _lastPosition      = -1;
            _lastPlayState     = -1;
            _loadTimeoutTicks  = 0;
            _pollTimer.Stop();

            if (!EnsureInitialized()) return;

            try
            {
                dynamic wmp = _wmp!;
                wmp.settings.autoStart = false;
                wmp.URL = filePath;
                _pollTimer.Start();
                LogMessage?.Invoke($"[WMP] 読み込み開始: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                NotifyError($"WMP読み込みエラー: {ex.Message}");
            }
        }

        // ─── 再生コマンド ──────────────────────────────────────────────
        public void Play()
        {
            if (_wmp == null) return;
            try { dynamic d = _wmp; d.controls.play(); } catch { }
        }

        public void Pause()
        {
            if (_wmp == null) return;
            try { dynamic d = _wmp; d.controls.pause(); } catch { }
        }

        public void Seek(double seconds)
        {
            if (_wmp == null) return;
            try { dynamic d = _wmp; d.controls.currentPosition = seconds; } catch { }
        }

        public void SetVolume(double volume)
        {
            if (_wmp == null) return;
            try { dynamic d = _wmp; d.settings.volume = (int)Math.Round(volume * 100.0); } catch { }
        }

        public void SetMute(bool muted)
        {
            if (_wmp == null) return;
            try { dynamic d = _wmp; d.settings.mute = muted; } catch { }
        }

        // ─── 遅延初期化 ───────────────────────────────────────────────
        private bool EnsureInitialized()
        {
            if (_initialized) return true;
            try
            {
                _axHost = new WmpAxHost { Dock = DockStyle.Fill, Visible = false };
                _container.Controls.Add(_axHost);
                _axHost.CreateControl();

                _wmp = _axHost.GetOcxObject();
                if (_wmp == null)
                    throw new InvalidOperationException("WMP インターフェースの取得に失敗しました");

                dynamic wmp = _wmp;
                wmp.settings.autoStart = false;
                wmp.uiMode             = "none";

                _initialized     = true;
                _hint.Visible    = false;
                _axHost.Visible  = true;

                LogMessage?.Invoke("[WMP] Windows Media Player プレビューを初期化しました（検証モード）");
                return true;
            }
            catch (Exception ex)
            {
                return FailInit(ex.Message);
            }
        }

        private bool FailInit(string message)
        {
            _initFailed = true;
            LogMessage?.Invoke($"[WMP エラー] 初期化失敗: {message}");
            VideoError?.Invoke($"WMPプレビューを開始できませんでした: {message}");
            return false;
        }

        // ─── ポーリングタイマー ────────────────────────────────────────
        private void Poll(object? sender, EventArgs e)
        {
            if (_wmp == null) return;
            try
            {
                dynamic wmp = _wmp;
                int state = (int)wmp.playState;

                // currentMedia・controls は未ロード時に null を返す場合があるため個別に保護する
                double duration = 0;
                try
                {
                    object? mediaObj = wmp.currentMedia;
                    if (mediaObj != null) duration = (double)((dynamic)mediaObj).duration;
                }
                catch { }

                double position = 0;
                try
                {
                    object? ctrlObj = wmp.controls;
                    if (ctrlObj != null) position = (double)((dynamic)ctrlObj).currentPosition;
                }
                catch { }

                // メディア読み込み完了の検出
                if (!_mediaLoaded && duration > 0)
                {
                    _mediaLoaded = true;
                    VideoLoaded?.Invoke(duration);
                    LogMessage?.Invoke($"[WMP] 読み込み完了 — 動画時間: {FormatSec(duration)}");
                }

                // タイムアウト検出（5秒経過してもメディアが読み込まれない場合）
                if (!_mediaLoaded)
                {
                    _loadTimeoutTicks++;
                    // wmppsBuffering=6, wmppsTransitioning=9 は待機継続
                    if (_loadTimeoutTicks >= LoadTimeoutPollCount && state != 6 && state != 9)
                    {
                        NotifyError(
                            "メディアの読み込みがタイムアウトしました。" +
                            "WMPがこのファイルを再生できない可能性があります。");
                        return;
                    }
                }

                // 再生位置の更新
                if (_mediaLoaded && Math.Abs(position - _lastPosition) >= 0.1)
                {
                    _lastPosition = position;
                    TimeUpdated?.Invoke(position, duration);
                }

                // 再生状態変化の検出
                if (state != _lastPlayState)
                {
                    _lastPlayState = state;
                    switch (state)
                    {
                        case 3: PlaybackStarted?.Invoke(); break; // wmppsPlaying
                        case 2: PlaybackPaused?.Invoke();  break; // wmppsPaused
                        case 8: PlaybackEnded?.Invoke();   break; // wmppsMediaEnded
                    }
                }
            }
            catch
            {
                // COM エラー（WMP 解放後等）は無視
            }
        }

        // ─── エラー通知 ────────────────────────────────────────────────
        private void NotifyError(string message)
        {
            _pollTimer.Stop();
            LogMessage?.Invoke($"[WMP エラー] {message}");
            VideoError?.Invoke(message);
        }

        // ─── クリーンアップ ────────────────────────────────────────────
        public void StopAndCleanup()
        {
            _pollTimer.Stop();
            if (_wmp == null) return;
            try { dynamic d = _wmp; d.controls.stop(); } catch { }
        }

        // ─── ユーティリティ ────────────────────────────────────────────
        private static string FormatSec(double s)
        {
            if (double.IsNaN(s) || double.IsInfinity(s)) return "00:00:00";
            return $"{(int)(s / 3600):D2}:{(int)((s % 3600) / 60):D2}:{(int)(s % 60):D2}";
        }
    }
}
