using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class HealthcareProviderConfig : IEntityTypeConfiguration<HealthcareProvider>
    {
        public void Configure(EntityTypeBuilder<HealthcareProvider> builder)
        {
            throw new NotImplementedException("Configure is not implemented yet.");
        }
    }
}