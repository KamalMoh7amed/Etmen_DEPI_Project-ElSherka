using Etmen_Domain.Entities;

namespace Etmen_DAL.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<ApplicationUser> Users { get; }
        IPatientProfileRepository PatientProfiles { get; }
        IDoctorProfileRepository DoctorProfiles { get; }
        IMedicalRecordRepository MedicalRecords { get; }
        IRiskAssessmentRepository RiskAssessments { get; }
        IHealthcareProviderRepository HealthcareProviders { get; }
        IAppointmentRepository Appointments { get; }
        IAvailableSlotRepository AvailableSlots { get; }
        ILabResultRepository LabResults { get; }
        IFamilyLinkRepository FamilyLinks { get; }
        IChatMessageRepository ChatMessages { get; }
        IAlertRepository Alerts { get; }
        INotificationRepository Notifications { get; }
        ICrisisConfigurationRepository CrisisConfigurations { get; }
        IOutbreakZoneRepository OutbreakZones { get; }
        IEmergencyRequestRepository EmergencyRequests { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}