using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Etmen_Domain.Entities;

namespace Etmen_DAL.Configurations
{
    public class EmergencyRequestConfig : IEntityTypeConfiguration<EmergencyRequest>
    {
        public void Configure(EntityTypeBuilder<EmergencyRequest> builder)
        {
            throw new NotImplementedException("Configure is not implemented yet.");
        }
    }
}