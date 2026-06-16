using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Etmen_DAL.DbContext;
using Etmen_DAL.Repositories.Interfaces;
using Etmen_Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Etmen_DAL.Repositories.Implementations
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<Review>> GetByDoctorIdAsync(int doctorId)
            => await _dbSet.Include(r => r.PatientProfile)
                           .Where(r => r.DoctorProfileId == doctorId)
                           .OrderByDescending(r => r.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Review>> GetByProviderIdAsync(int providerId)
            => await _dbSet.Include(r => r.PatientProfile)
                           .Where(r => r.HealthcareProviderId == providerId)
                           .OrderByDescending(r => r.CreatedAt).ToListAsync();

        public async Task<double> GetAverageDoctorRatingAsync(int doctorId)
        {
            var ratings = await _dbSet.Where(r => r.DoctorProfileId == doctorId).Select(r => (double)r.Rating).ToListAsync();
            return ratings.Any() ? ratings.Average() : 0.0;
        }

        public async Task<double> GetAverageProviderRatingAsync(int providerId)
        {
            var ratings = await _dbSet.Where(r => r.HealthcareProviderId == providerId).Select(r => (double)r.Rating).ToListAsync();
            return ratings.Any() ? ratings.Average() : 0.0;
        }
    }
}
