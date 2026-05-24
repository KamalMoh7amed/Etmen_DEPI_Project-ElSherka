using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class CrisisConfigurationConfig : IEntityTypeConfiguration<CrisisConfiguration>
    {
        public void Configure(EntityTypeBuilder<CrisisConfiguration> builder)
        {
            throw new NotImplementedException("Configure is not implemented yet.");
        }
    }
}