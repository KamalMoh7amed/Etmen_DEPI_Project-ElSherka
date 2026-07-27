using Xunit;
using Etmen_BLL.Helpers;
using System.Collections.Generic;

namespace Etmen_Tests
{
    public class RiskCalculatorHelperTests
    {
        [Fact]
        public void CalculateNews2_NormalVitals_ReturnsLowRisk()
        {
            // Arrange
            decimal systolicBP = 115m;
            decimal heartRate = 72m;
            decimal temperature = 36.7m;
            decimal oxygenSaturation = 98m;

            // Act
            var (score, rating, ratingArabic, breakdown, recommendations) = 
                RiskCalculatorHelper.CalculateNews2(systolicBP, heartRate, temperature, oxygenSaturation);

            // Assert
            Assert.Equal(0, score);
            Assert.Equal("Low", rating);
            Assert.Equal("منخفض", ratingArabic);
            Assert.Contains("✅ مراقبة روتينية", recommendations[0]);
        }

        [Fact]
        public void CalculateNews2_SingleCriticalParameter_TriggersMediumRisk()
        {
            // Arrange
            // Systolic BP <= 90 scores 3, which is a single-parameter trigger for Medium risk
            decimal systolicBP = 85m; 
            decimal heartRate = 75m;
            decimal temperature = 36.5m;
            decimal oxygenSaturation = 98m;

            // Act
            var (score, rating, ratingArabic, breakdown, recommendations) = 
                RiskCalculatorHelper.CalculateNews2(systolicBP, heartRate, temperature, oxygenSaturation);

            // Assert
            Assert.True(score >= 3);
            Assert.Equal("Medium", rating);
            Assert.Equal("متوسط", ratingArabic);
            Assert.Contains("⚠️ تقييم عاجل من طبيب الطوارئ", recommendations[0]);
        }

        [Fact]
        public void CalculateNews2_MultipleAbnormalities_ReturnsHighRisk()
        {
            // Arrange
            decimal systolicBP = 80m;         // scores 3
            decimal heartRate = 135m;         // scores 3
            decimal temperature = 39.5m;      // scores 2
            decimal oxygenSaturation = 91m;   // scores 3

            // Act
            var (score, rating, ratingArabic, breakdown, recommendations) = 
                RiskCalculatorHelper.CalculateNews2(systolicBP, heartRate, temperature, oxygenSaturation);

            // Assert
            Assert.Equal(11, score);
            Assert.Equal("High", rating);
            Assert.Equal("مرتفع جداً", ratingArabic);
            Assert.Contains("🚨 استجابة سريرية فورية عاجلة", recommendations[0]);
        }

        [Fact]
        public void Calculate_NormalVitalsAndNoSymptoms_ReturnsLowScore()
        {
            // Act
            var (score, isEmergency, triggeredFactors) = RiskCalculatorHelper.Calculate(
                systolicBP: 120m,
                diastolicBP: 80m,
                heartRate: 70m,
                temperature: 36.6m,
                oxygenSaturation: 98m,
                bloodSugar: 100m,
                symptomsRaw: "لا توجد أعراض"
            );

            // Assert
            Assert.True(score < 0.25m);
            Assert.False(isEmergency);
        }

        [Fact]
        public void Calculate_SevereSymptomTrigger_SetsIsEmergencyTrue()
        {
            // Act
            var (score, isEmergency, triggeredFactors) = RiskCalculatorHelper.Calculate(
                systolicBP: 120m,
                diastolicBP: 80m,
                heartRate: 70m,
                temperature: 36.6m,
                oxygenSaturation: 98m,
                bloodSugar: 100m,
                symptomsRaw: "أعاني من ألم في الصدر وضيق تنفس شديد"
            );

            // Assert
            Assert.True(isEmergency);
            Assert.True(score >= 0.8m); // Emergency symptoms boost the score significantly
        }
    }
}
