using Etmen_Domain.Enums;

namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Core risk scoring engine used by RiskAssessmentService.
    /// Calculates a normalized 0-1 risk score based on patient vitals and reported symptoms.
    /// Thresholds are based on standard clinical guidelines.
    /// </summary>
    public static class RiskCalculatorHelper
    {
        // ── Vital thresholds ──────────────────────────────────────────────────────
        private const decimal NormalSystolicMin    = 90m;
        private const decimal NormalSystolicMax    = 140m;
        private const decimal NormalDiastolicMin   = 60m;
        private const decimal NormalDiastolicMax   = 90m;
        private const decimal NormalHeartRateMin   = 60m;
        private const decimal NormalHeartRateMax   = 100m;
        private const decimal NormalTempMin        = 36.0m;
        private const decimal NormalTempMax        = 37.5m;
        private const decimal NormalOxygenMin      = 95m;
        private const decimal NormalBloodSugarMin  = 70m;
        private const decimal NormalBloodSugarMax  = 180m;

        // ── High-risk symptom keywords (Arabic + English) ─────────────────────────
        private static readonly HashSet<string> HighRiskSymptoms = new(StringComparer.OrdinalIgnoreCase)
        {
            "ألم في الصدر", "chest pain", "ضيق التنفس", "shortness of breath",
            "فقدان الوعي", "loss of consciousness", "نزيف", "bleeding",
            "شلل", "paralysis", "تشنجات", "seizures", "ألم شديد", "severe pain"
        };

        private static readonly HashSet<string> MediumRiskSymptoms = new(StringComparer.OrdinalIgnoreCase)
        {
            "حمى", "fever", "صداع", "headache", "دوخة", "dizziness",
            "غثيان", "nausea", "تقيؤ", "vomiting", "إرهاق", "fatigue",
            "آلام عضلية", "muscle aches", "سعال", "cough", "التهاب الحلق", "sore throat"
        };

        /// <summary>
        /// Computes a composite risk score (0.0 – 1.0) from patient vitals and symptoms.
        /// </summary>
        public static (decimal Score, bool IsEmergency, List<string> TriggeredFactors) Calculate(
            decimal? systolicBP,
            decimal? diastolicBP,
            decimal? heartRate,
            decimal? temperature,
            decimal? oxygenSaturation,
            decimal? bloodSugar,
            string? symptomsRaw)
        {
            var triggeredFactors = new List<string>();
            decimal totalScore = 0;
            int factors = 0;

            // ── Vital scoring ─────────────────────────────────────────────────────
            if (systolicBP.HasValue)
            {
                var score = ScoreVital(systolicBP.Value, NormalSystolicMin, NormalSystolicMax);
                totalScore += score;
                factors++;
                if (score > 0.5m) triggeredFactors.Add($"ضغط الدم الانقباضي غير طبيعي ({systolicBP} mmHg)");
            }

            if (diastolicBP.HasValue)
            {
                var score = ScoreVital(diastolicBP.Value, NormalDiastolicMin, NormalDiastolicMax);
                totalScore += score;
                factors++;
                if (score > 0.5m) triggeredFactors.Add($"ضغط الدم الانبساطي غير طبيعي ({diastolicBP} mmHg)");
            }

            if (heartRate.HasValue)
            {
                var score = ScoreVital(heartRate.Value, NormalHeartRateMin, NormalHeartRateMax);
                totalScore += score;
                factors++;
                if (score > 0.5m) triggeredFactors.Add($"معدل ضربات القلب غير طبيعي ({heartRate} bpm)");
            }

            if (temperature.HasValue)
            {
                var score = ScoreVital(temperature.Value, NormalTempMin, NormalTempMax);
                totalScore += score;
                factors++;
                if (score > 0.5m) triggeredFactors.Add($"درجة الحرارة غير طبيعية ({temperature}°C)");
            }

            if (oxygenSaturation.HasValue)
            {
                // Oxygen: lower is worse; anything below 95% is risky
                decimal score = oxygenSaturation.Value >= NormalOxygenMin ? 0 :
                                oxygenSaturation.Value >= 90m ? 0.6m :
                                oxygenSaturation.Value >= 85m ? 0.85m : 1.0m;
                totalScore += score;
                factors++;
                if (score > 0.4m) triggeredFactors.Add($"تشبع الأكسجين منخفض ({oxygenSaturation}%)");
            }

            if (bloodSugar.HasValue)
            {
                var score = ScoreVital(bloodSugar.Value, NormalBloodSugarMin, NormalBloodSugarMax);
                totalScore += score;
                factors++;
                if (score > 0.5m) triggeredFactors.Add($"مستوى السكر في الدم غير طبيعي ({bloodSugar} mg/dL)");
            }

            // ── Symptom scoring ───────────────────────────────────────────────────
            decimal symptomScore = 0;
            if (!string.IsNullOrWhiteSpace(symptomsRaw))
            {
                var symptoms = symptomsRaw.Split([',', '،', ';'], StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => s.Trim());

                foreach (var symptom in symptoms)
                {
                    if (HighRiskSymptoms.Any(h => symptom.Contains(h, StringComparison.OrdinalIgnoreCase)))
                    {
                        symptomScore = Math.Max(symptomScore, 0.9m);
                        triggeredFactors.Add($"عرض خطير: {symptom}");
                    }
                    else if (MediumRiskSymptoms.Any(m => symptom.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    {
                        symptomScore = Math.Max(symptomScore, 0.5m);
                        triggeredFactors.Add($"عرض: {symptom}");
                    }
                }
            }

            totalScore += symptomScore;
            factors = Math.Max(factors, 1); // avoid div by zero

            var finalScore = Math.Min(Math.Round(totalScore / factors, 2), 1.0m);
            bool isEmergency = finalScore >= 0.8m || oxygenSaturation < 90m || systolicBP > 180m;

            return (finalScore, isEmergency, triggeredFactors);
        }

        /// <summary>
        /// Maps a risk score to its corresponding <see cref="RiskLevel"/> enum value.
        /// </summary>
        public static RiskLevel GetRiskLevel(decimal score) => score switch
        {
            >= 0.8m => RiskLevel.Emergency,
            >= 0.5m => RiskLevel.High,
            >= 0.3m => RiskLevel.Medium,
            _       => RiskLevel.Low
        };

        /// <summary>
        /// Returns a localized label for the risk level.
        /// </summary>
        public static string GetRiskLabel(RiskLevel level) => level switch
        {
            RiskLevel.Emergency => "طارئ",
            RiskLevel.High      => "مرتفع",
            RiskLevel.Medium    => "متوسط",
            RiskLevel.Low       => "منخفض",
            _                   => "غير محدد"
        };

        /// <summary>
        /// Returns a CSS color class associated with the risk level.
        /// </summary>
        public static string GetRiskColor(RiskLevel level) => level switch
        {
            RiskLevel.Emergency => "danger",
            RiskLevel.High      => "orange",
            RiskLevel.Medium    => "warning",
            RiskLevel.Low       => "success",
            _                   => "secondary"
        };

        /// <summary>
        /// Generates clinical recommendations based on triggered factors and risk level.
        /// </summary>
        public static List<string> GenerateRecommendations(RiskLevel level, List<string> triggeredFactors)
        {
            var recs = new List<string>();

            switch (level)
            {
                case RiskLevel.Emergency:
                    recs.Add("🚨 اتصل بالإسعاف فوراً أو توجه لأقرب طوارئ");
                    recs.Add("لا تترك المريض وحده");
                    recs.Add("إذا كان واعياً، ساعده على الجلوس بوضع مريح");
                    break;

                case RiskLevel.High:
                    recs.Add("📞 تواصل مع طبيبك المعالج في أقرب وقت");
                    recs.Add("راقب الأعراض عن كثب وسجّلها");
                    recs.Add("تجنب المجهود الجسدي حتى المراجعة الطبية");
                    break;

                case RiskLevel.Medium:
                    recs.Add("📅 احجز موعداً مع الطبيب خلال يومين");
                    recs.Add("خذ قسطاً من الراحة وشرب كميات كافية من الماء");
                    recs.Add("راقب تطور الحالة وعُد للتقييم إذا ساءت");
                    break;

                case RiskLevel.Low:
                    recs.Add("✅ حالتك مستقرة في الوقت الحالي");
                    recs.Add("حافظ على نمط الحياة الصحي والرياضة المنتظمة");
                    recs.Add("قم بالفحوصات الدورية لمتابعة صحتك");
                    break;
            }

            return recs;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Scores a vital value: 0 = normal, 1 = critically abnormal.
        /// </summary>
        private static decimal ScoreVital(decimal value, decimal min, decimal max)
        {
            if (value >= min && value <= max) return 0m;

            var deviation = value < min
                ? (min - value) / min
                : (value - max) / max;

            return Math.Min(Math.Round(deviation, 2), 1.0m);
        }
    }
}
