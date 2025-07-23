using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenIdProvider.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddEnumForUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserOrganizationRoles""
                ALTER COLUMN ""Role"" TYPE integer
                USING
                  CASE ""Role""
                    WHEN 'Viewer' THEN 0
                    WHEN 'Admin' THEN 1
                    WHEN 'Owner' THEN 2
                    ELSE 0
                  END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserOrganizationRoles""
                ALTER COLUMN ""Role"" TYPE character varying(50)
                USING
                  CASE ""Role""
                    WHEN 0 THEN 'Viewer'
                    WHEN 1 THEN 'Admin'
                    WHEN 2 THEN 'Owner'
                    ELSE 'Viewer'
                  END;
            ");
        }
    }
}
