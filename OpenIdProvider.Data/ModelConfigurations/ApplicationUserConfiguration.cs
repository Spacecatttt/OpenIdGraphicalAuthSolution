using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName).IsRequired();

        builder.HasOne(u => u.PrimaryOrganization)
               .WithMany(o => o.PrimaryUsers)
               .HasForeignKey(u => u.PrimaryOrganizationId)
               .OnDelete(DeleteBehavior.Cascade);

        // many-to-many relationship with Group
        builder.HasMany(u => u.Groups)
               .WithMany(g => g.Users)
               .UsingEntity(j => j.ToTable("UserGroups"));
    }
}
