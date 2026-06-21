
namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IEmergencyRequestRepository : IGenericRepository<EmergencyRequest>
    {
        Task<IEnumerable<EmergencyRequest>> GetByPatientIdAsync(int patientId);
        Task<IEnumerable<EmergencyRequest>> GetByProviderIdAsync(int providerId);
        Task<IEnumerable<EmergencyRequest>> GetPendingRequestsAsync();
        Task<IEnumerable<EmergencyRequest>> GetByStatusAsync(EmergencyRequestStatus status);
        Task<EmergencyRequest?> GetWithTrackingInfoAsync(int requestId);
        Task AcceptRequestAsync(int requestId, int providerId);
        Task RejectRequestAsync(int requestId, string reason);
        Task CompleteRequestAsync(int requestId, string notes);
        Task<IEnumerable<EmergencyRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetPendingCountAsync();
    }
}