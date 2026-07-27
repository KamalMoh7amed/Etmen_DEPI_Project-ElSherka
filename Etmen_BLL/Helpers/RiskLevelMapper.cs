using Etmen_Domain.Enums;

namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Converts a numeric RiskScore (0.0 – 1.0) to a <see cref="RiskLevel"/> enum value,
    /// a display colour, and a human-readable Arabic description.
    /// Used by RiskService and CrisisRiskEngineService.
    /// </summary>
    public static class RiskLevelMapper
    {
        public static RiskLevel ToLevel(decimal score) => score switch
        {
            >= 0.70m => RiskLevel.High,
            >= 0.40m => RiskLevel.Medium,
            _        => RiskLevel.Low
        };

        public static string ToColor(RiskLevel level) => level switch
        {
            RiskLevel.Low       => "#10B981", // Emerald Green
            RiskLevel.Medium    => "#F59E0B", // Amber Orange
            RiskLevel.High      => "#F97316", // Orange
            RiskLevel.Critical  => "#E74C3C", // Red
            RiskLevel.Emergency => "#EF4444", // Crimson Red
            _                   => "#3B82F6"  // Blue
        };

        public static string ToArabicLabel(RiskLevel level) => level switch
        {
            RiskLevel.Low       => "خطر منخفض",
            RiskLevel.Medium    => "خطر متوسط",
            RiskLevel.High      => "خطر مرتفع",
            RiskLevel.Critical  => "حرج جداً",
            RiskLevel.Emergency => "حالة طوارئ قصوى 🚨",
            _                   => "غير محدد"
        };
    }
}
