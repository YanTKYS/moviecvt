using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MovieConverter
{
    public enum DiagnosticLevel { Ok, Warning, Error }

    public record DiagnosticItem(
        string Label,
        DiagnosticLevel Level,
        string StatusText,
        string Detail,
        string? Guidance = null
    );

    public class EnvironmentDiagnostics
    {
        private readonly string _appDir;
        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;
        private readonly string? _inputFile;

        public EnvironmentDiagnostics(
            string appDir, string ffmpegPath, string ffprobePath, string? inputFile)
        {
            _appDir      = appDir;
            _ffmpegPath  = ffmpegPath;
            _ffprobePath = ffprobePath;
            _inputFile   = inputFile;
        }

        public async Task<List<DiagnosticItem>> RunAllAsync(CancellationToken ct = default)
        {
            var results = new List<DiagnosticItem>();

            // ─ アプリ情報 ─
            string version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            results.Add(new DiagnosticItem(
                "アプリバージョン", DiagnosticLevel.Ok, version, _appDir));

            // ─ WebView2 ─
            results.Add(CheckWebView2());

            // ─ ffmpeg ─
            results.Add(CheckToolExists("ffmpeg.exe", _ffmpegPath,
                "変換を実行するには ffmpeg.exe が必要です。docs/ffmpeg_setup.md を参照してください。",
                isOptional: false));
            if (File.Exists(_ffmpegPath))
                results.Add(await CheckVersionAsync("ffmpeg 起動確認", _ffmpegPath, "-version", ct));

            // ─ ffprobe ─
            results.Add(CheckToolExists("ffprobe.exe", _ffprobePath,
                "動画情報の表示に ffprobe.exe が必要です（省略可）。変換は実行できます。",
                isOptional: true));
            if (File.Exists(_ffprobePath))
                results.Add(await CheckVersionAsync("ffprobe 起動確認", _ffprobePath, "-version", ct));

            // ─ ファイルシステム ─
            results.Add(CheckWritable("ログフォルダ", Path.Combine(_appDir, "logs"),
                "管理者権限の確認、またはフォルダの設定を確認してください。",
                isError: false));

            // ─ 選択中の動画ファイル（ある場合のみ）─
            if (!string.IsNullOrEmpty(_inputFile))
            {
                results.Add(CheckInputFileExists());
                string? outputDir = Path.GetDirectoryName(_inputFile);
                if (!string.IsNullOrEmpty(outputDir))
                    results.Add(CheckWritable("出力先フォルダ", outputDir,
                        "出力先への書き込み権限がありません。管理者に相談してください。",
                        isError: true));
            }

            return results;
        }

        private static DiagnosticItem CheckWebView2()
        {
            try
            {
                string? ver = Microsoft.Web.WebView2.Core.CoreWebView2Environment
                    .GetAvailableBrowserVersionString();
                return new DiagnosticItem(
                    "WebView2 Runtime", DiagnosticLevel.Ok, "インストール済み", ver ?? "");
            }
            catch
            {
                return new DiagnosticItem(
                    "WebView2 Runtime",
                    DiagnosticLevel.Warning,
                    "未インストール",
                    "動画プレビューは利用できません。変換は実行できます。",
                    "WebView2 Runtime（Microsoft Edge WebView2 Runtime）が必要です。管理者に相談してください。");
            }
        }

        private static DiagnosticItem CheckToolExists(
            string label, string path, string guidance, bool isOptional)
        {
            if (File.Exists(path))
                return new DiagnosticItem(label, DiagnosticLevel.Ok, "配置済み", path);

            return new DiagnosticItem(
                label,
                isOptional ? DiagnosticLevel.Warning : DiagnosticLevel.Error,
                "未配置",
                $"配置先: {path}",
                guidance);
        }

        private static async Task<DiagnosticItem> CheckVersionAsync(
            string label, string exePath, string args, CancellationToken ct)
        {
            try
            {
                var (exitCode, output) = await RunProcessAsync(exePath, args, ct);

                if (exitCode == 0)
                {
                    string versionLine = "";
                    foreach (string line in output.Split('\n'))
                    {
                        string t = line.Trim();
                        if (t.Length > 0)
                        {
                            versionLine = t.Length > 64 ? t[..64] + "..." : t;
                            break;
                        }
                    }
                    return new DiagnosticItem(label, DiagnosticLevel.Ok, "正常", versionLine);
                }

                // DLL 不足（STATUS_DLL_NOT_FOUND / STATUS_INVALID_IMAGE_FORMAT）
                if (exitCode == unchecked((int)0xC0000135) ||
                    exitCode == unchecked((int)0xC000007B))
                    return new DiagnosticItem(
                        label,
                        DiagnosticLevel.Error,
                        "DLL 不足",
                        $"終了コード: 0x{unchecked((uint)exitCode):X8}",
                        "bin\\ffmpeg\\ フォルダに DLL 一式（avcodec-*.dll、avformat-*.dll 等）が配置されているか確認してください。");

                return new DiagnosticItem(
                    label,
                    DiagnosticLevel.Error,
                    "起動失敗",
                    $"終了コード: {exitCode}",
                    "管理者に相談してください。");
            }
            catch (OperationCanceledException)
            {
                return new DiagnosticItem(
                    label, DiagnosticLevel.Warning, "タイムアウト", "確認に時間がかかりすぎました。");
            }
            catch (Exception ex)
            {
                return new DiagnosticItem(label, DiagnosticLevel.Error, "エラー", ex.Message);
            }
        }

        private static DiagnosticItem CheckWritable(
            string label, string dir, string guidance, bool isError)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string test = Path.Combine(dir, $".write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(test, "test");
                File.Delete(test);
                return new DiagnosticItem(label, DiagnosticLevel.Ok, "書き込み可", dir);
            }
            catch (Exception ex)
            {
                return new DiagnosticItem(
                    label,
                    isError ? DiagnosticLevel.Error : DiagnosticLevel.Warning,
                    "書き込み不可",
                    ex.Message,
                    guidance);
            }
        }

        private DiagnosticItem CheckInputFileExists()
        {
            if (File.Exists(_inputFile))
                return new DiagnosticItem(
                    "選択中の動画ファイル", DiagnosticLevel.Ok, "存在する", _inputFile!);
            return new DiagnosticItem(
                "選択中の動画ファイル",
                DiagnosticLevel.Error,
                "見つかりません",
                _inputFile ?? "",
                "ファイルが移動・削除されていないか確認してください。");
        }

        private static async Task<(int exitCode, string output)> RunProcessAsync(
            string fileName, string arguments, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var output = new StringBuilder();
            var tcs = new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived  += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.Exited             += (s, e) => tcs.TrySetResult(process.ExitCode);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(TimeSpan.FromSeconds(5));

            using (linked.Token.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                tcs.TrySetCanceled();
            }))
            {
                int code = await tcs.Task.ConfigureAwait(false);
                return (code, output.ToString());
            }
        }

        public string FormatAsText(List<DiagnosticItem> items)
        {
            string version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";

            var sb = new StringBuilder();
            sb.AppendLine("動作確認レポート");
            sb.AppendLine($"生成日時           : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"アプリバージョン    : {version}");
            sb.AppendLine($"実行フォルダ        : {_appDir}");
            sb.AppendLine($"OS                 : {Environment.OSVersion}");
            sb.AppendLine();
            sb.AppendLine(new string('─', 52));
            sb.AppendLine();

            foreach (var item in items)
            {
                string mark = item.Level switch
                {
                    DiagnosticLevel.Ok      => "[ OK ]",
                    DiagnosticLevel.Warning => "[注意]",
                    DiagnosticLevel.Error   => "[ NG ]",
                    _                       => "[    ]"
                };
                sb.AppendLine($"{mark}  {item.Label}: {item.StatusText}");
                if (!string.IsNullOrEmpty(item.Detail))
                    sb.AppendLine($"         {item.Detail}");
                if (!string.IsNullOrEmpty(item.Guidance))
                    sb.AppendLine($"         → {item.Guidance}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
