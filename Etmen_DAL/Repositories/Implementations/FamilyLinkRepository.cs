

namespace Etmen_DAL.Repositories.Implementations
{
    public class FamilyLinkRepository : GenericRepository<FamilyLink>, IFamilyLinkRepository
    {
        public FamilyLinkRepository(EtmenDbContext context) : base(context) { }

        public async Task<IEnumerable<FamilyLink>> GetByPrimaryPatientIdAsync(int id)
            => await _dbSet.Include(f => f.LinkedPatient).ThenInclude(p => p.ApplicationUser).Where(f => f.PrimaryPatientId == id).ToListAsync();

        public async Task<IEnumerable<FamilyLink>> GetByLinkedPatientIdAsync(int id)
            => await _dbSet.Include(f => f.PrimaryPatient).ThenInclude(p => p.ApplicationUser).Where(f => f.LinkedPatientId == id).ToListAsync();

        public async Task<IEnumerable<PatientProfile>> GetFamilyMembersAsync(int patientId)
        {
            var links = await _dbSet.Where(f => f.PrimaryPatientId == patientId || f.LinkedPatientId == patientId).ToListAsync();
            var ids = links.Select(f => f.PrimaryPatientId == patientId ? f.LinkedPatientId : f.PrimaryPatientId).ToList();
            return await _context.PatientProfiles.Include(p => p.ApplicationUser).Where(p => ids.Contains(p.Id)).ToListAsync();
        }

        public async Task<FamilyLink?> GetByInviteTokenAsync(string token)
            => await _dbSet.Include(f => f.PrimaryPatient).ThenInclude(p => p.ApplicationUser).FirstOrDefaultAsync(f => f.InviteToken == token && !f.IsAccepted);

        public async Task<bool> IsFamilyLinkExistsAsync(int p1, int p2)
            => await _dbSet.AnyAsync(f => (f.PrimaryPatientId == p1 && f.LinkedPatientId == p2) || (f.PrimaryPatientId == p2 && f.LinkedPatientId == p1));

        public async Task UpdatePermissionsAsync(int linkId, bool rec, bool risk, bool book)
        {
            var link = await _dbSet.FindAsync(linkId);
            if (link != null) { link.CanViewRecords = rec; link.CanViewRisk = risk; link.CanBookAppointments = book; _dbSet.Update(link); }
        }

        public async Task AcceptInviteAsync(string token, int linkedId)
        {
            var link = await _dbSet.FirstOrDefaultAsync(f => f.InviteToken == token);
            if (link != null) { link.IsAccepted = true; link.LinkedPatientId = linkedId; link.AcceptedAt = DateTime.UtcNow; _dbSet.Update(link); }
        }
    }
}