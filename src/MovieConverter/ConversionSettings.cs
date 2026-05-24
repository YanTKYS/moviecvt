using System;

namespace MovieConverter
{
    public enum ConversionMode
    {
        RangeOnly,  // 選択範囲を変換
        FullVideo   // 動画全体を変換
    }

    public enum QualityPreset
    {
        FastCut       = 0,  // しない（高速カット）
        SpeedPriority = 1,  // 速度優先
        HighQuality   = 2,  // 画質優先
        Standard      = 3,  // 標準
        SmallSize     = 4   // 容量優先
    }

    public enum ResolutionPreset
    {
        Original,  // 元のまま
        P720,      // 720p
        P480       // 480p
    }

    public enum SpeedPreset
    {
        Default,   // medium（バランス重視・デフォルト）
        Fast,      // fast（やや高速）
        VeryFast   // veryfast（高速・品質やや低下）
    }

    public class ConversionSettings
    {
        public string InputFile { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public QualityPreset Quality { get; set; } = QualityPreset.Standard;
        public ResolutionPreset Resolution { get; set; } = ResolutionPreset.P720;
        public ConversionMode Mode { get; set; } = ConversionMode.RangeOnly;
        public SpeedPreset Speed { get; set; } = SpeedPreset.Default;
    }
}
