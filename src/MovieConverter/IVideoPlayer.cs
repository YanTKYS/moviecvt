using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MovieConverter
{
    /// <summary>
    /// 動画プレビュープレイヤーの抽象インターフェース。
    /// WebView2 実装（WebView2VideoPlayer）または将来的な代替実装を差し替え可能にする。
    /// </summary>
    public interface IVideoPlayer
    {
        // ── UI ────────────────────────────────────────────────────────
        /// <summary>フォームのレイアウトに追加するコンテナコントロール。</summary>
        Control PreviewControl { get; }

        /// <summary>InitializeAsync が正常完了した後 true になる。</summary>
        bool IsReady { get; }

        // ── ライフサイクル ────────────────────────────────────────────
        /// <summary>
        /// プレイヤーを非同期初期化する。フォーム起動時に一度だけ呼ぶ。
        /// </summary>
        /// <param name="appDirectory">アプリ実行ファイルのディレクトリ（Assets/player.html 等の参照用）。</param>
        Task InitializeAsync(string appDirectory);

        // ── 動画読み込み ───────────────────────────────────────────────
        /// <summary>
        /// 指定ファイルを読み込む。IsReady=false の場合は準備完了後に自動読み込みする。
        /// </summary>
        void LoadVideo(string filePath);

        // ── 再生コントロール ───────────────────────────────────────────
        void Play();
        void Pause();

        /// <param name="seconds">シーク先の位置（秒）。</param>
        void Seek(double seconds);

        /// <param name="volume">音量 0.0〜1.0。</param>
        void SetVolume(double volume);
        void SetMute(bool muted);

        // ── イベント（すべて UI スレッドで発火）──────────────────────
        /// <summary>動画の読み込みが完了した。引数: 総秒数。</summary>
        event Action<double>? VideoLoaded;

        /// <summary>再生位置が更新された。引数: 現在位置（秒）、総秒数。</summary>
        event Action<double, double>? TimeUpdated;

        /// <summary>再生開始。</summary>
        event Action? PlaybackStarted;

        /// <summary>一時停止。</summary>
        event Action? PlaybackPaused;

        /// <summary>再生終了（末尾到達）。</summary>
        event Action? PlaybackEnded;

        /// <summary>ブラウザの自動再生ポリシーでブロックされた。</summary>
        event Action? PlaybackBlocked;

        /// <summary>
        /// すべてのロード方式が失敗した。引数: エラーメッセージ。
        /// このイベントが発火する前に LogMessage でフォールバック試行のログが出る。
        /// </summary>
        event Action<string>? VideoError;

        /// <summary>プレイヤー上にファイルがドロップされた。引数: ファイルパス。</summary>
        event Action<string>? FileDropped;

        /// <summary>アプリのログペインに表示すべきメッセージ。</summary>
        event Action<string>? LogMessage;
    }
}
