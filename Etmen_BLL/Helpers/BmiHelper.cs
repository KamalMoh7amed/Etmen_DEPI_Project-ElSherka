namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Provides BMI calculation and WHO-based categorization utilities.
    /// </summary>
    public static class BmiHelper
    {
        public static decimal Calculate(decimal weightKg, decimal heightCm)
        {
            if (heightCm <= 0 || weightKg <= 0) return 0;
            var heightM = heightCm / 100;
            return Math.Round(weightKg / (heightM * heightM), 2);
        }

        /// <summary>
        /// Returns WHO standard BMI category string.
        /// </summary>
        public static string GetCategory(decimal bmi) => bmi switch
        {
            <= 0            => "غير محدد",
            < 18.5m         => "نقص الوزن",
            < 25m           => "وزن طبيعي",
            < 30m           => "زيادة الوزن",
            < 35m           => "سمنة - درجة أولى",
            < 40m           => "سمنة - درجة ثانية",
            _               => "سمنة مفرطة - درجة ثالثة"
        };

        /// <summary>
        /// Returns a CSS-friendly color class based on BMI range.
        /// </summary>
        public static string GetCategoryColor(decimal bmi) => bmi switch
        {
            <= 0     => "secondary",
            < 18.5m  => "info",
            < 25m    => "success",
            < 30m    => "warning",
            _        => "danger"
        };
    }
}
