using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Data.ModelConfigurations;

public class OrganizationClientPermissionConfiguration : IEntityTypeConfiguration<OrganizationClientPermission>
{
    public void Configure(EntityTypeBuilder<OrganizationClientPermission> builder)
    {
        builder.HasKey(oc => new { oc.OrganizationId, oc.ClientId });

        // Configure the one-to-many relationship
        builder.HasOne(oc => oc.Organization)
               .WithMany(o => o.AllowedClientPermissions)
               .HasForeignKey(oc => oc.OrganizationId);
    }
}