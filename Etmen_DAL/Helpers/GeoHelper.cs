using System;
using System.Collections.Generic;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Helpers
{
    public static class GeoHelper
    {
        private const double EarthRadiusKm = 6371.0;

        /// <summary>
        /// حساب المسافة بين نقطتين باستخدام إحداثيات GPS (Haversine Formula)
        /// </summary>
        public static decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            var lat1Rad = ToRadians(lat1);
            var lon1Rad = ToRadians(lon1);
            var lat2Rad = ToRadians(lat2);
            var lon2Rad = ToRadians(lon2);

            var deltaLat = lat2Rad - lat1Rad;
            var deltaLon = lon2Rad - lon1Rad;

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = EarthRadiusKm * c;
            return (decimal)Math.Round(distance, 2);
        }

        public static bool IsPointInZone(decimal latitude, decimal longitude, OutbreakZone zone)
        {
            var distance = CalculateDistance(latitude, longitude, zone.CenterLatitude, zone.CenterLongitude);
            return distance <= zone.RadiusInKm;
        }

        /// <summary>
        /// التحقق إذا كانت النقطة داخل مضلع جغرافي (Polygon)
        /// </summary>
        public static bool IsPointInPolygon(decimal latitude, decimal longitude, List<(decimal Lat, decimal Lng)> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                if (((polygon[i].Lat > latitude) != (polygon[j].Lat > latitude)) &&
                    (longitude < (polygon[j].Lng - polygon[i].Lng) * (latitude - polygon[i].Lat) /
                    (polygon[j].Lat - polygon[i].Lat) + polygon[i].Lng))
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        /// <summary>
        /// البحث عن أقرب عنصر من قائمة بناءً على الإحداثيات
        /// </summary>
        public static T? FindNearest<T>(IEnumerable<T> items, decimal myLat, decimal myLon,
            Func<T, decimal> getLat, Func<T, decimal> getLon) where T : class
        {
            T? nearest = null;
            decimal minDistance = decimal.MaxValue;

            foreach (var item in items)
            {
                var distance = CalculateDistance(myLat, myLon, getLat(item), getLon(item));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = item;
                }
            }

            return nearest;
        }

        /// <summary>
        /// تحويل الدرجات إلى راديان
        /// </summary>
        private static double ToRadians(decimal degrees)
        {
            return (double)degrees * Math.PI / 180.0;
        }
    }
}