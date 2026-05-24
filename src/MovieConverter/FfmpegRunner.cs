using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MovieConverter
{
    public class FfmpegRunner
    {
        // ffmpegの進捗出力例: "frame=  120 fps=25 q=28.0 size=1024kB time=00:01:23.45 bitrate=..."
        private static readonly Regex TimePattern =
            new Regex(@"time=(\d+):(\d+):(\d+\.\d+)", RegexOptions.Compiled);

        private static bool TryParseProgressTime(string line, out double seconds)
        {
            seconds = 0;
            var m = TimePattern.Match(line);
            if (!m.Success) return false;
            int h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            int mi = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            double s = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            seconds = h * 3600 + mi * 60 + s;
            return true;
        }

        private static readonly string FfmpegRelativePath =
            Path.Combine("bin", "ffmpeg", "ffmpeg.exe");

        public string FfmpegPath { get; }

        public FfmpegRunner()
        {
            // 単一ファイル発行時は AppContext.BaseDirectory が一時展開先になるため、
            // 実行ファイルの物理パスから親フォルダを取得する
            string exeDir = Path.GetDirectoryName(Environment.ProcessPath)
                ?? AppContext.BaseDirectory;
            FfmpegPath = Path.Combine(exeDir, FfmpegRelativePath);
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

            // 選択範囲変換のみ -ss/-to を付ける（全体変換時はパラメータなし）
            if (settings.Mode == ConversionMode.RangeOnly)
            {
                sb.Append($"-ss {FormatTimeForFfmpeg(settings.StartTime)} ");
                sb.Append($"-to {FormatTimeForFfmpeg(settings.EndTime)} ");
            }

            // 入力ファイル（スペース・日本語対応のためダブルクォートで囲む）
            sb.Append($"-i \"{settings.InputFile}\" ");

            if (settings.Quality == QualityPreset.FastCut)
            {
                // 高速カット: ストリームをそのままコピー（再エンコードなし）
                // カット位置は直前のキーフレームに丸められる場合がある
                sb.Append("-c copy ");
            }
            else
            {
                // 圧縮変換: 解像度フィルタ（FastCut時は無効）
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

                // 品質プリセット（再エンコード方式）
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
            }

            // 出力ファイル（上書きなし：ファイル名にタイムスタンプを含むため -y は付けない）
            sb.Append($"\"{outputFile}\"");

            return sb.ToString();
        }

        /// <summary>
        /// 事前変換の出力ファイルパスを生成する。
        /// </summary>
        public string BuildPreconvertedOutputPath(string inputFile)
        {
            string dir = Path.GetDirectoryName(inputFile) ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(inputFile);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeName = SanitizeFileName($"{baseName}_preconverted_{timestamp}");
            return Path.Combine(dir, safeName + ".mp4");
        }

        /// <param name="totalDurationSeconds">進捗計算に使う総秒数（範囲変換なら範囲長、全体変換なら動画時間）</param>
        /// <param name="progressCallback">進捗コールバック（0.0〜1.0）。バックグラウンドスレッドから呼ばれる。</param>
        public async Task RunAsync(
            ConversionSettings settings,
            string outputFile,
            double totalDurationSeconds,
            CancellationToken ct,
            Action<string> logCallback,
            Action<double>? progressCallback,
            Action<bool, int?> completedCallback)
        {
            if (!IsAvailable)
                throw new FileNotFoundException(
                    $"ffmpeg.exe が見つかりません。\n配置場所: {FfmpegPath}\n\ndocs/ffmpeg_setup.md を参照して配置してください。");

            string args = BuildArguments(settings, outputFile);
            await RunProcessAsync(args, totalDurationSeconds, ct, logCallback, progressCallback, completedCallback)
                .ConfigureAwait(false);
        }

        public async Task RunFaststartAsync(
            string inputFile,
            string outputFile,
            double totalDurationSeconds,
            CancellationToken ct,
            Action<string> logCallback,
            Action<double>? progressCallback,
            Action<bool, int?> completedCallback)
        {
            if (!IsAvailable)
                throw new FileNotFoundException(
                    $"ffmpeg.exe が見つかりません。\n配置場所: {FfmpegPath}\n\ndocs/ffmpeg_setup.md を参照して配置してください。");

            string args = $"-i \"{inputFile}\" -c copy -movflags +faststart \"{outputFile}\"";
            await RunProcessAsync(args, totalDurationSeconds, ct, logCallback, progressCallback, completedCallback)
                .ConfigureAwait(false);
        }

        private async Task RunProcessAsync(
            string args,
            double totalDurationSeconds,
            CancellationToken ct,
            Action<string> logCallback,
            Action<double>? progressCallback,
            Action<bool, int?> completedCallback)
        {
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
                if (e.Data == null) return;
                // 進捗行（time= を含む）は進捗コールバックに渡してlogには送らない
                if (progressCallback != null &&
                    TryParseProgressTime(e.Data, out double timeSec) &&
                    totalDurationSeconds > 0)
                {
                    progressCallback(Math.Min(1.0, timeSec / totalDurationSeconds));
                    return;
                }
                logCallback(e.Data);
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
