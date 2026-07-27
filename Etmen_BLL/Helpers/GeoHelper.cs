using System;

namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Geographic calculation utilities and Egyptian governorates dynamic lookup.
    /// Used by NearbyService, CrisisRiskEngineService, and AdminDashboardController.
    /// </summary>
    public static class GeoHelper
    {
        private const double EarthRadiusKm = 6371.0;

        // Standardized list of all 27 Egyptian Governorates with coordinates
        public static readonly (string Name, double Lat, double Lng)[] GovernorateCenters = new[]
        {
            ("القاهرة", 30.0444, 31.2357),
            ("الجيزة", 30.0131, 31.2089),
            ("الأسكندرية", 31.2001, 29.9187),
            ("الشرقية", 30.5877, 31.5020),
            ("الدقهلية", 31.0409, 31.3785),
            ("البحيرة", 31.0423, 30.4687),
            ("المنوفية", 30.5615, 31.0116),
            ("القليوبية", 30.4591, 31.1856),
            ("الغربية", 30.7865, 31.0004),
            ("الفيوم", 29.3084, 30.8428),
            ("قنا", 26.1551, 32.7160),
            ("سوهاج", 26.5570, 31.6948),
            ("المنيا", 28.0980, 30.7516),
            ("أسيوط", 27.1810, 31.1837),
            ("بني سويف", 29.0744, 31.0979),
            ("دمياط", 31.4175, 31.8144),
            ("أسوان", 24.0889, 32.8998),
            ("الأقصر", 25.6872, 32.6396),
            ("بورسعيد", 31.2653, 32.3019),
            ("السويس", 29.9668, 32.5498),
            ("الإسماعيلية", 30.6043, 32.2723),
            ("البحر الأحمر", 27.2579, 33.8116),
            ("الوادي الجديد", 25.4390, 30.5598),
            ("مطروح", 31.3543, 27.2373),
            ("شمال سيناء", 31.1325, 33.8033),
            ("جنوب سيناء", 28.2435, 33.6214),
            ("كفر الشيخ", 31.1107, 30.9388)
        };

        /// <summary>
        /// Resolves the Egyptian governorate name based on address, description, or coordinates.
        /// </summary>
        public static string GetGovernorate(string? address, string? description, decimal? latitude, decimal? longitude)
        {
            // 1. Check address for matching governorate names using Arabic normalization
            if (!string.IsNullOrWhiteSpace(address))
            {
                var normalizedAddress = NormalizeArabic(address);
                foreach (var center in GovernorateCenters)
                {
                    if (normalizedAddress.Contains(NormalizeArabic(center.Name)))
                    {
                        return center.Name;
                    }
                }
            }

            // 2. Check description for matching governorate names using Arabic normalization
            if (!string.IsNullOrWhiteSpace(description))
            {
                var normalizedDesc = NormalizeArabic(description);
                foreach (var center in GovernorateCenters)
                {
                    if (normalizedDesc.Contains(NormalizeArabic(center.Name)))
                    {
                        return center.Name;
                    }
                }
            }

            // 3. Fallback to closest coordinates from the 27 governorate centers
            if (latitude.HasValue && longitude.HasValue && latitude.Value != 0 && longitude.Value != 0)
            {
                double minDist = double.MaxValue;
                string closest = "القاهرة";

                foreach (var center in GovernorateCenters)
                {
                    double dist = CalculateDistanceKm(
                        (double)latitude.Value, (double)longitude.Value,
                        center.Lat, center.Lng);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = center.Name;
                    }
                }

                return closest;
            }

            return "القاهرة"; // default fallback
        }

        /// <summary>
        /// Normalizes Arabic text to ignore common spelling differences (hamzas, taa marbouta, etc.)
        /// </summary>
        private static string NormalizeArabic(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == 'أ' || chars[i] == 'إ' || chars[i] == 'آ')
                    chars[i] = 'ا';
                else if (chars[i] == 'ة')
                    chars[i] = 'ه';
                else if (chars[i] == 'ى')
                    chars[i] = 'ي';
            }
            return new string(chars);
        }

        /// <summary>
        /// Calculates the great-circle distance (km) between two coordinate pairs
        /// using the Haversine formula.
        /// </summary>
        public static double CalculateDistanceKm(
            double lat1, double lon1,
            double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        /// <summary>
        /// Returns true when (lat, lon) falls inside the bounding box defined by
        /// (minLat, minLon) → (maxLat, maxLon). Used for OutbreakZone checks.
        /// </summary>
        public static bool IsInsideBoundingBox(
            double lat, double lon,
            double minLat, double minLon,
            double maxLat, double maxLon)
            => lat >= minLat && lat <= maxLat
            && lon >= minLon && lon <= maxLon;

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
    }
}

