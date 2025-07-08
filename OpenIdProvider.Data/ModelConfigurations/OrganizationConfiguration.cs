using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        // Ensure the Slug is unique across all organizations
        builder.HasIndex(o => o.Slug).IsUnique();

        builder.Property(o => o.Name).IsRequired().HasMaxLength(256);
        builder.Property(o => o.Slug).IsRequired().HasMaxLength(100);

        // Configure the one-to-many relationship with its own Groups
        builder.HasMany(o => o.Groups)
               .WithOne(g => g.Organization)
               .HasForeignKey(g => g.OrganizationId)
               .OnDelete(DeleteBehavior.Cascade); // Deleting an org deletes its groups
    }
}