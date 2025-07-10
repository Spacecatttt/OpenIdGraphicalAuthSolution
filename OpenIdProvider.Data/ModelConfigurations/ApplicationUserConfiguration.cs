using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {

        builder.Property(g => g.DisplayName).IsRequired();
        // Configure the one-to-many relationship with Organization
        builder.HasOne(u => u.PrimaryOrganization)
               .WithMany(o => o.PrimaryUsers)
               .HasForeignKey(u => u.PrimaryOrganizationId)
               .OnDelete(DeleteBehavior.Cascade);

        // Note: The many-to-many relationship with Group is configured
        // in the UserGroupConfiguration file.
    }
}
