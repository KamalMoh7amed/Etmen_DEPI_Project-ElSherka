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
        // ── Clinical severity thresholds (based on AHA/WHO guidelines) ────────────
        // Systolic BP tiers - AHA guidelines (mmHg)
        private const decimal SBP_CRITICAL_LOW    = 70m;
        private const decimal SBP_MODERATE_LOW    = 80m;
        private const decimal SBP_MILD_LOW        = 89m;
        private const decimal SBP_NORMAL          = 120m;   // <120 normal
        private const decimal SBP_ELEVATED        = 130m;   // 120-129 elevated
        private const decimal SBP_STAGE1          = 140m;   // 130-139 Stage 1 HTN
        private const decimal SBP_STAGE2          = 180m;   // 140-179 Stage 2 HTN
                                                            // >=180 crisis

        // Diastolic BP tiers - AHA guidelines (mmHg)
        private const decimal DBP_CRITICAL_LOW    = 40m;
        private const decimal DBP_MODERATE_LOW    = 50m;
        private const decimal DBP_MILD_LOW        = 59m;
        private const decimal DBP_NORMAL          = 80m;    // <80 normal
        private const decimal DBP_ELEVATED        = 90m;    // 80-89 elevated
        private const decimal DBP_STAGE2          = 120m;   // 90-119 Stage 2 HTN
                                                            // >=120 crisis

        // Heart rate tiers (bpm)
        private const decimal HR_CRITICAL_LOW     = 40m;
        private const decimal HR_MODERATE_LOW     = 49m;
        private const decimal HR_MILD_LOW         = 59m;
        private const decimal HR_NORMAL_MIN       = 60m;
        private const decimal HR_NORMAL_MAX       = 100m;
        private const decimal HR_MILD_HIGH        = 120m;
        private const decimal HR_MODERATE_HIGH    = 140m;

        // Temperature tiers (°C)
        private const decimal TEMP_CRITICAL_LOW   = 35.0m;
        private const decimal TEMP_MODERATE_LOW   = 35.5m;
        private const decimal TEMP_NORMAL_MIN     = 36.0m;
        private const decimal TEMP_NORMAL_MAX     = 37.5m;
        private const decimal TEMP_MILD_FEVER     = 38.5m;
        private const decimal TEMP_MODERATE_FEVER = 39.5m;

        // Oxygen saturation tiers (%)
        private const decimal SPO2_NORMAL_MIN     = 95m;
        private const decimal SPO2_MILD           = 90m;
        private const decimal SPO2_MODERATE       = 85m;

        // Blood sugar tiers (mg/dL)
        private const decimal BS_CRITICAL_LOW     = 50m;
        private const decimal BS_MODERATE_LOW     = 69m;
        private const decimal BS_NORMAL_MIN       = 70m;
        private const decimal BS_NORMAL_MAX       = 180m;
        private const decimal BS_MILD_HIGH        = 220m;
        private const decimal BS_MODERATE_HIGH    = 300m;
        private const decimal BS_SEVERE_HIGH      = 400m;

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
        /// Uses tier-based clinical scoring (AHA/WHO guidelines) with max-weighted aggregation
        /// so that a single severe abnormality elevates the overall score appropriately.
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
            var scores = new List<decimal>();

            // ── Vital scoring (tier-based) ─────────────────────────────────────────
            if (systolicBP.HasValue)
            {
                var score = ScoreSystolicBP(systolicBP.Value);
                scores.Add(score);
                var status = GetStatus(score);
                triggeredFactors.Add($"ضغط الدم الانقباضي: {systolicBP} mmHg{status}");
            }

            if (diastolicBP.HasValue)
            {
                var score = ScoreDiastolicBP(diastolicBP.Value);
                scores.Add(score);
                var status = GetStatus(score);
                triggeredFactors.Add($"ضغط الدم الانبساطي: {diastolicBP} mmHg{status}");
            }

            if (heartRate.HasValue)
            {
                var score = ScoreHeartRate(heartRate.Value);
                scores.Add(score);
                var status = GetStatus(score);
                triggeredFactors.Add($"معدل ضربات القلب: {heartRate} bpm{status}");
            }

            if (temperature.HasValue)
            {
                var score = ScoreTemperature(temperature.Value);
                scores.Add(score);
                var status = GetStatus(score);
                triggeredFactors.Add($"درجة الحرارة: {temperature}°C{status}");
            }

            if (oxygenSaturation.HasValue)
            {
                var score = ScoreOxygen(oxygenSaturation.Value);
                scores.Add(score);
                var status = GetOxygenStatus(score);
                triggeredFactors.Add($"تشبع الأكسجين: {oxygenSaturation}%{status}");
            }

            if (bloodSugar.HasValue)
            {
                var score = ScoreBloodSugar(bloodSugar.Value);
                scores.Add(score);
                var status = GetStatus(score);
                triggeredFactors.Add($"مستوى السكر في الدم: {bloodSugar} mg/dL{status}");
            }

            // ── Aggregate vital scores ─────────────────────────────────────────────
            // Weighted: 60% max severity + 40% average across all vitals
            // This ensures a single severe finding elevates the score while
            // multiple moderate findings also contribute appropriately.
            decimal vitalScore = 0;
            if (scores.Count > 0)
            {
                var maxScore = scores.Max();
                var avgScore = scores.Average();
                vitalScore = Math.Min(Math.Round((0.6m * maxScore) + (0.4m * avgScore), 2), 1.0m);
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
                    else
                    {
                        triggeredFactors.Add($"عرض: {symptom}");
                    }
                }
            }

            // ── Final score ───────────────────────────────────────────────────────
            // Take the max between vital and symptom scores
            var finalScore = Math.Min(Math.Round(Math.Max(vitalScore, symptomScore), 2), 1.0m);
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
    public static List<string> GenerateRecommendations(RiskLevel level, List<string> triggeredFactors, bool isCrisisMode = false)
    {
        var recs = new List<string>();
        bool isCrisis = isCrisisMode || triggeredFactors.Any(f => f.Contains("عرض الأزمة") || f.Contains("الأزمة"));

        switch (level)
        {
            case RiskLevel.Emergency:
                recs.Add("🚨 اتصل بالإسعاف فوراً أو توجه لأقرب طوارئ");
                if (isCrisis)
                {
                    recs.Add("😷 يرجى عزل المريض فوراً في غرفة منفصلة جيدة التهوية لمنع انتشار العدوى");
                    recs.Add("🛡️ يجب على المريض والمخالطين ارتداء كمامات عالية الكفاءة (N95) والقفازات الواقية");
                    recs.Add("📏 حافظ على مسافة أمان لا تقل عن مترين وتجنب الملامسة المباشرة لمتعلقات المريض");
                    recs.Add("🧼 تطهير الأسطح المشتركة باستمرار بالكلور أو الكحول وتعقيم الأيدي");
                }
                else
                {
                    recs.Add("لا تترك المريض وحده");
                    recs.Add("إذا كان واعياً، ساعده على الجلوس بوضع مريح");
                }
                break;

            case RiskLevel.High:
                recs.Add("📞 تواصل مع طبيبك المعالج في أقرب وقت");
                if (isCrisis)
                {
                    recs.Add("😷 التزم بالعزل المنزلي الوقائي وارتداء الكمامة باستمرار لمنع نقل العدوى للمحيطين");
                    recs.Add("🧼 واظب على غسل اليدين وتطهير أدواتك الشخصية بشكل منفصل");
                }
                else
                {
                    recs.Add("راقب الأعراض عن كثب وسجّلها");
                    recs.Add("تجنب المجهود الجسدي حتى المراجعة الطبية");
                }
                break;

            case RiskLevel.Medium:
                recs.Add("📅 احجز موعداً مع الطبيب خلال يومين");
                if (isCrisis)
                {
                    recs.Add("😷 التزم بارتداء الكمامة في المنزل وعقم يديك بانتظام");
                    recs.Add("🏠 تجنب الخروج أو زيارة التجمعات لحين التأكد من استقرار حالتك");
                }
                else
                {
                    recs.Add("خذ قسطاً من الراحة وشرب كميات كافية من الماء");
                    recs.Add("راقب تطور الحالة وعُد للتقييم إذا ساءت");
                }
                break;

            case RiskLevel.Low:
                recs.Add("✅ حالتك مستقرة في الوقت الحالي");
                if (isCrisis)
                {
                    recs.Add("🧼 استمر باتباع التدابير الوقائية والنظافة الشخصية العامة");
                    recs.Add("😷 تجنب المخالطة اللصيقة بحالات مشتبه بإصابتها بالوباء");
                }
                else
                {
                    recs.Add("حافظ على نمط الحياة الصحي والرياضة المنتظمة");
                    recs.Add("قم بالفحوصات الدورية لمتابعة صحتك");
                }
                break;
        }
        return recs;
    }
        private static string GetStatus(decimal score) => score switch
        {
            >= 0.7m => " (شديد)",
            >= 0.4m => " (غير طبيعي)",
            >= 0.2m => " (مرتفع قليلاً)",
            _       => ""
        };

        /// <summary>Returns an oxygen-specific severity label suffix.</summary>
        private static string GetOxygenStatus(decimal score) => score switch
        {
            >= 0.7m => " (نقص أكسجة حاد)",
            >= 0.4m => " (نقص أكسجة)",
            >= 0.2m => " (منخفض قليلاً)",
            _       => ""
        };

        /// <summary>
        /// Clinically-accurate systolic BP scoring based on AHA hypertension stages.
        /// Normal: <120, Elevated: 120-129, Stage 1: 130-139, Stage 2: 140-179, Crisis: >=180
        /// </summary>
        private static decimal ScoreSystolicBP(decimal value) => value switch
        {
            < SBP_CRITICAL_LOW   => 1.0m,  // < 70:  critical hypotension
            < SBP_MODERATE_LOW   => 0.7m,  // 70-79: moderate hypotension
            < SBP_MILD_LOW       => 0.4m,  // 80-89: mild hypotension
            < SBP_NORMAL         => 0m,    // 90-119: normal
            < SBP_ELEVATED       => 0.2m,  // 120-129: elevated
            < SBP_STAGE1         => 0.4m,  // 130-139: Stage 1 HTN
            < SBP_STAGE2         => 0.6m,  // 140-179: Stage 2 HTN
            _                    => 0.9m   // >= 180: hypertensive crisis
        };

        /// <summary>
        /// Diastolic BP scoring based on AHA hypertension stages.
        /// Normal: <80, Elevated: 80-89, Stage 2: 90-119, Crisis: >=120
        /// </summary>
        private static decimal ScoreDiastolicBP(decimal value) => value switch
        {
            < DBP_CRITICAL_LOW   => 1.0m,  // < 40:  critical
            < DBP_MODERATE_LOW   => 0.7m,  // 40-49: moderate low
            < DBP_MILD_LOW       => 0.4m,  // 50-59: mild low
            < DBP_NORMAL         => 0m,    // 60-79: normal
            < DBP_ELEVATED       => 0.2m,  // 80-89: elevated
            < DBP_STAGE2         => 0.5m,  // 90-119: Stage 2 HTN
            _                    => 0.9m   // >= 120: critical
        };

        /// <summary>
        /// Heart rate scoring: tachycardia and bradycardia tiers.
        /// </summary>
        private static decimal ScoreHeartRate(decimal value) => value switch
        {
            < HR_CRITICAL_LOW    => 1.0m,  // < 40:  severe bradycardia
            < HR_MODERATE_LOW    => 0.7m,  // 40-49: moderate bradycardia
            < HR_MILD_LOW        => 0.4m,  // 50-59: mild bradycardia
            <= HR_NORMAL_MAX     => 0m,    // 60-100: normal
            <= HR_MILD_HIGH      => 0.5m,  // 101-120: mild tachycardia
            <= HR_MODERATE_HIGH  => 0.7m,  // 121-140: moderate tachycardia
            _                    => 1.0m   // > 140: severe tachycardia
        };

        /// <summary>
        /// Temperature scoring based on fever and hypothermia severity.
        /// </summary>
        private static decimal ScoreTemperature(decimal value) => value switch
        {
            < TEMP_CRITICAL_LOW  => 1.0m,  // < 35:  severe hypothermia
            < TEMP_MODERATE_LOW  => 0.6m,  // 35.0-35.5: moderate hypothermia
            < TEMP_NORMAL_MIN    => 0.3m,  // 35.6-35.9: mild hypothermia
            <= TEMP_NORMAL_MAX   => 0m,    // 36.0-37.5: normal
            <= TEMP_MILD_FEVER   => 0.4m,  // 37.6-38.5: mild fever
            <= TEMP_MODERATE_FEVER => 0.7m, // 38.6-39.5: moderate fever
            _                    => 0.9m   // > 39.5: high fever
        };

        /// <summary>
        /// Oxygen saturation scoring for hypoxemia severity.
        /// </summary>
        private static decimal ScoreOxygen(decimal value) => value switch
        {
            >= SPO2_NORMAL_MIN   => 0m,    // >= 95%: normal
            >= SPO2_MILD         => 0.5m,  // 90-94%: mild hypoxemia
            >= SPO2_MODERATE     => 0.8m,  // 85-89%: moderate hypoxemia
            _                    => 1.0m   // < 85%: severe hypoxemia
        };

        /// <summary>
        /// Blood sugar scoring for hypo/hyperglycemia severity.
        /// </summary>
        private static decimal ScoreBloodSugar(decimal value) => value switch
        {
            < BS_CRITICAL_LOW    => 1.0m,  // < 50:  severe hypoglycemia
            < BS_MODERATE_LOW    => 0.6m,  // 50-69: moderate hypoglycemia
            < BS_NORMAL_MIN      => 0.3m,  // 70-79: mild hypoglycemia
            <= BS_NORMAL_MAX     => 0m,    // 70-180: normal
            <= BS_MILD_HIGH      => 0.5m,  // 181-220: mild hyperglycemia
            <= BS_MODERATE_HIGH  => 0.65m, // 221-300: moderate hyperglycemia
            <= BS_SEVERE_HIGH    => 0.8m,  // 301-400: severe hyperglycemia
            _                    => 1.0m   // > 400: critical hyperglycemia
        };

        public static (int Score, string Rating, string RatingArabic, List<string> Breakdown, List<string> Recommendations) CalculateNews2(
            decimal? systolicBP,
            decimal? heartRate,
            decimal? temperature,
            decimal? oxygenSaturation)
        {
            if (!systolicBP.HasValue && !heartRate.HasValue && !temperature.HasValue && !oxygenSaturation.HasValue)
            {
                return (-1, "NotAvailable", "غير متوفر لعدم إدخال مؤشرات حيوية", new List<string>(), new List<string>());
            }

            var breakdown = new List<string>();
            var recs = new List<string>();
            int totalScore = 0;
            bool hasSingleThree = false;

            // 1. Oxygen Saturation (SpO2)
            if (oxygenSaturation.HasValue)
            {
                int score = 0;
                decimal val = oxygenSaturation.Value;
                if (val >= 96) score = 0;
                else if (val >= 94) score = 1;
                else if (val >= 92) score = 2;
                else score = 3;

                totalScore += score;
                if (score == 3) hasSingleThree = true;
                breakdown.Add($"تشبع الأكسجين ({val}%): +{score} نقاط");
            }

            // 2. Systolic BP
            if (systolicBP.HasValue)
            {
                int score = 0;
                decimal val = systolicBP.Value;
                if (val <= 90 || val >= 220) score = 3;
                else if (val >= 91 && val <= 100) score = 2;
                else if (val >= 101 && val <= 110) score = 1;
                else score = 0;

                totalScore += score;
                if (score == 3) hasSingleThree = true;
                breakdown.Add($"ضغط الدم الانقباضي ({val} mmHg): +{score} نقاط");
            }

            // 3. Heart Rate
            if (heartRate.HasValue)
            {
                int score = 0;
                decimal val = heartRate.Value;
                if (val <= 40 || val >= 131) score = 3;
                else if (val >= 111 && val <= 130) score = 2;
                else if (val >= 41 && val <= 50) score = 1;
                else if (val >= 91 && val <= 110) score = 1;
                else score = 0;

                totalScore += score;
                if (score == 3) hasSingleThree = true;
                breakdown.Add($"معدل ضربات القلب ({val} bpm): +{score} نقاط");
            }

            // 4. Temperature
            if (temperature.HasValue)
            {
                int score = 0;
                decimal val = temperature.Value;
                if (val <= 35.0m) score = 3;
                else if (val >= 39.1m) score = 2;
                else if (val >= 35.1m && val <= 36.0m) score = 1;
                else if (val >= 38.1m && val <= 39.0m) score = 1;
                else score = 0;

                totalScore += score;
                if (score == 3) hasSingleThree = true;
                breakdown.Add($"درجة الحرارة ({val}°C): +{score} نقاط");
            }

            // Categorize risk level
            string rating = "Low";
            string ratingArabic = "منخفض";

            if (totalScore >= 7)
            {
                rating = "High";
                ratingArabic = "مرتفع جداً";
                recs.Add("🚨 استجابة سريرية فورية عاجلة: يجب استدعاء فريق الطوارئ الطبي المتقدم فوراً.");
                recs.Add("مراقبة مستمرة للمؤشرات الحيوية ودعم مجرى الهواء والأكسجين.");
            }
            else if (totalScore >= 5 || hasSingleThree)
            {
                rating = "Medium";
                ratingArabic = "متوسط";
                recs.Add("⚠️ تقييم عاجل من طبيب الطوارئ: يجب إعلام الطبيب المسؤول وتكثيف المراقبة لتكون كل ساعة.");
                recs.Add("مراقبة تطور الأعراض بدقة والتجهيز لاحتمالية النقل.");
            }
            else
            {
                rating = "Low";
                ratingArabic = "منخفض";
                recs.Add("✅ مراقبة روتينية: استمر في متابعة العلامات الحيوية بشكل دوري كل 4 إلى 6 ساعات.");
            }

            return (totalScore, rating, ratingArabic, breakdown, recs);
        }
    }
}
