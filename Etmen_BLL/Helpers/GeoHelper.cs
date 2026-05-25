namespace Etmen_BLL.Helpers
{
    /// <summary>
    /// Geographic calculation utilities.
    /// Used by NearbyService and CrisisRiskEngineService.
    /// </summary>
    public static class GeoHelper
    {
        private const double EarthRadiusKm = 6371.0;

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
        /// (minLat, minLon) → (maxLat, maxLon).  Used for OutbreakZone checks.
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
