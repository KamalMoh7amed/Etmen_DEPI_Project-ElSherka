using Etmen_BLL.DTOs.Nearby;

namespace Etmen_PL.Models.ViewModels.Patient
{
    public class NearbySearchViewModel
    {
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Type { get; set; } = "Hospital";
        public int RadiusInKm { get; set; } = 5;
        public bool ShowAll { get; set; } = false;

        public List<ProviderDto> SearchResults { get; set; } = new();
    }
}
