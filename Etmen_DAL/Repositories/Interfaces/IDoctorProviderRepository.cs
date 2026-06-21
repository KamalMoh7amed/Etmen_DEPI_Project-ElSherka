
namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IDoctorProviderRepository : IGenericRepository<DoctorProvider>
    {
        Task<IEnumerable<DoctorProvider>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<DoctorProvider>> GetByProviderIdAsync(int providerId);
        Task<DoctorProvider?> GetAffiliationAsync(int doctorId, int providerId);
    }
}
