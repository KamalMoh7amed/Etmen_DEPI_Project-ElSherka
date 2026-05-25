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
        /// <summary>Maps a score to a <see cref="RiskLevel"/> using the standard thresholds.</summary>
        public static RiskLevel ToLevel(decimal score) => score switch
        {
            >= 0.70m => RiskLevel.High,
            >= 0.40m => RiskLevel.Medium,
            _        => RiskLevel.Low
        };

        /// <summary>Returns a Bootstrap / Tailwind colour token for the given level.</summary>
        public static string ToColor(RiskLevel level) => level switch
        {
            RiskLevel.High   => "danger",
            RiskLevel.Medium => "warning",
            RiskLevel.Low    => "success",
            _                => "secondary"
        };

        /// <summary>Returns an Arabic label for display in views.</summary>
        public static string ToArabicLabel(RiskLevel level) => level switch
        {
            RiskLevel.High   => "خطر مرتفع",
            RiskLevel.Medium => "خطر متوسط",
            RiskLevel.Low    => "خطر منخفض",
            _                => "غير محدد"
        };
    }
}
