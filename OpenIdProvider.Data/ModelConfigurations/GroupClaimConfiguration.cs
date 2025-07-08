using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class GroupClaimConfiguration : IEntityTypeConfiguration<GroupClaim>
{
    public void Configure(EntityTypeBuilder<GroupClaim> builder)
    {
        builder.HasKey(gc => gc.Id);

        builder.Property(gc => gc.Type).IsRequired();
        builder.Property(gc => gc.Value).IsRequired();

        // Note: The relationship with Group is configured in the GroupConfiguration file.
    }
}