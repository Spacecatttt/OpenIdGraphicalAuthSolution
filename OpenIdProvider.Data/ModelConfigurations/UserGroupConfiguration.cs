using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        // Define the composite primary key
        builder.HasKey(ug => new { ug.ApplicationUserId, ug.GroupId });

        // Configure the many-to-one relationship with ApplicationUser
        builder.HasOne(ug => ug.ApplicationUser)
               .WithMany(u => u.Groups)
               .HasForeignKey(ug => ug.ApplicationUserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Configure the many-to-one relationship with Group
        builder.HasOne(ug => ug.Group)
               .WithMany(g => g.Users)
               .HasForeignKey(ug => ug.GroupId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ug => ug.AssignedDate).IsRequired();
    }
}