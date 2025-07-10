using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models; // Adjust namespace if different

namespace OpenIdProvider.Data.Configurations;

public class UserOrganizationRoleConfiguration : IEntityTypeConfiguration<UserOrganizationRole>
{
    public void Configure(EntityTypeBuilder<UserOrganizationRole> builder)
    {
        // Define the composite primary key
        builder.HasKey(uor => new { uor.UserId, uor.OrganizationId });

        // Configure the many-to-one relationship from UserOrganizationRole to ApplicationUser
        builder.HasOne(uor => uor.User)
               .WithMany(u => u.ManagedOrganizations)
               .HasForeignKey(uor => uor.UserId)
               .OnDelete(DeleteBehavior.Cascade); // If a User is deleted, their entries in UserOrganizationRole should also be deleted

        // Configure the many-to-one relationship from UserOrganizationRole to Organization
        builder.HasOne(uor => uor.Organization)
               .WithMany(o => o.ManagedByUsers)
               .HasForeignKey(uor => uor.OrganizationId)
               .OnDelete(DeleteBehavior.Cascade); // If an Organization is deleted, related UserOrganizationRole entries should also be deleted

        builder.Property(uor => uor.Role)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(uor => uor.AddedDate)
               .IsRequired();
    }
}