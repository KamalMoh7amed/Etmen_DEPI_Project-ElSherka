using System.Collections.Generic;
using System.Threading.Tasks;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<IEnumerable<Review>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<Review>> GetByProviderIdAsync(int providerId);
        Task<double> GetAverageDoctorRatingAsync(int doctorId);
        Task<double> GetAverageProviderRatingAsync(int providerId);
    }
}
