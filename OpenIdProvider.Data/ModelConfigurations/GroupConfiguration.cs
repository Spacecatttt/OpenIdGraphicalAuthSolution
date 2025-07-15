using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(256);
        builder.Property(g => g.Description).HasMaxLength(1024);

        // The one-to-many relationship with Organization is configured in OrganizationConfiguration.
        // The many-to-many with User is configured in ApplicationUserConfiguration.

        // Configure the one-to-many relationship with its claims
        builder.HasMany(g => g.Claims)
               .WithOne(c => c.Group)
               .HasForeignKey(c => c.GroupId)
               .OnDelete(DeleteBehavior.Cascade); // Deleting a group deletes its claims
    }
}