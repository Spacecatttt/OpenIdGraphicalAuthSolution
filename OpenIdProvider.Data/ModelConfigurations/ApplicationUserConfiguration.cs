using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Configure the one-to-many relationship with Organization
        builder.HasOne(u => u.Organization)
               .WithMany(o => o.Users)
               .HasForeignKey(u => u.OrganizationId)
               .OnDelete(DeleteBehavior.Cascade); // Or Restrict

        // Note: The many-to-many relationship with Group is configured
        // in the UserGroupConfiguration file.
    }
}
