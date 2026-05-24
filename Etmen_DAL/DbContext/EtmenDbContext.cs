using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Etmen_Domain.Entities;
using System.Reflection;

namespace Etmen_DAL.DbContext
{
    public class EtmenDbContext : IdentityDbContext<ApplicationUser>
    {
        public EtmenDbContext(DbContextOptions<EtmenDbContext> options)
            : base(options) { }

        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        public DbSet<HealthcareProvider> HealthcareProviders { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AvailableSlot> AvailableSlots { get; set; }
        public DbSet<LabResult> LabResults { get; set; }
        public DbSet<FamilyLink> FamilyLinks { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CrisisConfiguration> CrisisConfigurations { get; set; }
        public DbSet<OutbreakZone> OutbreakZones { get; set; }
        public DbSet<EmergencyRequest> EmergencyRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}