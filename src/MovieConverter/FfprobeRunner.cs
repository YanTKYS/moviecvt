using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MovieConverter
{
    public record VideoInfo(
        string Duration,
        double DurationSeconds,
        string Resolution,
        string VideoCodec,
        string AudioCodec,
        string FrameRate,
        string FileSize,
        string Bitrate
    );

    public class FfprobeRunner
    {
        private static readonly string FfprobeRelativePath =
            Path.Combine("bin", "ffmpeg", "ffprobe.exe");

        public string FfprobePath { get; }

        public FfprobeRunner()
        {
            string exeDir = Path.GetDirectoryName(Environment.ProcessPath)
                ?? AppContext.BaseDirectory;
            FfprobePath = Path.Combine(exeDir, FfprobeRelativePath);
        }

        public bool IsAvailable => File.Exists(FfprobePath);

        public async Task<VideoInfo?> GetVideoInfoAsync(
            string inputFile,
            CancellationToken ct = default)
        {
            if (!IsAvailable) return null;

            var psi = new ProcessStartInfo
            {
                FileName = FfprobePath,
                Arguments = $"-v error -print_format json -show_format -show_streams \"{inputFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            var outputBuffer = new StringBuilder();
            var tcs = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) outputBuffer.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) => { };
            process.Exited += (s, e) => tcs.TrySetResult(outputBuffer.ToString());

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                tcs.TrySetCanceled();
            }))
            {
                try
                {
                    string json = await tcs.Task.ConfigureAwait(false);
                    return ParseVideoInfo(json);
                }
                catch { return null; }
            }
        }

        private static VideoInfo? ParseVideoInfo(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string duration = "", resolution = "", videoCodec = "",
                       audioCodec = "", frameRate = "", fileSize = "", bitrate = "";
                double durationSeconds = 0;

                if (root.TryGetProperty("streams", out var streams))
                {
                    foreach (var stream in streams.EnumerateArray())
                    {
                        string codecType = stream.TryGetProperty("codec_type", out var ct)
                            ? ct.GetString() ?? "" : "";

                        if (codecType == "video" && string.IsNullOrEmpty(videoCodec))
                        {
                            if (stream.TryGetProperty("codec_name", out var cn))
                                videoCodec = FriendlyVideoCodec(cn.GetString() ?? "");

                            if (stream.TryGetProperty("width", out var w) &&
                                stream.TryGetProperty("height", out var h))
                                resolution = $"{w.GetInt32()}×{h.GetInt32()}";

                            if (stream.TryGetProperty("r_frame_rate", out var fps))
                                frameRate = ParseFrameRate(fps.GetString() ?? "");

                            if (string.IsNullOrEmpty(duration) &&
                                stream.TryGetProperty("duration", out var sd) &&
                                double.TryParse(sd.GetString(), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out double vs))
                            {
                                duration = SecondsToHms(vs);
                                durationSeconds = vs;
                            }
                        }
                        else if (codecType == "audio" && string.IsNullOrEmpty(audioCodec))
                        {
                            if (stream.TryGetProperty("codec_name", out var cn))
                                audioCodec = FriendlyAudioCodec(cn.GetString() ?? "");
                        }
                    }
                }

                if (root.TryGetProperty("format", out var format))
                {
                    if (string.IsNullOrEmpty(duration) &&
                        format.TryGetProperty("duration", out var fd) &&
                        double.TryParse(fd.GetString(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out double fs))
                    {
                        duration = SecondsToHms(fs);
                        durationSeconds = fs;
                    }

                    if (format.TryGetProperty("size", out var sz) &&
                        long.TryParse(sz.GetString(), out long bytes))
                        fileSize = FormatFileSize(bytes);

                    if (format.TryGetProperty("bit_rate", out var br) &&
                        long.TryParse(br.GetString(), out long bps))
                        bitrate = FormatBitrate(bps);
                }

                return new VideoInfo(duration, durationSeconds, resolution, videoCodec, audioCodec,
                                     frameRate, fileSize, bitrate);
            }
            catch { return null; }
        }

        private static string FriendlyVideoCodec(string name) =>
            name.ToLowerInvariant() switch
            {
                "h264" or "libx264"       => "H.264 / AVC",
                "hevc" or "libx265"       => "H.265 / HEVC",
                "vp9"  or "libvpx-vp9"   => "VP9",
                "vp8"  or "libvpx"        => "VP8",
                "av1"  or "libaom-av1"    => "AV1",
                "mpeg4"                   => "MPEG-4",
                "mpeg2video"              => "MPEG-2",
                _                         => name.ToUpperInvariant()
            };

        private static string FriendlyAudioCodec(string name) =>
            name.ToLowerInvariant() switch
            {
                "aac"                                       => "AAC",
                "mp3"                                       => "MP3",
                "ac3"                                       => "AC-3",
                "eac3"                                      => "E-AC-3",
                "dts"                                       => "DTS",
                "flac"                                      => "FLAC",
                "opus"                                      => "Opus",
                "vorbis"                                    => "Vorbis",
                "pcm_s16le" or "pcm_s24le" or "pcm_s32le" => "PCM",
                _                                           => name.ToUpperInvariant()
            };

        private static string ParseFrameRate(string rateStr)
        {
            var parts = rateStr.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int num) &&
                int.TryParse(parts[1], out int den) &&
                den > 0)
            {
                double fps = (double)num / den;
                if (fps <= 0 || fps > 500) return "";
                return fps % 1 == 0 ? $"{(int)fps} fps" : $"{fps:F2} fps";
            }
            return "";
        }

        private static string SecondsToHms(double totalSeconds)
        {
            if (double.IsNaN(totalSeconds) || totalSeconds < 0) return "";
            int h = (int)(totalSeconds / 3600);
            int m = (int)((totalSeconds % 3600) / 60);
            int s = (int)(totalSeconds % 60);
            return $"{h:D2}:{m:D2}:{s:D2}";
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)     return $"{bytes / 1_048_576.0:F0} MB";
            if (bytes >= 1_024)         return $"{bytes / 1_024.0:F0} KB";
            return $"{bytes} B";
        }

        private static string FormatBitrate(long bps)
        {
            if (bps >= 1_000_000) return $"{bps / 1_000_000.0:F1} Mbps";
            if (bps >= 1_000)     return $"{bps / 1_000.0:F0} Kbps";
            return $"{bps} bps";
        }
    }
}
