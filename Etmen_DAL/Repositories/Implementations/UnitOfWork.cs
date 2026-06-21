using Microsoft.EntityFrameworkCore.Storage;

namespace Etmen_DAL.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EtmenDbContext _context;
        private IDbContextTransaction? _transaction;

        private IGenericRepository<ApplicationUser>? _users;
        private IPatientProfileRepository? _patientProfiles;
        private IDoctorProfileRepository? _doctorProfiles;
        private IMedicalRecordRepository? _medicalRecords;
        private IRiskAssessmentRepository? _riskAssessments;
        private IHealthcareProviderRepository? _healthcareProviders;
        private IAppointmentRepository? _appointments;
        private IAvailableSlotRepository? _availableSlots;
        private ILabResultRepository? _labResults;
        private IFamilyLinkRepository? _familyLinks;
        private IChatMessageRepository? _chatMessages;
        private IAlertRepository? _alerts;
        private INotificationRepository? _notifications;
        private ICrisisConfigurationRepository? _crisisConfigurations;
        private IOutbreakZoneRepository? _outbreakZones;
        private IEmergencyRequestRepository? _emergencyRequests;
        private IGenericRepository<StaffProfile>? _staffProfiles;
        private IGenericRepository<StaffActivityLog>? _staffActivityLogs;
        private IDoctorProviderRepository? _doctorProviders;
        private IReviewRepository? _reviews;

        public UnitOfWork(EtmenDbContext context) => _context = context;

        public IGenericRepository<ApplicationUser> Users => _users ??= new GenericRepository<ApplicationUser>(_context);
        public IPatientProfileRepository PatientProfiles => _patientProfiles ??= new PatientProfileRepository(_context);
        public IDoctorProfileRepository DoctorProfiles => _doctorProfiles ??= new DoctorProfileRepository(_context);
        public IMedicalRecordRepository MedicalRecords => _medicalRecords ??= new MedicalRecordRepository(_context);
        public IRiskAssessmentRepository RiskAssessments => _riskAssessments ??= new RiskAssessmentRepository(_context);
        public IHealthcareProviderRepository HealthcareProviders => _healthcareProviders ??= new HealthcareProviderRepository(_context);
        public IAppointmentRepository Appointments => _appointments ??= new AppointmentRepository(_context);
        public IAvailableSlotRepository AvailableSlots => _availableSlots ??= new AvailableSlotRepository(_context);
        public ILabResultRepository LabResults => _labResults ??= new LabResultRepository(_context);
        public IFamilyLinkRepository FamilyLinks => _familyLinks ??= new FamilyLinkRepository(_context);
        public IChatMessageRepository ChatMessages => _chatMessages ??= new ChatMessageRepository(_context);
        public IAlertRepository Alerts => _alerts ??= new AlertRepository(_context);
        public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
        public ICrisisConfigurationRepository CrisisConfigurations => _crisisConfigurations ??= new CrisisConfigurationRepository(_context);
        public IOutbreakZoneRepository OutbreakZones => _outbreakZones ??= new OutbreakZoneRepository(_context);
        public IEmergencyRequestRepository EmergencyRequests => _emergencyRequests ??= new EmergencyRequestRepository(_context);
        public IGenericRepository<StaffProfile> StaffProfiles => _staffProfiles ??= new GenericRepository<StaffProfile>(_context);
        public IGenericRepository<StaffActivityLog> StaffActivityLogs => _staffActivityLogs ??= new GenericRepository<StaffActivityLog>(_context);
        public IDoctorProviderRepository DoctorProviders => _doctorProviders ??= new DoctorProviderRepository(_context);
        public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
        public async Task BeginTransactionAsync() => _transaction = await _context.Database.BeginTransactionAsync();
        public async Task CommitTransactionAsync()
        {
            if (_transaction != null) { await _transaction.CommitAsync(); await _transaction.DisposeAsync(); _transaction = null; }
        }
        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null) { await _transaction.RollbackAsync(); await _transaction.DisposeAsync(); _transaction = null; }
        }
        public void Dispose() => _context.Dispose();
    }
}