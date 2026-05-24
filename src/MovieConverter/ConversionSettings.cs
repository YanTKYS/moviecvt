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
        FastCut,      // しない（高速カット）
        HighQuality,  // 画質優先
        Standard,     // 標準
        SmallSize     // 容量優先
    }

    public enum ResolutionPreset
    {
        Original,  // 元のまま
        P720,      // 720p
        P480       // 480p
    }

    public class ConversionSettings
    {
        public string InputFile { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public QualityPreset Quality { get; set; } = QualityPreset.Standard;
        public ResolutionPreset Resolution { get; set; } = ResolutionPreset.P720;
        public ConversionMode Mode { get; set; } = ConversionMode.RangeOnly;
    }
}
