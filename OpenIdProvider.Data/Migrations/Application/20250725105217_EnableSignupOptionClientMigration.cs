using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenIdProvider.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class EnableSignupOptionClientMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserClientPermissions""
                ALTER COLUMN ""ClientId"" TYPE integer
                USING ""ClientId""::integer;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""OrganizationClientPermissions""
                ALTER COLUMN ""ClientId"" TYPE integer
                USING ""ClientId""::integer;
            ");

            migrationBuilder.AddColumn<bool>(
                name: "EnableSignup",
                table: "ClientOwnerships",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableSignup",
                table: "ClientOwnerships");

            migrationBuilder.Sql(@"
                ALTER TABLE ""UserClientPermissions""
                ALTER COLUMN ""ClientId"" TYPE text
                USING ""ClientId""::text;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""OrganizationClientPermissions""
                ALTER COLUMN ""ClientId"" TYPE text
                USING ""ClientId""::text;
            ");
        }
    }
}
