using Xunit;
using Etmen_BLL.Helpers;

namespace Etmen_Tests
{
    public class GeoHelperTests
    {
        [Fact]
        public void CalculateDistanceKm_SamePoint_ReturnsZero()
        {
            // Arrange
            double lat = 30.0444; // Cairo
            double lng = 31.2357;

            // Act
            double dist = GeoHelper.CalculateDistanceKm(lat, lng, lat, lng);

            // Assert
            Assert.Equal(0.0, dist, 2);
        }

        [Fact]
        public void CalculateDistanceKm_CairoToAlexandria_ReturnsCorrectDistance()
        {
            // Arrange
            double cairoLat = 30.0444;
            double cairoLng = 31.2357;
            double alexLat = 31.2001;
            double alexLng = 29.9187;

            // Act
            double dist = GeoHelper.CalculateDistanceKm(cairoLat, cairoLng, alexLat, alexLng);

            // Assert
            // The actual great circle distance is roughly 180 km
            Assert.True(dist > 175.0 && dist < 185.0, $"Calculated distance was {dist} km");
        }

        [Theory]
        [InlineData("شارع التسعين، التجمع، القاهرة الجديدة", "القاهرة")]
        [InlineData("ميدان المنشية، الاسكندرية", "الأسكندرية")]
        [InlineData("المنصورة، الدقهلية، مصر", "الدقهلية")]
        public void GetGovernorate_MatchingAddress_ResolvesCorrectGovernorate(string address, string expectedGov)
        {
            // Act
            string gov = GeoHelper.GetGovernorate(address, null, null, null);

            // Assert
            Assert.Equal(expectedGov, gov);
        }

        [Fact]
        public void GetGovernorate_CoordinatesOnly_ResolvesClosestGovernorate()
        {
            // Arrange
            // coordinates close to Alexandria
            decimal lat = 31.18m;
            decimal lng = 29.93m;

            // Act
            string gov = GeoHelper.GetGovernorate(null, null, lat, lng);

            // Assert
            Assert.Equal("الأسكندرية", gov);
        }

        [Fact]
        public void IsInsideBoundingBox_PointInside_ReturnsTrue()
        {
            // Act
            bool inside = GeoHelper.IsInsideBoundingBox(
                lat: 30.0, lon: 31.0,
                minLat: 29.0, minLon: 30.0,
                maxLat: 31.0, maxLon: 32.0
            );

            // Assert
            Assert.True(inside);
        }

        [Fact]
        public void IsInsideBoundingBox_PointOutside_ReturnsFalse()
        {
            // Act
            bool inside = GeoHelper.IsInsideBoundingBox(
                lat: 35.0, lon: 31.0,
                minLat: 29.0, minLon: 30.0,
                maxLat: 31.0, maxLon: 32.0
            );

            // Assert
            Assert.False(inside);
        }
    }
}
