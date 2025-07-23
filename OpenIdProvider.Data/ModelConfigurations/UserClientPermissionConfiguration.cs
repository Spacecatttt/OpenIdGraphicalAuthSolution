using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class UserClientPermissionConfiguration : IEntityTypeConfiguration<UserClientPermission>
{
    public void Configure(EntityTypeBuilder<UserClientPermission> builder)
    {
        builder.HasKey(uc => new { uc.UserId, uc.ClientId });

        // Configure the one-to-many relationship
        builder.HasOne(uc => uc.User)
               .WithMany(u => u.AllowedClientPermissions)
               .HasForeignKey(uc => uc.UserId);
    }
}