using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MovieConverter
{
    /// <summary>
    /// ffmpeg.exe を外部プロセスとして呼び出し、動画変換を実行する。
    /// </summary>
    public class FfmpegRunner
    {
        private static readonly string FfmpegRelativePath =
            Path.Combine("bin", "ffmpeg", "ffmpeg.exe");

        public string FfmpegPath { get; }

        public FfmpegRunner()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            FfmpegPath = Path.Combine(appDir, FfmpegRelativePath);
        }

        public bool IsAvailable => File.Exists(FfmpegPath);

        /// <summary>
        /// 出力ファイルパスを生成する。タイムスタンプ付きで既存ファイルと衝突しない。
        /// </summary>
        public string BuildOutputPath(string inputFile)
        {
            string dir = Path.GetDirectoryName(inputFile) ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(inputFile);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeName = SanitizeFileName($"{baseName}_cut_{timestamp}");
            return Path.Combine(dir, safeName + ".mp4");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static string FormatTimeForFfmpeg(TimeSpan ts)
        {
            int hours = (int)ts.TotalHours;
            return $"{hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        private string BuildArguments(ConversionSettings settings, string outputFile)
        {
            var sb = new StringBuilder();

            // シーク位置を -i の前に指定（再エンコード時の高速シーク）
            sb.Append($"-ss {FormatTimeForFfmpeg(settings.StartTime)} ");
            sb.Append($"-to {FormatTimeForFfmpeg(settings.EndTime)} ");

            // 入力ファイル（スペース・日本語対応のためダブルクォートで囲む）
            sb.Append($"-i \"{settings.InputFile}\" ");

            // 解像度フィルタ
            switch (settings.Resolution)
            {
                case ResolutionPreset.P720:
                    sb.Append("-vf scale=-2:720 ");
                    break;
                case ResolutionPreset.P480:
                    sb.Append("-vf scale=-2:480 ");
                    break;
                // Original: フィルタなし
            }

            // 品質プリセット（再エンコード方式：カット精度を優先）
            switch (settings.Quality)
            {
                case QualityPreset.HighQuality:
                    sb.Append("-c:v libx264 -crf 23 -preset medium -c:a aac -b:a 160k ");
                    break;
                case QualityPreset.SmallSize:
                    sb.Append("-c:v libx264 -crf 32 -preset medium -c:a aac -b:a 96k ");
                    break;
                default: // Standard
                    sb.Append("-c:v libx264 -crf 28 -preset medium -c:a aac -b:a 128k ");
                    break;
            }

            // 出力ファイル（上書きなし：ファイル名にタイムスタンプを含むため -y は付けない）
            sb.Append($"\"{outputFile}\"");

            return sb.ToString();
        }

        /// <summary>
        /// 変換を非同期で実行する。
        /// </summary>
        /// <param name="settings">変換設定</param>
        /// <param name="outputFile">出力ファイルパス</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <param name="logCallback">ログ出力コールバック（バックグラウンドスレッドから呼ばれる場合あり）</param>
        /// <param name="completedCallback">完了時コールバック。success, exitCode（キャンセル時はnull）</param>
        public async Task RunAsync(
            ConversionSettings settings,
            string outputFile,
            CancellationToken ct,
            Action<string> logCallback,
            Action<bool, int?> completedCallback)
        {
            if (!IsAvailable)
                throw new FileNotFoundException(
                    $"ffmpeg.exe が見つかりません。\n配置場所: {FfmpegPath}\n\ndocs/ffmpeg_setup.md を参照して配置してください。");

            string args = BuildArguments(settings, outputFile);
            logCallback($"[実行] ffmpeg {args}");

            var psi = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            process.Exited += (s, e) => tcs.TrySetResult(process.ExitCode);

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) logCallback(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) logCallback(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (ct.Register(() =>
            {
                try
                {
                    if (!process.HasExited) process.Kill();
                }
                catch { /* プロセスが既に終了している場合は無視 */ }
                tcs.TrySetCanceled();
            }))
            {
                try
                {
                    int exitCode = await tcs.Task.ConfigureAwait(false);
                    completedCallback(exitCode == 0, exitCode);
                }
                catch (TaskCanceledException)
                {
                    completedCallback(false, null);
                }
            }
        }
    }
}
